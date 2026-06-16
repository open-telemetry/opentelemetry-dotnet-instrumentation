// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#include "netfx_runtime_capture.h"

#if defined(_WIN32)

#include "logger.h"
#include "suspension_guards.h"

namespace ProfilerStackCapture
{

NetFxRuntimeCapture::NetFxRuntimeCapture(IProfilerApi* profilerApi, const NetFxCaptureOptions& options)
    : profilerApi_(profilerApi)
    , options_(options)
    , stackWalkGuard_(std::make_unique<StackWalkGuard>(profilerApi, options.probeTimeout, options.probeTimeout))
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

HRESULT NetFxRuntimeCapture::CaptureStack(ThreadID managedThreadId, StackSnapshotCallbackContext* clientData)
{
    if (clientData == nullptr)
    {
        return E_INVALIDARG;
    }

    const CanarySnapshot canary = SnapshotCanary();
    if (!canary.IsValid())
    {
        // Canary not ready - expected only during startup. In continuous
        // profiling mode, failing the batch is acceptable; the next cycle
        // will capture stacks once the canary is elected.
        // TODO: permanently disable sampling if canary-not-ready persists
        // for more than 10 consecutive cycles.
        trace::Logger::Debug("[NetFxRuntimeCapture] No canary available; skipping. ManagedID=", managedThreadId);
        return E_FAIL;
    }
    if (canary.managedId == managedThreadId)
    {
        return S_FALSE; // never walk the canary itself
    }

    DWORD   osThreadId = 0;
    HRESULT hr         = profilerApi_->GetThreadInfo(managedThreadId, &osThreadId);
    if (FAILED(hr) || osThreadId == 0)
    {
        trace::Logger::Debug("[NetFxRuntimeCapture] GetThreadInfo failed. ManagedID=", managedThreadId,
                             ", HRESULT=", trace::HResultStr(hr));
        return FAILED(hr) ? hr : E_FAIL;
    }

    // Caller-site contract:
    //   Both ThreadGuards (target + canary) live inside try. On probe
    //   failure, throw ProbeResult; stack unwinding destroys both guards
    //   (resumes both threads) BEFORE the catch block executes. The catch
    //   block is then safe to allocate/log - no thread is suspended.
    try
    {
        ThreadGuard target(osThreadId);
        if (!target.IsAcquired())
            throw StackWalkGuard::ProbeResult::Failed;

        ThreadGuard canaryGuard(canary.osId);
        if (!canaryGuard.IsAcquired())
            throw StackWalkGuard::ProbeResult::Failed;

#if defined(_M_AMD64)
        // ----------------------------------------------------------------
        // x64 path: RTL frame-0 probe drives classification (managed vs
        // native). Probe IS the seed-discovery step:
        //   - Managed verdict -> ctx is unchanged and is the DSS seed.
        //   - Native verdict  -> frame0 holds composed frame-0; ctx has
        //                        been unwound to frame-1 by the probe.
        // Canary DSS probe runs AFTER the RTL probe so an RTL hazard
        // short-circuits without spending a canary round. Canary probe is
        // still mandatory before any DoStackSnapshot call - NetFx has no
        // runtime-suspension shield.
        // ----------------------------------------------------------------

        CONTEXT ctx{};
        ctx.ContextFlags = CONTEXT_FULL;
        if (!target.GetContext(ctx))
            throw StackWalkGuard::ProbeResult::Failed;

        // RTL frame-0 probe.
        continuous_profiler::CapturedFrame frame0{};
        if (!stackWalkGuard_->ScheduleRtlFrame0Probe(&ctx, &frame0))
            throw StackWalkGuard::ProbeResult::Failed;

        if (auto result = stackWalkGuard_->AwaitRtlFrame0ProbeResult(); result != StackWalkGuard::ProbeResult::Success)
            throw result;

        // Canary DSS probe. Certifies process-wide DSS health before any
        // DoStackSnapshot call downstream.
        if (!stackWalkGuard_->ScheduleDssProbe(canary.managedId))
            throw StackWalkGuard::ProbeResult::Failed;

        if (auto result = stackWalkGuard_->AwaitProbeResult(); result != StackWalkGuard::ProbeResult::Success)
            throw result;

        return nativeWalk_->ContinueFromProbedFrame0(target, managedThreadId, ctx, frame0, clientData);
#else
        // ----------------------------------------------------------------
        // x86 path: no RTL machinery. Canary DSS probe followed by
        // seedless DSS is the only safe option.
        // ----------------------------------------------------------------
        if (!stackWalkGuard_->ScheduleDssProbe(canary.managedId))
            throw StackWalkGuard::ProbeResult::Failed;

        if (auto result = stackWalkGuard_->AwaitProbeResult(); result != StackWalkGuard::ProbeResult::Success)
            throw result;

        return profilerApi_->DoStackSnapshotUnseeded(managedThreadId, clientData);
#endif
    }
    catch (StackWalkGuard::ProbeResult result)
    {
        // Both ThreadGuards destroyed by stack unwinding - target and
        // canary resumed. Safe to allocate and log.
        trace::Logger::Debug("[NetFxRuntimeCapture] Probe abandoned. ManagedID=", managedThreadId,
                             ", OsID=", osThreadId, ", CanaryOsID=", canary.osId,
                             ", Reason=", StackWalkGuard::ProbeResultName(result));
        return E_FAIL;
    }
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
        trace::Logger::Info("[NetFxRuntimeCapture] New canary elected. ManagedID=", tid, ", OsID=", it->second);
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

    std::wstring                threadName(name, cchName);
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
    trace::Logger::Info("[NetFxRuntimeCapture] Canary designated via NameChanged. ManagedID=", threadId,
                        ", OsID=", it->second, L", Name=", threadName);
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
    trace::Logger::Info("[NetFxRuntimeCapture] Canary designated via AssignedToOSThread. ManagedID=", managedThreadId,
                        ", OsID=", osThreadId);
}

} // namespace ProfilerStackCapture

#endif // defined(_WIN32)