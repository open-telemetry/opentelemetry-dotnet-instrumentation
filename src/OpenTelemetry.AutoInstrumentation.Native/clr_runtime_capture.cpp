// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#include "clr_runtime_capture.h"
#if defined(_WIN32) && defined(_M_AMD64)
#include "suspension_guards.h"
#endif
#include "logger.h"

namespace ProfilerStackCapture
{

ClrRuntimeCapture::ClrRuntimeCapture(IProfilerApi* profilerApi, std::chrono::milliseconds probeTimeout)
    : profilerApi_(profilerApi)
#if defined(_WIN32) && defined(_M_AMD64)
    , stackWalkGuard_(std::make_unique<StackWalkGuard>(profilerApi, probeTimeout, probeTimeout))
    , nativeWalk_(std::make_unique<SafeNativeWalkService>(profilerApi))
#endif
{
#if !defined(_WIN32) || !defined(_M_AMD64)
    (void)probeTimeout;
#endif
}

HRESULT ClrRuntimeCapture::SuspendRuntime()
{
    if (profilerApi_ == nullptr)
    {
        return E_FAIL;
    }

    HRESULT hr = profilerApi_->SuspendRuntime();
    if (FAILED(hr))
    {
        trace::Logger::Error("[ClrRuntimeCapture] SuspendRuntime failed. HRESULT=", trace::HResultStr(hr));
    }
    // No probes here: seedless DSS is fully shielded by SuspendRuntime.
    return hr;
}

void ClrRuntimeCapture::ResumeRuntime() noexcept
{
    if (profilerApi_ == nullptr)
    {
        return;
    }
    HRESULT hr = profilerApi_->ResumeRuntime();
    if (FAILED(hr))
    {
        trace::Logger::Error("[ClrRuntimeCapture] ResumeRuntime failed. HRESULT=", trace::HResultStr(hr));
    }
}

HRESULT ClrRuntimeCapture::CaptureStack(ThreadID managedThreadId, StackSnapshotCallbackContext* clientData)
{
    if (clientData == nullptr)
    {
        return E_INVALIDARG;
    }

    HRESULT hr = profilerApi_->DoStackSnapshotUnseeded(managedThreadId, clientData);
    if (SUCCEEDED(hr))
    {
        return hr;
    }

#if defined(_WIN32) && defined(_M_AMD64)
    trace::Logger::Debug("[ClrRuntimeCapture] Seedless DSS failed. ManagedID=", managedThreadId,
                         ", HRESULT=", trace::HResultStr(hr), " - attempting native walk fallback");

    DWORD   osThreadId = 0;
    HRESULT osHr       = profilerApi_->GetThreadInfo(managedThreadId, &osThreadId);
    if (FAILED(osHr) || osThreadId == 0)
    {
        trace::Logger::Debug("[ClrRuntimeCapture] GetThreadInfo failed for native walk. ManagedID=", managedThreadId,
                             ", HRESULT=", trace::HResultStr(osHr));
        return hr;
    }

    // Caller-site contract:
    //   ThreadGuard RAII lives inside try. On probe failure, throw
    //   ProbeResult; stack unwinding destroys ThreadGuard (resumes
    //   target) BEFORE the catch block executes. The catch block is
    //   then safe to allocate/log because no thread is suspended.
    try
    {
        ThreadGuard target(osThreadId);
        if (!target.IsAcquired())
            throw StackWalkGuard::ProbeResult::Failed;

        // Capture target CONTEXT locally. Hazard-free: GetThreadContext on a
        // suspended thread acquires no shared CS we care about.
        CONTEXT ctx{};
        ctx.ContextFlags = CONTEXT_FULL;
        if (!target.GetContext(ctx))
            throw StackWalkGuard::ProbeResult::Failed;

        // RTL frame-0 probe. On hazard, both ctx and frame0 are documented
        // as stale and MUST NOT be consumed.
        continuous_profiler::CapturedFrame frame0{};
        if (!stackWalkGuard_->ScheduleRtlFrame0Probe(&ctx, &frame0))
            throw StackWalkGuard::ProbeResult::Failed;

        if (auto result = stackWalkGuard_->AwaitRtlFrame0ProbeResult(); result != StackWalkGuard::ProbeResult::Success)
            throw result;

        // Target remains suspended for the duration of the walk. ThreadGuard
        // is the suspension contract by construction.
        return nativeWalk_->ContinueFromProbedFrame0(target, managedThreadId, ctx, frame0, clientData);
    }
    catch (StackWalkGuard::ProbeResult result)
    {
        // ThreadGuard destroyed by stack unwinding - target resumed.
        // Safe to allocate and log.
        trace::Logger::Debug("[ClrRuntimeCapture] Native walk abandoned. ManagedID=", managedThreadId,
                             ", OsID=", osThreadId, ", Reason=", StackWalkGuard::ProbeResultName(result));
        return hr;
    }
#else
    return hr;
#endif
}

} // namespace ProfilerStackCapture