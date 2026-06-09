// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#ifndef OTEL_PROFILER_SAFE_NATIVE_WALK_SERVICE_H_
#define OTEL_PROFILER_SAFE_NATIVE_WALK_SERVICE_H_

#if defined(_WIN32) && defined(_M_AMD64)

#include <chrono>
#include <memory>
#include "profiler_api.h"
#include "native_symbol_resolver.h"
#include "suspension_guards.h"

namespace ProfilerStackCapture
{
/// @brief Result of a native stack walk that stopped upon hitting managed code.
struct NativeWalkResult
{
    HRESULT hr               = E_FAIL;
    bool    hasSeed          = false; // true if walk found a managed frame
    CONTEXT seedCtx          = {};    // CONTEXT at the managed frame boundary (valid if hasSeed)
    DWORD   nativeFrameCount = 0;
};

/// @brief Unified service for probed native stack walk with DSS seeded handoff.
///
/// Sequence:
///   1. Suspend target thread (or accept already-suspended handle)
///   2. Probe HeapAlloc + RtlUnwind locks
///   3. Walk native frames via RTL unwind until managed frame hit
///   4. If managed frame found: DSS seeded with captured CONTEXT
///   5. Resume thread
///
/// Owns: StackSafetyProbe, RTL walk internals
/// Does NOT own: IProfilerApi (borrowed)
class SafeNativeWalkService
{
public:
    explicit SafeNativeWalkService(IProfilerApi*             profilerApi);
    ~SafeNativeWalkService();

    SafeNativeWalkService(const SafeNativeWalkService&)            = delete;
    SafeNativeWalkService& operator=(const SafeNativeWalkService&) = delete;

   
    ///Hybrid stack capture: probed native RTL walk until a managed
    ///        boundary is found, then seeded DoStackSnapshot from that
    ///        CONTEXT. If no managed frame is reached, the walk terminates
    ///        with the native frames already emitted (bounded by stack
    ///        depth). Seed is discovered internally - callers must not
    ///        attempt to pre-fetch or pass one in.
    ///  The thread must already be suspended by the caller, and the handle passed in must 
    ///  be valid (safety of the walk relies on the thread remaining suspended for the duration of the call). This is
    ///  used by the CLR capture implementation, which performs its own suspension and needs to control the exact point
    ///  of capture to properly seed DSS.
    HRESULT CaptureNativeThenSeededDss(ThreadGuard&                        threadGuard,
                                          ThreadID                      managedThreadId,
                                          StackSnapshotCallbackContext* clientData);

    /// @brief Access symbol resolver for name lookup during frame rendering.
    INativeSymbolResolver& GetSymbolResolver();

private:
    IProfilerApi*                     profilerApi_;

   
    /// @brief Walk native frames starting from the given context, stopping when
    ///        a managed frame is found. Emits native frames via clientData callback.
    NativeWalkResult WalkNativeUntilManaged(const CONTEXT&                initialCtx,
                                            StackSnapshotCallbackContext* clientData);
};

} // namespace ProfilerStackCapture

#endif // defined(_WIN32) && defined(_M_AMD64)
#endif // OTEL_PROFILER_SAFE_NATIVE_WALK_SERVICE_H_