// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#include "netfx_runtime_capture.h"

#if defined(_WIN32)

#include "logger.h"
#include "suspension_guards.h"
#include "stack_walk_guard.h"

namespace ProfilerStackCapture
{

NetFxRuntimeCapture::NetFxRuntimeCapture(IProfilerApi*              profilerApi,
                                         const NetFxCaptureOptions& options)
    : profilerApi_(profilerApi)
    , options_(options)
    , stackWalkGuard_(std::make_unique<StackWalkGuard>(profilerApi, options.probeTimeout, 
        options.probeTimeout
    ))
#if defined(_M_AMD64)
    , nativeWalk_(std::make_unique<SafeNativeWalkService>(profilerApi))
#endif
{
    trace::Logger::Info(L"[NetFxRuntimeCapture] Initialized with canary prefix: ", options_.canaryNamePrefix);
}

NetFxRuntimeCapture::~NetFxRuntimeCapture()
{
    Stop();
}

void NetFxRuntimeCapture::Stop()
{
    stopRequested_.store(true, std::memory_order_release);
}

CanarySnapshot NetFxRuntimeCapture::SnapshotCanary() const
{
    std::lock_guard<std::mutex> lock(mutex_);
    return canary_;
}

HRESULT NetFxRuntimeCapture::CaptureStack(ThreadID                      managedThreadId,
                                          StackSnapshotCallbackContext* clientData)
{
    if (clientData == nullptr)
    {
        return E_INVALIDARG;
    }

    const CanarySnapshot canary = SnapshotCanary();
    if (!canary.IsValid())
    {
        trace::Logger::Debug("[NetFxRuntimeCapture] No canary available; skipping. ManagedID=",
                             managedThreadId);
        return E_FAIL;
    }
    if (canary.managedId == managedThreadId)
    {
        return S_FALSE; // never walk the canary itself
    }

    DWORD osThreadId = 0;
    HRESULT hr = profilerApi_->GetThreadInfo(managedThreadId, &osThreadId);
    if (FAILED(hr) || osThreadId == 0)
    {
        trace::Logger::Debug("[NetFxRuntimeCapture] GetThreadInfo failed. ManagedID=", managedThreadId,
                             ", HRESULT=", trace::HResultStr(hr));
        return FAILED(hr) ? hr : E_FAIL;
    }

    // Anchor the lock-state invariant: target stays suspended for the
    // full probe + DSS + (optional) native walk sequence.
    ThreadGuard target(osThreadId);
    if (!target.IsAcquired())
    {
        trace::Logger::Debug("[NetFxRuntimeCapture] Failed to suspend target. ManagedID=", managedThreadId,
                             ", OsID=", osThreadId);
        return E_FAIL;
    }
    ThreadGuard canaryGuard(canary.osId);
    // Per-target probe set: HeapLock + CanaryDSS (+ Rtl on x64).
    if (!stackWalkGuard_->ScheduleProbe(kNetFxSeedlessProbes, canary.managedId))
    {
        trace::Logger::Debug("[NetFxRuntimeCapture] Failed to prepare probes. ManagedID=", managedThreadId,
                             ", OsID=", osThreadId);
        return E_FAIL;
    }
    if (!stackWalkGuard_->AwaitProbeResult())
    {
        trace::Logger::Debug("[NetFxRuntimeCapture] Probes failed for target. ManagedID=", managedThreadId,
                             ", OsID=", osThreadId);
        return E_FAIL;
    }

    clientData->frame.threadId = managedThreadId;
    hr = profilerApi_->DoStackSnapshotUnseeded(managedThreadId, clientData);
    if (SUCCEEDED(hr))
    {
        return hr;
    }

    trace::Logger::Debug("[NetFxRuntimeCapture] Seedless DSS failed. ManagedID=", managedThreadId,
                         ", HRESULT=", trace::HResultStr(hr), " - attempting native walk fallback");

#if defined(_M_AMD64)
    // Probes already certified for this suspended target; no need to re-run.
    if (nativeWalk_)
    {
        return nativeWalk_->CaptureNativeThenSeededDss(target.GetHandle(), managedThreadId, clientData);
    }
#endif
    return hr;
}

// ---------------------------------------------------------------------------
// Canary bookkeeping
// ---------------------------------------------------------------------------

void NetFxRuntimeCapture::ReelectCanaryLocked()
{
    canary_ = {};
    for (const auto& [tid, name] : threadNames_)
    {
        if (!options_.IsCanaryThreadName(name))
        {
            continue;
        }
        auto it = activeThreads_.find(tid);
        if (it == activeThreads_.end())
        {
            continue;
        }
        canary_ = CanarySnapshot{tid, it->second};
        trace::Logger::Info("[NetFxRuntimeCapture] New canary elected. ManagedID=", tid,
                            ", OsID=", it->second);
        return;
    }
}

void NetFxRuntimeCapture::OnThreadDestroyed(ThreadID threadId)
{
    std::lock_guard<std::mutex> lock(mutex_);

    activeThreads_.erase(threadId);
    threadNames_.erase(threadId);

    if (canary_.managedId != threadId)
    {
        return;
    }

    trace::Logger::Info("[NetFxRuntimeCapture] Canary destroyed. ManagedID=", threadId);
    ReelectCanaryLocked();
}

void NetFxRuntimeCapture::OnThreadNameChanged(ThreadID threadId, ULONG cchName, WCHAR name[])
{
    if (name == nullptr || cchName == 0)
    {
        return;
    }

    std::wstring threadName(name, cchName);
    std::lock_guard<std::mutex> lock(mutex_);

    threadNames_[threadId] = threadName;

    if (canary_.IsValid() || !options_.IsCanaryThreadName(threadName))
    {
        return;
    }

    auto it = activeThreads_.find(threadId);
    if (it == activeThreads_.end())
    {
        return;
    }

    canary_ = CanarySnapshot{threadId, it->second};
    trace::Logger::Info("[NetFxRuntimeCapture] Canary designated via NameChanged. ManagedID=",
                        threadId, ", OsID=", it->second, L", Name=", threadName);
}

void NetFxRuntimeCapture::OnThreadAssignedToOSThread(ThreadID managedThreadId, DWORD osThreadId)
{
    std::lock_guard<std::mutex> lock(mutex_);

    activeThreads_[managedThreadId] = osThreadId;

    if (canary_.IsValid())
    {
        return;
    }

    auto nameIt = threadNames_.find(managedThreadId);
    if (nameIt == threadNames_.end() || !options_.IsCanaryThreadName(nameIt->second))
    {
        return;
    }

    canary_ = CanarySnapshot{managedThreadId, osThreadId};
    trace::Logger::Info("[NetFxRuntimeCapture] Canary designated via AssignedToOSThread. ManagedID=",
                        managedThreadId, ", OsID=", osThreadId);
}

} // namespace ProfilerStackCapture

#endif // defined(_WIN32)