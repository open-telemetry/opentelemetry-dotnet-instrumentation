// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#ifndef OTEL_PROFILER_STACK_SAFETY_PROBE_H_
#define OTEL_PROFILER_STACK_SAFETY_PROBE_H_

#if defined(_WIN32)

#include <chrono>
#include <cstdint>
#include "invocation_queue.h"
#include "profiler_api.h"

namespace ProfilerStackCapture
{

enum class ProbeFlags : uint32_t
{
    None            = 0,
    HeapAlloc       = 0x1 << 0,
    RtlUnwind       = 0x1 << 1, // x64 only
    DoStackSnapshot = 0x1 << 2, // NetFx only
};

inline constexpr ProbeFlags operator|(ProbeFlags a, ProbeFlags b)
{
    return static_cast<ProbeFlags>(static_cast<uint32_t>(a) | static_cast<uint32_t>(b));
}
inline constexpr bool HasFlag(ProbeFlags flags, ProbeFlags test)
{
    return (static_cast<uint32_t>(flags) & static_cast<uint32_t>(test)) != 0;
}

#if defined(_M_AMD64)
constexpr ProbeFlags kClrSuspensionProbeFlags    = ProbeFlags::HeapAlloc | ProbeFlags::RtlUnwind;
constexpr ProbeFlags kThreadSuspensionProbeFlags = ProbeFlags::HeapAlloc | ProbeFlags::RtlUnwind | ProbeFlags::DoStackSnapshot;
constexpr ProbeFlags kNativeWalkProbeFlags       = ProbeFlags::HeapAlloc | ProbeFlags::RtlUnwind;
#else
constexpr ProbeFlags kThreadSuspensionProbeFlags = ProbeFlags::HeapAlloc | ProbeFlags::DoStackSnapshot;
#endif

/// @brief Runs a configurable set of safety probes on a SHARED background
///        worker (InvocationQueue) with a wall-clock timeout.
///
/// Borrowed dependencies (must outlive this object):
///   - IProfilerApi*   captured into the worker lambda
///   - InvocationQueue runs the lambda
///
/// Lifetime contract: the InvocationQueue's worker thread MUST be joined
/// (i.e. ~InvocationQueue MUST run) before the IProfilerApi instance this
/// probe references is destroyed.  UnifiedStackCaptureStrategy guarantees
/// this via member declaration order.
class StackSafetyProbe
{
public:
    StackSafetyProbe(IProfilerApi*             profilerApi,
                     InvocationQueue*          queue,
                     ProbeFlags                flags,
                     std::chrono::milliseconds probeTimeout);

    ~StackSafetyProbe() = default;

    StackSafetyProbe(const StackSafetyProbe&)            = delete;
    StackSafetyProbe& operator=(const StackSafetyProbe&) = delete;

    /// canaryManagedId only consulted when DoStackSnapshot flag is set.
    /// probeRip        only consulted when RtlUnwind       flag is set.
    bool Run(ThreadID canaryManagedId = 0, DWORD64 probeRip = 0);

private:

    IProfilerApi*             profilerApi_;
    InvocationQueue*          queue_;
    ProbeFlags                flags_;
    std::chrono::milliseconds probeTimeout_;
};

} // namespace ProfilerStackCapture

#endif // defined(_WIN32)
#endif // OTEL_PROFILER_STACK_SAFETY_PROBE_H_