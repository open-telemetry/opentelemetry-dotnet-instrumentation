// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if defined(_WIN32) && (defined(_M_AMD64) || defined(_M_IX86))
#include "profiler_stack_capture.h"

#include <stdexcept>
#include <algorithm>
#include <cassert>
#include <atlbase.h>
#include <atlcom.h>
#include "logger.h"

#ifndef DECLSPEC_IMPORT
#define DECLSPEC_IMPORT __declspec(dllimport)
#endif

#if defined(_M_AMD64)
extern "C"
{
    DECLSPEC_IMPORT PRUNTIME_FUNCTION NTAPI RtlLookupFunctionEntry(DWORD64               ControlPc,
                                                                   PDWORD64              ImageBase,
                                                                   PUNWIND_HISTORY_TABLE HistoryTable);
}
#endif // defined(_M_AMD64)

namespace ProfilerStackCapture
{

/// @brief Helper for safety probe worker (SEH-protected, no std::unique_ptr)
static HRESULT ExecuteProbeOperations(IProfilerApi* profilerApi, ThreadID canaryManagedId, const CONTEXT& canaryCtx)
{
    HRESULT result = S_OK;

    int* testAlloc = nullptr;
    __try
    {

        // Test 1: Heap allocation (using new/delete instead of unique_ptr as we are inside SEH block)
        if (testAlloc = new int(42))
        {
            delete testAlloc;
            testAlloc = nullptr;
        }
#if defined(_M_AMD64)
        // Test 2: RTL function lookup
        UNWIND_HISTORY_TABLE historyTable = {};
        DWORD64              imageBase    = 0;
        RtlLookupFunctionEntry(canaryCtx.Rip, &imageBase, &historyTable);
#endif // defined(_M_AMD64)

        // Test 3: DoStackSnapshot
        auto probeCallback = [](FunctionID, UINT_PTR, COR_PRF_FRAME_INFO, ULONG32, BYTE[], void*) -> HRESULT
        { return S_FALSE; };

        result =
            profilerApi->DoStackSnapshot(canaryManagedId, probeCallback, COR_PRF_SNAPSHOT_DEFAULT, nullptr, nullptr, 0);
    }
    __except (EXCEPTION_EXECUTE_HANDLER)
    {
        DWORD exceptionCode = GetExceptionCode();
        trace::Logger::Debug("[StackCapture] ExecuteProbeOperations - Exception during safety tests. ExceptionCode=0x",
                             std::hex, exceptionCode, std::dec
#if defined(_M_AMD64)
                             ,
                             ", RIP=0x", canaryCtx.Rip
#endif
        );
        if (testAlloc)
        {
            delete testAlloc;
        }
        return E_FAIL;
    }

    // If stack snapshot was aborted, treat as success for probe purposes, as we explicitly
    // short-circuited it from the callback, prompting CORPROF_E_STACKSNAPSHOT_ABORTED
    return result == CORPROF_E_STACKSNAPSHOT_ABORTED ? S_OK : result;
}

// InvocationQueue implementation
InvocationQueue::InvocationQueue()
{
    worker_ = std::unique_ptr<std::thread, ThreadJoinerOnDelete>(new std::thread(&InvocationQueue::WorkerLoop, this));
}
InvocationQueue::~InvocationQueue()
{
    Stop();
}
void InvocationQueue::Stop()
{
    bool expected = false;
    if (stop_.compare_exchange_strong(expected, true))
    {
        condVar_.notify_all();
    }
    else
    {
        stop_ = true;
        condVar_.notify_all();
    }
}
InvocationStatus InvocationQueue::Invoke(const std::function<void()>& fn, std::chrono::milliseconds timeout)
{
    if (stop_.load())
        return InvocationStatus::TimedOut;
    auto item = std::make_shared<QueuedInvocation>();
    item->fn  = fn;
    auto fut  = item->completedPromise.get_future();
    {
        std::lock_guard<std::mutex> lock(mutex_);
        queue_.push_back(item);
    }
    condVar_.notify_one();
    return fut.wait_for(timeout) == std::future_status::ready ? InvocationStatus::Invoked : InvocationStatus::TimedOut;
}
void InvocationQueue::WorkerLoop()
{
    for (;;)
    {
        std::shared_ptr<QueuedInvocation> item;
        {
            std::unique_lock<std::mutex> lock(mutex_);
            condVar_.wait(lock, [this]() { return stop_.load() || !queue_.empty(); });
            if (stop_.load())
                break;
            if (!queue_.empty())
            {
                item = queue_.front();
                queue_.pop_front();
            }
            else
                continue;
        }
        try
        {
            item->fn();
        }
        catch (...)
        {
        }
        item->completedPromise.set_value();
    }
}

// ProfilerApiAdapter
HRESULT ProfilerApiAdapter::DoStackSnapshot(ThreadID              threadId,
                                            StackSnapshotCallback callback,
                                            DWORD                 infoFlags,
                                            void*                 clientData,
                                            BYTE*                 context,
                                            ULONG                 contextSize)
{
    return profilerInfo_->DoStackSnapshot(threadId, callback, infoFlags, clientData, context, contextSize);
}
HRESULT ProfilerApiAdapter::GetFunctionFromIP(LPCBYTE ip, FunctionID* functionId)
{
    return profilerInfo_->GetFunctionFromIP(ip, functionId);
}

// ScopedThreadSuspend
ScopedThreadSuspend::ScopedThreadSuspend(DWORD nativeThreadId) : threadHandle_(INVALID_HANDLE_VALUE), suspended_(false)
{
    threadHandle_ = OpenThread(THREAD_GET_CONTEXT | THREAD_SUSPEND_RESUME, FALSE, nativeThreadId);
    if (threadHandle_ == NULL)
    {
        throw std::runtime_error("Failed to open thread handle");
    }

    DWORD suspendCount = SuspendThread(threadHandle_);
    if (suspendCount == static_cast<DWORD>(-1))
    {
        CloseHandle(threadHandle_);
        threadHandle_ = INVALID_HANDLE_VALUE;
        throw std::runtime_error("Failed to suspend thread");
    }

    suspended_ = true;
}

ScopedThreadSuspend::~ScopedThreadSuspend()
{
    if (threadHandle_ != INVALID_HANDLE_VALUE)
    {
        if (suspended_)
        {
            ResumeThread(threadHandle_);
        }
        CloseHandle(threadHandle_);
    }
}

ScopedThreadSuspend::ScopedThreadSuspend(ScopedThreadSuspend&& other) noexcept
    : threadHandle_(other.threadHandle_), suspended_(other.suspended_)
{
    other.threadHandle_ = INVALID_HANDLE_VALUE;
    other.suspended_    = false;
}

ScopedThreadSuspend& ScopedThreadSuspend::operator=(ScopedThreadSuspend&& other) noexcept
{
    if (this != &other)
    {
        if (threadHandle_ != INVALID_HANDLE_VALUE)
        {
            if (suspended_)
                ResumeThread(threadHandle_);
            CloseHandle(threadHandle_);
        }
        threadHandle_       = other.threadHandle_;
        suspended_          = other.suspended_;
        other.threadHandle_ = INVALID_HANDLE_VALUE;
        other.suspended_    = false;
    }
    return *this;
}

// StackCaptureEngine
StackCaptureEngine::StackCaptureEngine(std::unique_ptr<IProfilerApi> profilerApi, const CaptureOptions& options)
    : profilerApi_(std::move(profilerApi)), options_(options)
{
    invocationQueue_ = std::make_unique<InvocationQueue>();
    trace::Logger::Info(L"[StackCapture] Engine initialized with canary prefix: ", options_.canaryThreadName);
}
StackCaptureEngine::~StackCaptureEngine()
{
    Stop();
}

void StackCaptureEngine::Stop()
{
    stopRequested_ = true;
    captureCondVar_.notify_all();
    if (invocationQueue_)
        invocationQueue_->Stop();
}

HRESULT StackCaptureEngine::ThreadDestroyed(ThreadID threadId)
{
    std::lock_guard<std::mutex> lock(threadListMutex_);
    activeThreads_.erase(threadId);
    threadNames_.erase(threadId);

    // Clear canary if it was this thread
    if (canaryThread_.managedId == threadId)
    {
        trace::Logger::Info("[StackCapture] Canary thread destroyed - ManagedID=", threadId,
                            ", NativeID=", canaryThread_.nativeId);
        canaryThread_.reset();
        // threadNames_ has map of thread ID and names - find another canary if possible
        for (const auto& [managedId, name] : threadNames_)
        {
            if (options_.IsCanaryThread(name))
            {
                auto osThreadIt = activeThreads_.find(managedId);
                if (osThreadIt != activeThreads_.end())
                {
                    canaryThread_ = CanaryThreadInfo{managedId, osThreadIt->second};
                    trace::Logger::Info("[StackCapture] New canary thread designated after destruction - ManagedID=",
                                        managedId, ", NativeID=", osThreadIt->second, ", Name=", name);
                    captureCondVar_.notify_all();
                    break;
                }
            }
        }
    }

    return S_OK;
}

HRESULT StackCaptureEngine::ThreadAssignedToOSThread(ThreadID managedThreadId, DWORD osThreadId)
{
    std::lock_guard<std::mutex> lock(threadListMutex_);
    activeThreads_[managedThreadId] = osThreadId;

    if (canaryThread_.isValid())
    {
        return S_OK;
    }

    auto nameIt = threadNames_.find(managedThreadId);
    if (nameIt != threadNames_.end())
    {
        if (options_.IsCanaryThread(nameIt->second))
        {
            canaryThread_ = CanaryThreadInfo{managedThreadId, osThreadId};
            trace::Logger::Info("[StackCapture] Canary thread designated via ThreadAssignedToOSThread - ManagedID=",
                                managedThreadId, ", NativeID=", osThreadId, ", Name=", nameIt->second);
            captureCondVar_.notify_all();
        }
    }

    return S_OK;
}

HRESULT StackCaptureEngine::ThreadNameChanged(ThreadID threadId, ULONG cchName, WCHAR name[])
{
    if (!name || cchName == 0)
        return S_OK;

    std::lock_guard<std::mutex> lock(threadListMutex_);

    std::wstring threadName(name, cchName);
    threadNames_[threadId] = threadName;
    trace::Logger::Debug("[StackCapture] ThreadNameChanged - ManagedID=", threadId, ", Name=", threadName);
    if (options_.IsCanaryThread(threadName))
    {
        auto osThreadIt = activeThreads_.find(threadId);
        if (osThreadIt != activeThreads_.end() && !canaryThread_.isValid())
        {
            canaryThread_ = CanaryThreadInfo{threadId, osThreadIt->second};
            captureCondVar_.notify_all();
            trace::Logger::Info("[StackCapture] Canary thread designated via ThreadNameChanged - ManagedID=", threadId,
                                ", NativeID=", osThreadIt->second, ", Name=", threadName);
        }
        else
        {
            trace::Logger::Debug(
                "[StackCapture] Canary thread name matched but OS thread not yet assigned - ManagedID=", threadId,
                ", Name=", threadName);
        }
    }

    return S_OK;
}

bool StackCaptureEngine::SafetyProbe(const CanaryThreadInfo& canaryInfo)
{
    if (!invocationQueue_)
        return true;

    std::atomic<HRESULT> snapshotHr{S_OK};
    InvocationStatus     status = InvocationStatus::TimedOut;

    try
    {
        ScopedThreadSuspend canaryThread(canaryInfo.nativeId);

        CONTEXT canaryCtx      = {};
        canaryCtx.ContextFlags = CONTEXT_FULL;

        // Get thread context
        if (!GetThreadContext(canaryThread.GetHandle(), &canaryCtx))
        {
            DWORD error = GetLastError();
            trace::Logger::Error("[StackCapture] SafetyProbe failed - GetThreadContext failed. Error=", error,
                                 ", NativeID=", canaryThread_.nativeId);
            return false;
        }

        auto canaryManagedId = canaryInfo.managedId;

        status = invocationQueue_->Invoke(
            [this, canaryManagedId, canaryCtx, &snapshotHr]()
            {
                HRESULT hr = ExecuteProbeOperations(profilerApi_.get(), canaryManagedId, canaryCtx);
                snapshotHr.store(hr);
            },
            options_.probeTimeout);

        // Canary thread auto-resumes here via RAII
    }
    catch (const std::exception& ex)
    {
        trace::Logger::Error("[StackCapture] SafetyProbe failed - Exception during thread suspension/context capture: ",
                             ex.what());
        return false;
    }

    // Check invocation status
    if (status != InvocationStatus::Invoked)
    {
        trace::Logger::Warn("[StackCapture] SafetyProbe failed - Probe operations timed out after ",
                            options_.probeTimeout.count(), "ms");
        return false;
    }

    // Analyze HRESULT from probe operations
    HRESULT hr = snapshotHr.load();

    if (hr == CORPROF_E_STACKSNAPSHOT_UNSAFE)
    {
        trace::Logger::Warn("[StackCapture] SafetyProbe detected UNSAFE condition - DoStackSnapshot returned "
                            "CORPROF_E_STACKSNAPSHOT_UNSAFE");
        return false;
    }

    if (FAILED(hr))
    {
        // Log specific HRESULT codes for diagnostics
        if (hr == E_FAIL)
        {
            trace::Logger::Error("[StackCapture] SafetyProbe failed - Probe operations returned E_FAIL (0x", std::hex,
                                 hr, std::dec, ")");
        }
        else if (hr == E_ABORT)
        {
            trace::Logger::Error("[StackCapture] SafetyProbe failed - Probe operations aborted (0x", std::hex, hr,
                                 std::dec, ")");
        }
        else
        {
            trace::Logger::Error("[StackCapture] SafetyProbe failed - Probe operations returned HRESULT=0x", std::hex,
                                 hr, std::dec);
        }
        return false;
    }

    trace::Logger::Debug("[StackCapture] SafetyProbe succeeded - Stack capture is safe");
    return true;
}

HRESULT StackCaptureEngine::CaptureStackUnseeded(ThreadID managedThreadId, StackCaptureContext* stackCaptureContext)
{
    stackCaptureContext->clientParams->threadId = managedThreadId;
    return profilerApi_->DoStackSnapshotUnseeded(managedThreadId, stackCaptureContext->clientParams);
}

HRESULT StackCaptureEngine::CaptureStack(ThreadID             managedThreadId,
                                         HANDLE               threadHandle,
                                         StackCaptureContext* stackCaptureContext)
{
    // Unseeded DoStackSnapshot: works when the thread is in managed code.
    // Threads stuck in native code will fail and be skipped.
    HRESULT hr = CaptureStackUnseeded(managedThreadId, stackCaptureContext);

    if (SUCCEEDED(hr))
    {
        trace::Logger::Debug("[StackCapture] Unseeded capture succeeded. ThreadID=", managedThreadId);
    }
    else
    {
        trace::Logger::Debug("[StackCapture] Unseeded capture failed (0x", std::hex, hr, std::dec,
                             "). ThreadID=", managedThreadId);
    }

    return hr;
}

CanaryThreadInfo StackCaptureEngine::WaitForCanaryThread(std::chrono::milliseconds timeout)
{
    trace::Logger::Debug("[StackCapture] Waiting for canary thread (timeout=", timeout.count(), "ms)");
    CanaryThreadInfo canary;
    {
        std::unique_lock<std::mutex> lock(threadListMutex_);
        bool                         result = captureCondVar_.wait_for(lock, timeout,
                                                                       [this]() { return stopRequested_.load() || canaryThread_.isValid(); });

        if (!result)
        {
            trace::Logger::Warn("[StackCapture] Canary thread wait timed out after ", timeout.count(), "ms");
        }
        else
        {
            canary = canaryThread_;
            trace::Logger::Debug("[StackCapture] Canary thread ready - ManagedID=", canary.managedId,
                                 ", NativeID=", canary.nativeId);
        }
    }

    return canary;
}

HRESULT StackCaptureEngine::CaptureStacks(std::unordered_set<ThreadID> const&                threads,
                                          continuous_profiler::StackSnapshotCallbackContext* clientData)
{
    auto canary = WaitForCanaryThread();

    if (!canary.isValid())
        return E_FAIL;

    for (const auto& managedId : threads)
    {
        if (stopRequested_)
            break;
        if (managedId == canary.managedId)
            continue;
        DWORD nativeId = 0;
        {
            std::lock_guard<std::mutex> lock(threadListMutex_);
            auto                        it = activeThreads_.find(managedId);
            if (it == activeThreads_.end())
            {
                continue;
            }
            nativeId = it->second;
        }
        try
        {
            ScopedThreadSuspend targetThread(nativeId);
            if (!SafetyProbe(canary))
            {
                trace::Logger::Debug(
                    "[StackCapture] CaptureStacks - Skipping thread due to safety probe failure. ManagedID=", managedId,
                    ", NativeID=", nativeId);
                continue;
            }
            clientData->threadId = managedId;
            StackCaptureContext stackCaptureContext{0, &stopRequested_, clientData};
            CaptureStack(managedId, targetThread.GetHandle(), &stackCaptureContext);
        }
        catch (const std::exception& ex)
        {
            trace::Logger::Error("[StackCapture] CaptureStacks - Exception during stack capture for ManagedID=",
                                 managedId, ", NativeID=", nativeId, ": ", ex.what());
        }
    }
    return S_OK;
}

} // namespace ProfilerStackCapture
#endif // defined(_WIN32) && (defined(_M_AMD64) || defined(_M_IX86))