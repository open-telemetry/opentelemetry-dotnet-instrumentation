// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if defined(_WIN32) && defined(_M_AMD64)
#include "profiler_stack_capture.h"

#include <stdexcept>
#include <algorithm>
#include <cassert>
#include <dbghelp.h>
#include <atlbase.h>
#include <atlcom.h>
#include <sstream>
#include <iomanip>
#include "logger.h"

#ifndef DECLSPEC_IMPORT
#define DECLSPEC_IMPORT __declspec(dllimport)
#endif

extern "C"
{
    DECLSPEC_IMPORT PRUNTIME_FUNCTION NTAPI  RtlLookupFunctionEntry(DWORD64               ControlPc,
                                                                    PDWORD64              ImageBase,
                                                                    PUNWIND_HISTORY_TABLE HistoryTable);
    DECLSPEC_IMPORT PEXCEPTION_ROUTINE NTAPI RtlVirtualUnwind(DWORD                          HandlerType,
                                                              DWORD64                        ImageBase,
                                                              DWORD64                        ControlPc,
                                                              PRUNTIME_FUNCTION              FunctionEntry,
                                                              PCONTEXT                       ContextRecord,
                                                              PVOID*                         HandlerData,
                                                              PDWORD64                       EstablisherFrame,
                                                              PKNONVOLATILE_CONTEXT_POINTERS ContextPointers);
}

namespace ProfilerStackCapture
{

// ========================================================================================
// SEH-Protected Helper Functions
// ========================================================================================

/// @brief Helper function for reading return address (SEH-protected, no C++ objects)
static bool ReadReturnAddressFromStack(DWORD64 rsp, DWORD64* pReturnAddress)
{
    __try
    {
        *pReturnAddress = *reinterpret_cast<PULONG64>(rsp);
        return true;
    }
    __except (EXCEPTION_EXECUTE_HANDLER)
    {
        trace::Logger::Debug("[StackCapture] ReadReturnAddressFromStack - Access violation reading RSP=0x", std::hex,
                             rsp, std::dec, ", ExceptionCode=0x", std::hex, GetExceptionCode(), std::dec);
        return false;
    }
}

/// @brief Helper function for RtlVirtualUnwind (SEH-protected, no C++ objects)
static bool SafeRtlVirtualUnwind(DWORD64           imageBase,
                                 DWORD64           controlPc,
                                 PRUNTIME_FUNCTION runtimeFunction,
                                 PCONTEXT          context,
                                 PULONG64          pEstablisherFrame)
{
    __try
    {
        PVOID   handlerData = nullptr;
        ULONG64 eFrame      = 0;
        RtlVirtualUnwind(0, imageBase, controlPc, runtimeFunction, context, &handlerData, &eFrame, nullptr);

        if (pEstablisherFrame)
        {
            *pEstablisherFrame = eFrame;
        }

        return true;
    }
    __except (EXCEPTION_EXECUTE_HANDLER)
    {
        trace::Logger::Debug("[StackCapture] SafeRtlVirtualUnwind - RtlVirtualUnwind failed. ImageBase=0x", std::hex,
                             imageBase, ", ControlPC=0x", controlPc, std::dec, ", ExceptionCode=0x", std::hex,
                             GetExceptionCode(), std::dec);
        return false;
    }
}

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
        // Test 2: RTL function lookup
        UNWIND_HISTORY_TABLE historyTable = {};
        DWORD64              imageBase    = 0;
        RtlLookupFunctionEntry(canaryCtx.Rip, &imageBase, &historyTable);

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
                             std::hex, exceptionCode, std::dec, ", RIP=0x", canaryCtx.Rip);
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

// PrepareContextForSnapshot - walks native stack to find managed frame and prepares context for DoStackSnapshot
static HRESULT PrepareContextForSnapshot(ThreadID           managedThreadId,
                                         HANDLE             threadHandle,
                                         CONTEXT*           pContext,
                                         IProfilerApi*      profilerApi,
                                         std::atomic<bool>* pStopRequested)
{
    const int MAX_WALK_EVER = 10000;
    DWORD64   origRSP       = 0;

    // Quick check: are we already at managed code?
    FunctionID fid = 0;
    HRESULT    hr  = profilerApi->GetFunctionFromIP(reinterpret_cast<LPCBYTE>(pContext->Rip), &fid);
    if (SUCCEEDED(hr) && fid != 0)
    {
        return S_OK;
    }

    // Walk native frames to find managed code
    for (int walkCount = 0; walkCount < MAX_WALK_EVER; ++walkCount)
    {
        if (pStopRequested && pStopRequested->load())
        {
            return E_ABORT;
        }

        // Check for stack progress
        if (origRSP != 0 && pContext->Rsp <= origRSP)
        {
            break;
        }
        origRSP = pContext->Rsp;

        // Check for end of stack
        if (pContext->Rip == 0)
        {
            break;
        }

        // Try to find runtime function for current RIP
        UNWIND_HISTORY_TABLE historyTable    = {};
        DWORD64              imageBase       = 0;
        PRUNTIME_FUNCTION    runtimeFunction = RtlLookupFunctionEntry(pContext->Rip, &imageBase, &historyTable);

        DWORD64 instructionPointer;

        if (!runtimeFunction)
        {
            // Leaf function - read return address from stack
            DWORD64 returnAddress = 0;
            if (!ReadReturnAddressFromStack(pContext->Rsp, &returnAddress))
            {
                return E_FAIL;
            }
            // no runtime function,  manually unwind to previous frame, adjust the RIP and RSP fields
            pContext->Rip = returnAddress;
            pContext->Rsp += sizeof(DWORD64);
            instructionPointer = returnAddress;
        }
        else
        {
            // Has unwind info - use function begin address (critical for CLR detection)
            instructionPointer = imageBase + runtimeFunction->BeginAddress;

            // Unwind to previous frame, updates Rip and Rsp fields, we have runtimeFunction to guide us
            if (!SafeRtlVirtualUnwind(imageBase, pContext->Rip, runtimeFunction, pContext, nullptr))
            {
                return E_FAIL;
            }
        }
        // Virtual unwind traverses frames, so after unwind, RIP points to caller's instruction
        // For leaf functions, we manually set RIP to return address
        // Illustration: we use virtual unwind or manual stack read to move from:
        // SleepEx() -> CLR transition stub -> YourApp.DoWork()
        // Before:
        // kernel32.dll!SleepEx ->RIP might point to this (native code)
        // |_CLR transition stub
        //   |_YourApp.DoWork() -> We want to start HERE in managed code, not in SleepEx or the transition stub
        // After: RIP is adjusted to point to the managed frame - YourApp.DoWork()

        // Check if this instruction pointer is managed
        hr = profilerApi->GetFunctionFromIP(reinterpret_cast<LPCBYTE>(instructionPointer), &fid);
        if (SUCCEEDED(hr) && fid != 0)
        {
            // Update context to point to this managed frame's beginning
            pContext->Rip = instructionPointer; // this is the seed for DoStackSnapshot
            return S_OK;
        }
    }

    // Exhausted all frames without finding managed code, log it
    trace::Logger::Warn(
        "[StackCapture] PrepareContextForSnapshot - Unable to locate managed frame in stack walk for ThreadID=",
        managedThreadId);
    return E_FAIL;
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
        if (nameIt->second == options_.canaryThreadName)
        {
            canaryThread_ = CanaryThreadInfo{managedThreadId, osThreadId};
            trace::Logger::Info("[StackCapture] Canary thread designated via ThreadAssignedToOSThread - ManagedID=",
                                managedThreadId, ", NativeID=", osThreadId, ", Name=", nameIt->second);
        }
    }

    // Notify waiting capture thread if canary was found
    if (canaryThread_.isValid())
    {
        captureCondVar_.notify_one();
    }

    return S_OK;
}

HRESULT StackCaptureEngine::ThreadNameChanged(ThreadID threadId, ULONG cchName, WCHAR name[])
{
    if (!name || cchName == 0)
        return S_OK;

    std::lock_guard<std::mutex> lock(threadListMutex_);
    if (canaryThread_.isValid())
    {
        return S_OK;
    }

    std::wstring threadName(name, cchName);
    threadNames_[threadId] = threadName;
    trace::Logger::Debug("[StackCapture] ThreadNameChanged - ManagedID=", threadId, ", Name=", threadName);
    if (threadName == options_.canaryThreadName)
    {
        auto osThreadIt = activeThreads_.find(threadId);
        if (osThreadIt != activeThreads_.end())
        {
            canaryThread_ = CanaryThreadInfo{threadId, osThreadIt->second};
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

    // Notify waiting capture thread if canary was found
    if (canaryThread_.isValid())
    {
        captureCondVar_.notify_one();
    }

    return S_OK;
}

bool StackCaptureEngine::SafetyProbe()
{
    if (!invocationQueue_)
        return true;

    std::atomic<HRESULT> snapshotHr{S_OK};
    InvocationStatus     status = InvocationStatus::TimedOut;

    try
    {
        ScopedThreadSuspend canaryThread(canaryThread_.nativeId);

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

        auto canaryManagedId = canaryThread_.managedId;

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

HRESULT StackCaptureEngine::CaptureStackSeeded(ThreadID             managedThreadId,
                                               HANDLE               threadHandle,
                                               StackCaptureContext* stackCaptureContext)
{
    CONTEXT context      = {};
    context.ContextFlags = CONTEXT_FULL;

    if (!GetThreadContext(threadHandle, &context))
    {
        return E_FAIL;
    }

    // Prepare context (uses SEH-protected helpers internally, no C++ RAII in that path)
    HRESULT hr =
        PrepareContextForSnapshot(managedThreadId, threadHandle, &context, profilerApi_.get(), &stopRequested_);
    if (FAILED(hr))
    {
        return hr;
    }
    stackCaptureContext->clientParams->threadId = managedThreadId;
    hr                                          = profilerApi_->DoStackSnapshot(managedThreadId,
                                                                                continuous_profiler::IStackCaptureStrategy::StackSnapshotCallbackDefault,
                                                                                COR_PRF_SNAPSHOT_DEFAULT, stackCaptureContext->clientParams,
                                                                                reinterpret_cast<BYTE*>(&context), sizeof(CONTEXT));

    if (FAILED(hr))
    {
        trace::Logger::Error("[StackCapture] CaptureStackSeeded - DoStackSnapshot failed. HRESULT=0x", std::hex, hr,
                             std::dec, ", ThreadID=", managedThreadId, ", SeededRIP=0x", std::hex, context.Rip,
                             std::dec);
    }
    else
    {
        trace::Logger::Debug("[StackCapture] CaptureStackSeeded - Successfully captured stack. ThreadID=",
                             managedThreadId);
    }

    return hr;
}

bool StackCaptureEngine::IsManagedFunction(BYTE* ip) const
{
    FunctionID fid = 0;
    HRESULT    hr  = profilerApi_->GetFunctionFromIP(ip, &fid);
    return SUCCEEDED(hr) && fid != 0;
}

bool StackCaptureEngine::WaitForCanaryThread(std::chrono::milliseconds timeout)
{
    trace::Logger::Debug("[StackCapture] Waiting for canary thread (timeout=", timeout.count(), "ms)");
    std::unique_lock<std::mutex> lock(threadListMutex_);
    bool                         result =
        captureCondVar_.wait_for(lock, timeout, [this]() { return stopRequested_.load() || canaryThread_.isValid(); });

    if (!result)
    {
        trace::Logger::Warn("[StackCapture] Canary thread wait timed out after ", timeout.count(), "ms");
    }

    return result;
}

HRESULT StackCaptureEngine::CaptureStacks(std::unordered_set<ThreadID> const&                threads,
                                          continuous_profiler::StackSnapshotCallbackContext* clientData)
{
    bool canaryReady = WaitForCanaryThread();

    if (!canaryReady)
        return E_FAIL;

    for (const auto& managedId : threads)
    {
        if (stopRequested_)
            break;
        if (managedId == canaryThread_.managedId)
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
            if (!SafetyProbe())
            {
                trace::Logger::Debug(
                    "[StackCapture] CaptureStacks - Skipping thread due to safety probe failure. ManagedID=", managedId,
                    ", NativeID=", nativeId);
                continue;
            }
            clientData->threadId = managedId;
            StackCaptureContext stackCaptureContext{0, &stopRequested_, clientData};
            CaptureStackSeeded(managedId, targetThread.GetHandle(), &stackCaptureContext);
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
#endif // defined(_WIN32) && defined(_M_AMD64)