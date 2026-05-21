// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0


#include "suspension_policy.h"

#include <stdexcept>

#include "logger.h"

namespace ProfilerStackCapture
{

ClrRuntimeSuspensionPolicy::ClrRuntimeSuspensionPolicy(IProfilerApi* profilerApi, InvocationQueue* queue)
    : profilerApi_(profilerApi)
{
#if defined(_WIN32) && defined(_M_AMD64)
    safetyProbe_ = std::make_unique<StackSafetyProbe>(profilerApi, queue, 
        kClrSuspensionProbeFlags, CaptureOptions{}.probeTimeout);
#else
    (void)queue;
#endif
}

HRESULT ClrRuntimeSuspensionPolicy::BeginBatch()
{
    return profilerApi_->SuspendRuntime();
}

void ClrRuntimeSuspensionPolicy::EndBatch() noexcept
{
    HRESULT hr = profilerApi_->ResumeRuntime();
    if (FAILED(hr))
    {
        trace::Logger::Error("[ClrRuntimeSuspensionPolicy] ResumeRuntime failed. HRESULT=",
                             trace::HResultStr(hr));
    }
}

bool ClrRuntimeSuspensionPolicy::PrepareForCapture(const std::unordered_set<ThreadID>&)
{
    return true;
}

bool ClrRuntimeSuspensionPolicy::ShouldSkipThread(ThreadID) const
{
    return false;
}

bool ClrRuntimeSuspensionPolicy::RequiresOsThreadIdForManagedSnapshot() const
{
    return false;
}

HRESULT ClrRuntimeSuspensionPolicy::GetOsThreadId(ThreadID managedThreadId, DWORD* osThreadId)
{
    return profilerApi_->GetThreadInfo(managedThreadId, osThreadId);
}

std::unique_ptr<IThreadCaptureScope> ClrRuntimeSuspensionPolicy::AcquireThread(DWORD)
{
    return std::make_unique<NoopThreadCaptureScope>();
}

#if defined(_WIN32)
#if defined(_M_AMD64)
static void ProbeFunction()
{
    // This function is never called directly.  Its address is used as a probe target
    // for RtlLookupFunctionEntry, which will exercise the loader lock if it's trying
    // to walk a thread that's suspended in the middle of loader lock acquisition.
}
#endif
bool ClrRuntimeSuspensionPolicy::BeforeStackSnapshot()
{
#if defined(_M_AMD64)
    if (!safetyProbe_)
    {
        return true;
    }
    return safetyProbe_->Run(/*canary*/ 0, reinterpret_cast<DWORD64>(&ProbeFunction));
#else
    return true;
#endif
}
#else
bool ClrRuntimeSuspensionPolicy::BeforeStackSnapshot()
{
    return true;
}
#endif

#if defined(_WIN32)
OsThreadSuspensionPolicy::OsThreadSuspensionPolicy(IProfilerApi*         profilerApi,
                                                   InvocationQueue*      queue,
                                                   const CaptureOptions& options)
    : profilerApi_(profilerApi)
    , options_(options)
    , safetyProbe_(
          std::make_unique<StackSafetyProbe>(profilerApi, queue, kThreadSuspensionProbeFlags, options.probeTimeout))
{
    trace::Logger::Info(L"[OsThreadSuspensionPolicy] Initialized with canary prefix: ", options_.canaryThreadName);
}


OsThreadSuspensionPolicy::~OsThreadSuspensionPolicy()
{
    Stop();
}

HRESULT OsThreadSuspensionPolicy::BeginBatch()
{
    return S_OK;
}

void OsThreadSuspensionPolicy::EndBatch() noexcept
{
}

bool OsThreadSuspensionPolicy::PrepareForCapture(const std::unordered_set<ThreadID>&)
{
    auto canary = WaitForCanaryThread(options_.probeTimeout);
    return canary.isValid();
}

bool OsThreadSuspensionPolicy::ShouldSkipThread(ThreadID managedThreadId) const
{
    std::lock_guard<std::mutex> lock(threadListMutex_);
    return canaryThread_.managedId == managedThreadId;
}

bool OsThreadSuspensionPolicy::RequiresOsThreadIdForManagedSnapshot() const
{
    return true;
}

HRESULT OsThreadSuspensionPolicy::GetOsThreadId(ThreadID managedThreadId, DWORD* osThreadId)
{
    if (!osThreadId)
    {
        return E_INVALIDARG;
    }

    std::lock_guard<std::mutex> lock(threadListMutex_);
    auto it = activeThreads_.find(managedThreadId);
    if (it == activeThreads_.end())
    {
        return profilerApi_->GetThreadInfo(managedThreadId, osThreadId);
    }

    *osThreadId = it->second;
    return S_OK;
}

std::unique_ptr<IThreadCaptureScope> OsThreadSuspensionPolicy::AcquireThread(DWORD osThreadId)
{
    return std::make_unique<ScopedThreadCaptureScope>(osThreadId);
}

bool OsThreadSuspensionPolicy::BeforeStackSnapshot()
{
    CanaryThreadInfo canary;
    {
        std::lock_guard<std::mutex> lock(threadListMutex_);
        canary = canaryThread_;
    }

    return canary.isValid() && SafetyProbe(canary);
}

void OsThreadSuspensionPolicy::Stop()
{
    stopRequested_ = true;
    captureCondVar_.notify_all();
}

void OsThreadSuspensionPolicy::OnThreadDestroyed(ThreadID threadId)
{
    std::lock_guard<std::mutex> lock(threadListMutex_);

    activeThreads_.erase(threadId);
    threadNames_.erase(threadId);

    if (canaryThread_.managedId != threadId)
    {
        return;
    }

    trace::Logger::Info("[OsThreadSuspensionPolicy] Canary thread destroyed - ManagedID=",
                        threadId, ", NativeID=", canaryThread_.nativeId);

    canaryThread_.reset();

    for (const auto& [managedId, name] : threadNames_)
    {
        if (!options_.IsCanaryThread(name))
        {
            continue;
        }

        auto osThreadIt = activeThreads_.find(managedId);
        if (osThreadIt == activeThreads_.end())
        {
            continue;
        }

        canaryThread_ = CanaryThreadInfo{managedId, osThreadIt->second};
        trace::Logger::Info("[OsThreadSuspensionPolicy] New canary thread designated - ManagedID=",
                            managedId, ", NativeID=", osThreadIt->second, ", Name=", name);
        captureCondVar_.notify_all();
        break;
    }
}

void OsThreadSuspensionPolicy::OnThreadNameChanged(ThreadID threadId, ULONG cchName, WCHAR name[])
{
    if (!name || cchName == 0)
    {
        return;
    }

    std::lock_guard<std::mutex> lock(threadListMutex_);

    std::wstring threadName(name, cchName);
    threadNames_[threadId] = threadName;

    trace::Logger::Debug("[OsThreadSuspensionPolicy] ThreadNameChanged - ManagedID=",
                         threadId, ", Name=", threadName);

    if (!options_.IsCanaryThread(threadName) || canaryThread_.isValid())
    {
        return;
    }

    auto osThreadIt = activeThreads_.find(threadId);
    if (osThreadIt == activeThreads_.end())
    {
        trace::Logger::Debug("[OsThreadSuspensionPolicy] Canary name matched but OS thread not yet assigned - ManagedID=",
                             threadId);
        return;
    }

    canaryThread_ = CanaryThreadInfo{threadId, osThreadIt->second};
    captureCondVar_.notify_all();

    trace::Logger::Info("[OsThreadSuspensionPolicy] Canary thread designated via ThreadNameChanged - ManagedID=",
                        threadId, ", NativeID=", osThreadIt->second, ", Name=", threadName);
}

void OsThreadSuspensionPolicy::OnThreadAssignedToOSThread(ThreadID managedThreadId, DWORD osThreadId)
{
    std::lock_guard<std::mutex> lock(threadListMutex_);

    activeThreads_[managedThreadId] = osThreadId;

    if (canaryThread_.isValid())
    {
        return;
    }

    auto nameIt = threadNames_.find(managedThreadId);
    if (nameIt == threadNames_.end() || !options_.IsCanaryThread(nameIt->second))
    {
        return;
    }

    canaryThread_ = CanaryThreadInfo{managedThreadId, osThreadId};
    captureCondVar_.notify_all();

    trace::Logger::Info("[OsThreadSuspensionPolicy] Canary thread designated via ThreadAssignedToOSThread - ManagedID=",
                        managedThreadId, ", NativeID=", osThreadId, ", Name=", nameIt->second);
}

CanaryThreadInfo OsThreadSuspensionPolicy::WaitForCanaryThread(std::chrono::milliseconds timeout)
{
    if (timeout.count() == 0)
    {
        timeout = CaptureOptions{}.probeTimeout;
    }
    trace::Logger::Debug("[OsThreadSuspensionPolicy] Waiting for canary thread (timeout=", timeout.count(), "ms)");
   
    CanaryThreadInfo canary;
    std::unique_lock<std::mutex> lock(threadListMutex_);

    bool result = captureCondVar_.wait_for(lock, timeout,
        [this]() { return stopRequested_.load() || canaryThread_.isValid(); });

    if (!result)
    {
        trace::Logger::Warn("[OsThreadSuspensionPolicy] Canary thread wait timed out after ",
                            timeout.count(), "ms");
        return canary;
    }

    canary = canaryThread_;

    trace::Logger::Debug("[OsThreadSuspensionPolicy] Canary thread ready - ManagedID=",
                         canary.managedId, ", NativeID=", canary.nativeId);

    return canary;
}

bool OsThreadSuspensionPolicy::SafetyProbe(const CanaryThreadInfo& canaryInfo)
{
    if (!safetyProbe_)
        return true;

    // Canary must be suspended so DoStackSnapshot can target it.
    ScopedThreadSuspend canaryThread(canaryInfo.nativeId);
    if (!canaryThread.IsValid())
    {
        trace::Logger::Error("[OsThreadSuspensionPolicy] SafetyProbe failed - unable to suspend canary. NativeID=",
                             canaryInfo.nativeId);
        return false;
    }

    DWORD64 probeRip = 0;
#if defined(_M_AMD64)
    probeRip = reinterpret_cast<DWORD64>(&ProbeFunction);
#endif
    return safetyProbe_->Run(canaryInfo.managedId, probeRip);
}
#endif // defined(_WIN32)
} // namespace ProfilerStackCapture

