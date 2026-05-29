// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#include "clr_runtime_capture.h"
#if defined(_WIN32) && defined(_M_AMD64)
#include "suspension_guards.h"
#endif
#include "logger.h"

namespace ProfilerStackCapture
{

ClrRuntimeCapture::ClrRuntimeCapture(IProfilerApi*             profilerApi,
                                     std::chrono::milliseconds probeTimeout)
    : profilerApi_(profilerApi)
#if defined(_WIN32) && defined(_M_AMD64)
    , stackWalkGuard_(std::make_unique<StackWalkGuard>(profilerApi, probeTimeout, 
        probeTimeout))
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

HRESULT ClrRuntimeCapture::CaptureStack(ThreadID                      managedThreadId,
                                        StackSnapshotCallbackContext* clientData)
{
    if (clientData == nullptr)
    {
        return E_INVALIDARG;
    }

    clientData->frame.threadId = managedThreadId;

    HRESULT hr = profilerApi_->DoStackSnapshotUnseeded(managedThreadId, clientData);
    if (SUCCEEDED(hr))
    {
        return hr;
    }

    trace::Logger::Debug("[ClrRuntimeCapture] Seedless DSS failed. ManagedID=", managedThreadId,
                         ", HRESULT=", trace::HResultStr(hr), " - attempting native walk fallback");

#if defined(_WIN32) && defined(_M_AMD64)
    DWORD   osThreadId = 0;
    HRESULT osHr       = profilerApi_->GetThreadInfo(managedThreadId, &osThreadId);
    if (FAILED(osHr) || osThreadId == 0)
    {
        trace::Logger::Debug("[ClrRuntimeCapture] GetThreadInfo failed for native walk. ManagedID=", managedThreadId,
                             ", HRESULT=", trace::HResultStr(osHr));
        return hr;
    }
    ThreadGuard target(osThreadId);
    if (!target.IsAcquired())
    {
        trace::Logger::Debug("[ClrRuntimeCapture] Failed to suspend target for native walk. ManagedID=", managedThreadId,
                             ", OsID=", osThreadId);
        return hr;
    }
    // We are leaving CLR's safety envelope (going to RtlVirtualUnwind).
    // Gate the native walk on HeapLock + Rtl probes for THIS target's
    // lock state.  No CanaryDSS - DSS itself is still CLR-shielded.
    if (!stackWalkGuard_->ScheduleProbe(kClrNativeWalkProbes,0))
    {
        trace::Logger::Debug("[ClrRuntimeCapture] Native walk probes failed. ManagedID=", managedThreadId);
        return hr;
    }
    if (!stackWalkGuard_->AwaitProbeResult())
    {
        trace::Logger::Debug("[ClrRuntimeCapture] Native walk probes failed (abandoned). ManagedID=", managedThreadId);
        return hr;
    }
    
    return nativeWalk_->CaptureNativeThenSeededDss(target.GetHandle(), managedThreadId, clientData);
#else
    return hr;
#endif
}

} // namespace ProfilerStackCapture