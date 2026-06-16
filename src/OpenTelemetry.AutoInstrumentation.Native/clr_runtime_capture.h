// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#ifndef OTEL_PROFILER_CLR_RUNTIME_CAPTURE_H_
#define OTEL_PROFILER_CLR_RUNTIME_CAPTURE_H_

// Cross-platform CLR (.NET Core / .NET 5+) runtime capture.
//
// Suspension is global, driven by the profiler API (SuspendRuntime /
// ResumeRuntime).  Under runtime suspension, seedless DSS is safe
// without further probing.
//
// On Windows x64 only, when seedless DSS fails (e.g. thread in native
// code with no managed frames on top), we attempt a native-walk
// fallback: suspend the target via ThreadGuard, run the RTL frame-0
// probe through StackWalkGuard (heap/STL gate + RtlLookupFunctionEntry +
// GetFunctionFromIP + loader-lock + RtlVirtualUnwind), then dispatch via
// SafeNativeWalkService (managed seed -> seeded DSS, or native frame-0 ->
// walk natively until managed boundary -> seeded DSS from there).
// No canary DSS is needed: SuspendRuntime already certifies DSS health.

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
    ~ClrRuntimeCapture() = default;

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