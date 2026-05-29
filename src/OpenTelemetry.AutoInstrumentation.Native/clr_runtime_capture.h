// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#ifndef OTEL_PROFILER_CLR_RUNTIME_CAPTURE_H_
#define OTEL_PROFILER_CLR_RUNTIME_CAPTURE_H_

// Cross-platform CLR (.NET Core / .NET 5+) runtime capture.
//
// Suspension is global, driven by the profiler API.  Seedless DSS is
// shielded by SuspendRuntime - NO probes are required.
//
// On Windows x64 only, when seedless DSS fails we attempt a native-walk
// fallback that uses RtlVirtualUnwind to walk until a managed IP, then
// seeded DSS from there.  That path leaves CLR's safety envelope and
// is gated by HeapLock + Rtl probes via SafetyProber.

#include <chrono>
#include <memory>

#include "runtime_capture.h"

#if defined(_WIN32) && defined(_M_AMD64)
#include "safe_native_walk_service.h"
#include "stack_walk_guard.h"
#endif

namespace ProfilerStackCapture
{

class ClrRuntimeCapture final : public IRuntimeCapture
{
public:
    explicit ClrRuntimeCapture(IProfilerApi*             profilerApi,
                               std::chrono::milliseconds probeTimeout = std::chrono::milliseconds(250));

    HRESULT SuspendRuntime() override;
    void    ResumeRuntime() noexcept override;

    HRESULT CaptureStack(ThreadID                      managedThreadId,
                         StackSnapshotCallbackContext* clientData) override;

private:
    IProfilerApi* profilerApi_;

#if defined(_WIN32) && defined(_M_AMD64)
    std::unique_ptr<StackWalkGuard> stackWalkGuard_;
    std::unique_ptr<SafeNativeWalkService> nativeWalk_;
#endif
};

} // namespace ProfilerStackCapture

#endif // OTEL_PROFILER_CLR_RUNTIME_CAPTURE_H_