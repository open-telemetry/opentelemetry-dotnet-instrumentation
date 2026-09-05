// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#ifndef OTEL_PROFILER_RUNTIME_CAPTURE_H_
#define OTEL_PROFILER_RUNTIME_CAPTURE_H_

// IRuntimeCapture: the runtime-flavor-specific seam used by the stack
// capture orchestrator.  Replaces ISuspensionPolicy.
//
// Each implementation owns its own per-thread story end-to-end; the
// orchestrator sees four lifecycle/capture verbs:
//   * SuspendRuntime / ResumeRuntime  (driven by RuntimeGuard)
//   * CaptureStack(managedTid, ctx)   (CLR or NetFx decides internally)
//   * OnThreadXxx                     (lifecycle hooks, default no-op)
//   * RequestShutdown / WaitForShutdown (terminal cancellation and join)

#include <memory>
#include <corhlpr.h>
#include <corprof.h>

#include "profiler_api.h"

namespace ProfilerStackCapture
{

class IRuntimeCapture
{
public:
    virtual ~IRuntimeCapture() = default;

    /// <summary>
    /// Bring the runtime to a state where one or more stacks can be walked.
    /// ClrRuntimeCapture: profiler-API SuspendRuntime (seedless DSS is safe
    ///   under runtime suspension; probes only run in the native-walk fallback).
    /// NetFxRuntimeCapture: S_OK (suspension is per-thread, done in CaptureStack).
    /// </summary>
    virtual HRESULT SuspendRuntime() = 0;

    /// <summary>
    /// Reverse SuspendRuntime.  Must be safe to call only if SuspendRuntime
    /// succeeded (RuntimeGuard enforces this).
    /// </summary>
    virtual void ResumeRuntime() noexcept = 0;

    /// <summary>
    /// Capture exactly one managed thread's stack.  Implementation handles
    /// thread resolution, per-thread suspension, safety checks, and the
    /// DSS (or future native-walk fallback) internally.
    /// </summary>
    virtual HRESULT CaptureStack(ThreadID managedThreadId, StackSnapshotCallbackContext* clientData) = 0;

    // Lifecycle notifications routed from ICorProfilerCallback.
    // Default no-op; NetFxRuntimeCapture overrides for canary tracking.
    virtual void OnThreadCreated(ThreadID /*threadId*/) {}
    virtual void OnThreadDestroyed(ThreadID /*threadId*/) {}
    virtual void OnThreadNameChanged(ThreadID /*threadId*/, ULONG /*cchName*/, WCHAR /*name*/[]) {}
    virtual void OnThreadAssignedToOSThread(ThreadID /*managedThreadId*/, DWORD /*osThreadId*/) {}

    /// <summary>
    /// Terminal shutdown request. Implementations must close runtime-specific
    /// capture admission and release waiters without blocking.
    /// </summary>
    virtual void RequestShutdown() noexcept = 0;

    /// <summary>
    /// Waits for runtime-specific capture workers to finish after RequestShutdown.
    /// </summary>
    virtual void WaitForShutdown() noexcept = 0;
};

} // namespace ProfilerStackCapture

#endif // OTEL_PROFILER_RUNTIME_CAPTURE_H_