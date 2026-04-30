// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#ifndef OTEL_NATIVE_STACK_WALKER_H_
#define OTEL_NATIVE_STACK_WALKER_H_

#include <memory>
#include <atomic>
#include <corhlpr.h>
#include <corprof.h>
#include "stack_capture_strategy.h"
#include "profiler_api.h"

namespace ProfilerStackCapture
{

/// @brief Platform-agnostic interface for native (non-managed) stack walking.
///
/// Implementations:
///   - RtlNativeStackWalker  (Win x64) - RTL unwind via RtlLookupFunctionEntry
///   - NullNativeStackWalker (all others) - no-op, returns E_NOTIMPL
///   - Future: Linux perf / libunwind implementation
class INativeStackWalker
{
public:
    virtual ~INativeStackWalker() = default;

    /// @brief Walk native frames for a thread identified by managed thread ID.
    /// The implementation is responsible for resolving the OS thread ID and
    /// suspending/resuming the thread as needed.
    /// Suitable for DotNet strategy where CLR is already suspended but OS
    /// thread suspend is still needed for GetThreadContext.
    virtual HRESULT WalkThread(IProfilerApi*                                     profilerApi,
                               ThreadID                                          managedThreadId,
                               ProfilerStackCapture::StackSnapshotCallbackContext* clientData)         = 0;
    virtual HRESULT WalkSuspendedThread(void*                                               suspendedThread,
                                        ProfilerStackCapture::StackSnapshotCallbackContext* clientData)
    = 0;

};

/// @brief No-op implementation for platforms without native stack walk support.
class NullNativeStackWalker : public INativeStackWalker
{
public:
    HRESULT WalkThread(IProfilerApi*,
                       ThreadID, ProfilerStackCapture::StackSnapshotCallbackContext*) override
    {
        return E_NOTIMPL;
    }
    HRESULT WalkSuspendedThread(void*, ProfilerStackCapture::StackSnapshotCallbackContext*) override
    {
        return E_NOTIMPL;
    }
};

/// @brief Creates the platform-appropriate native stack walker.
/// Win x64 -> RtlNativeStackWalker; everything else -> NullNativeStackWalker.
std::unique_ptr<INativeStackWalker> CreateNativeStackWalker();

} // namespace ProfilerStackCapture

#endif // OTEL_NATIVE_STACK_WALKER_H_