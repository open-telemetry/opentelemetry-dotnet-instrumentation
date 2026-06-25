// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#ifndef OTEL_PROFILER_SAFE_NATIVE_WALK_SERVICE_H_
#define OTEL_PROFILER_SAFE_NATIVE_WALK_SERVICE_H_

#if defined(_WIN32) && defined(_M_AMD64)

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

/// @brief Post-probe native walk + seeded-DSS handoff. Stateless helper
///        invoked by IRuntimeCapture orchestrators once the RTL frame-0
///        probe in StackWalkGuard has succeeded.
///
/// Responsibility scope:
///   - Dispatch the probe verdict (managed seed vs native frame-0).
///   - Walk native frames until a managed boundary is hit.
///   - Hand off to seeded DoStackSnapshot at the boundary.
///
/// Out of scope (owned by the caller):
///   - Target suspension (ThreadGuard, compile-time contract on every entry).
///   - Hazard probing (StackWalkGuard, runs before this service is called).
///   - Resume (ThreadGuard RAII).
///
/// Owns: nothing.
/// Borrows: IProfilerApi, NativeSymbolResolver singleton.
class SafeNativeWalkService
{
public:
    explicit SafeNativeWalkService(IProfilerApi* profilerApi);
    ~SafeNativeWalkService();

    SafeNativeWalkService(const SafeNativeWalkService&)            = delete;
    SafeNativeWalkService& operator=(const SafeNativeWalkService&) = delete;

    /// @brief Dispatch a successful RTL frame-0 probe to the appropriate
    ///        capture path; sole public entry post-probe.
    ///
    /// Pre: StackWalkGuard::AwaitRtlFrame0ProbeResult() returned ProbeResult::Success,
    /// caller still holds target suspended via ThreadGuard, and ctx /
    /// frame0 were populated by the probe.
    ///
    /// Branches:
    ///   frame0.isUnmanagedFrame == false (managed seed):
    ///       ctx is the seed (probe did NOT unwind). Seeded DSS from
    ///       ctx, skip the native walk entirely.
    ///   frame0.isUnmanagedFrame == true (native frame-0):
    ///       Emit frame0 via callback, then walk natively from ctx
    ///       (which the probe unwound to frame-1). On hitting managed
    ///       code, switch to seeded DSS from that boundary.
    ///
    /// Stamps clientData->frame.threadId in both branches.
    ///
    /// @param threadGuard       Compile-time suspension contract; body
    ///                          relies on caller's RAII for the full
    ///                          duration. Not otherwise consulted.
    /// @param managedThreadId   CLR ThreadID for DSS calls and frame
    ///                          stamping.
    /// @param ctx               Dual-semantic per frame0.isUnmanagedFrame
    ///                          (see Branches above).
    /// @param frame0            Probe-composed frame-0; dispatch selector.
    /// @param clientData        Caller's callback + frame slot. Mutated.
    HRESULT ContinueFromProbedFrame0(ThreadGuard&                              threadGuard,
                                     ThreadID                                  managedThreadId,
                                     const CONTEXT&                            ctx,
                                     const continuous_profiler::CapturedFrame& frame0,
                                     StackSnapshotCallbackContext*             clientData);

    /// @brief Access symbol resolver for name lookup during frame rendering.
    INativeSymbolResolver& GetSymbolResolver();

private:
    IProfilerApi* profilerApi_;

    /// @brief Issue seeded DoStackSnapshot and normalize the
    ///        caller-stopped result (CORPROF_E_STACKSNAPSHOT_ABORTED ->
    ///        S_OK). Sole place that issues seeded DSS - shared by the
    ///        managed-frame-0 fast path and the post-walk handoff.
    HRESULT IssueSeededDss(ThreadID managedThreadId, const CONTEXT& seedCtx,
                           StackSnapshotCallbackContext* clientData);

    /// @brief Walk native frames starting from the given context, stopping when
    ///        a managed frame is found. Emits native frames via clientData callback.
    NativeWalkResult WalkNativeUntilManaged(const CONTEXT&                initialCtx,
                                            StackSnapshotCallbackContext* clientData);
};

} // namespace ProfilerStackCapture

#endif // defined(_WIN32) && defined(_M_AMD64)
#endif // OTEL_PROFILER_SAFE_NATIVE_WALK_SERVICE_H_