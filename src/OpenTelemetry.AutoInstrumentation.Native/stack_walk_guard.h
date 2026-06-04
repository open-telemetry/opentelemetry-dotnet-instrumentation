// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#pragma once

#if defined(_WIN32)

#include <atomic>
#include <chrono>
#include <condition_variable>
#include <cstdint>
#include <memory>
#include <mutex>
#include <thread>

#include "profiler_api.h"
#include "stack_capture_types.h"

namespace ProfilerStackCapture
{

// Park-then-pump safety gate for stack-walk operations, implemented as a
// fixed state machine with one dedicated worker thread.
//
// Rationale:
//   The caller will suspend an arbitrary target thread (and, for the DSS
//   check, a canary thread) at an unknown point. The suspended thread may
//   hold global critical sections used by malloc, STL debug iterators, the
//   RTL function-table lookup, or CLR internals reached by DoStackSnapshot.
//   We must determine whether those CSes are currently acquirable WITHOUT
//   acquiring them on the suspending thread itself (which would deadlock).
//
//   This guard parks a worker thread that, on signal, attempts each probe
//   operation in turn. If a probe blocks because its CS is held by the
//   suspended thread, the caller's probe_timeout fires; the caller sets the
//   abandon flag, resumes the suspended thread, and treats the round as
//   unsafe. The worker eventually unblocks, observes the abandon flag, and
//   returns to Idle without publishing a stale verdict.
//
// State machine (all transitions guarded by mutex_):
//
//   Idle --ScheduleProbe()--> Scheduled --worker observes--> Running
//     ^                                                        |
//     |                worker writes result, state=Idle        |
//     +--------------------------------------------------------+
//                                |
//          AwaitProbeResult() reads result while state==Idle
//
// Footprint scoping by architecture:
//   - CanaryDss path (STL gate + canary DSS) compiles on ALL Windows
//     targets (x86 + x64). Used by NetFx on both archs and by CLR's
//     native-walk preflight on x64 (canary == 0 path).
//   - RtlFrame0 path (frame-0 RTL composition + loader-lock probe)
//     compiles ONLY on Windows x64. RtlVirtualUnwind /
//     RtlLookupFunctionEntry have no x86 equivalent, so the entire RTL
//     machinery - public API methods, ProbeKind::RtlFrame0 enum value,
//     ProbeRequest staging fields, RunRtlFrame0Checks - is gated by
//     `#if defined(_M_AMD64)` so x86 incurs zero footprint and zero
//     binary weight from a feature it cannot use.
//
// Probe taxonomy:
//   The worker ALWAYS runs the STL gate first (compulsory, hardcoded:
//   heap CS + STL iterator/debug-list CS acquirability). Cooperative -
//   if abandon_ is set, the worker bails before further work. After the
//   STL gate passes, the worker dispatches on req.kind:
//
//     CanaryDss  - DoStackSnapshot on a separate "coast clear" thread;
//                  certifies process-wide DSS health for callers without
//                  a runtime-suspension shield (NetFx).
//
//     RtlFrame0  - (x64 only) Cooperative native frame-0 composition on
//                  the target thread. Walks RtlLookupFunctionEntry +
//                  classification + loader-lock probe + (if native)
//                  RtlVirtualUnwind, checkpointing abandon_ at each
//                  loader/RTL/CLR CS boundary. On success the worker
//                  stages the decoded frame and the unwound (frame-1)
//                  CONTEXT in ProbeRequest; the orchestrator publishes
//                  them under mutex_ to the caller's *ctx_inout and
//                  *frame_out iff !abandon_.
class StackWalkGuard
{
public:
    enum class ProbeResult : uint8_t
    {
        Success = 0,
        Failed,
        Stopping,

        HeapOrStlLockTimeout,
        CanaryDssTimeout,
#if defined(_M_AMD64)
        RtlLookupTimeout,
        ClrIpMapTimeout,
        LoaderLockTimeout,
        RtlUnwindTimeout,
#endif
    };

    static const char* ProbeResultName(ProbeResult result) noexcept;

    StackWalkGuard(IProfilerApi*             profilerApi,
                   std::chrono::milliseconds park_timeout,
                   std::chrono::milliseconds probe_timeout);
    ~StackWalkGuard();

    StackWalkGuard(const StackWalkGuard&)            = delete;
    StackWalkGuard& operator=(const StackWalkGuard&) = delete;

    // CanaryDss: STL gate + (if canary != 0) DSS on a coast-clear thread.
    // canary == 0 reduces to STL-only.
    // Available on all Windows architectures.
    bool ScheduleDssProbe(ThreadID canary = 0);

    // Shared verdict path. Returns Success iff every requested check
    // completed successfully. On timeout sets the abandon flag and returns
    // a timeout result mapped from the worker's last published probe stage.
    ProbeResult AwaitProbeResult();

    // True if the worker is Idle (diagnostic).
    bool IsIdle() const noexcept;

#if defined(_M_AMD64)
    // RtlFrame0 probe (x64 only).
    //
    // Contract:
    //   ScheduleRtlFrame0Probe + AwaitRtlFrame0ProbeResult != Success:
    //       hazard / abandoned / failed. *ctx and *frame are NOT written.
    //       Caller must abort the round.
    //   ScheduleRtlFrame0Probe + AwaitRtlFrame0ProbeResult == Success:
    //       *frame holds decoded frame-0.
    //       *ctx holds either the managed seed context or the unwound
    //       frame-1 context for native continuation.
    bool ScheduleRtlFrame0Probe(CONTEXT* ctx, continuous_profiler::CapturedFrame* frame);

    // Alias for symmetry; identical to AwaitProbeResult().
    ProbeResult AwaitRtlFrame0ProbeResult();
#endif // defined(_M_AMD64)

private:
    enum class State : uint8_t
    {
        Idle,      // ready for a new request OR verdict published & not yet consumed
        Scheduled, // caller has staged a request; worker hasn't picked up yet
        Running,   // worker is executing probe checks
        Stopping,  // shutdown in progress
    };

    enum class ProbeKind : uint8_t
    {
        None,      // no request staged
        CanaryDss, // STL gate + optional canary DSS
#if defined(_M_AMD64)
        RtlFrame0, // STL gate + cooperative RTL frame-0 walk (x64 only)
#endif
    };

    // Zero-alloc work item handed from the scheduling thread to the
    // worker. All members are scalars / PODs / NON-OWNING pointers, so
    // the latch copy in Schedule() is a trivially-copyable memcpy with
    // no allocation on the (suspending) caller thread.
    struct ProbeRequest
    {
        ProbeKind kind = ProbeKind::None;

        // CanaryDss payload.
        ThreadID canary = 0;

#if defined(_M_AMD64)
        // RtlFrame0 payload (x64 only, borrowed, non-owning).
        // The worker READS *ctx_inout exactly once at the start of the
        // probe (caller is blocked in Await throughout, so the read is
        // race-free). The worker NEVER writes through these pointers.
        // The orchestrator's publish step writes through them under
        // mutex_, ONLY when the probe succeeds AND !abandon_, copying
        // from the staged_* fields below.
        CONTEXT*                            ctx_inout = nullptr;
        continuous_profiler::CapturedFrame* frame_out = nullptr;

        // RtlFrame0 worker-owned staging area (x64 only). The worker
        // copies *ctx_inout into staged_ctx at probe start and operates
        // on staged_ctx exclusively (RtlVirtualUnwind mutates in place).
        // The staged_* fields are committed to *ctx_inout / *frame_out
        // by the orchestrator under mutex_ iff the probe succeeds.
        CONTEXT                            staged_ctx   = {};
        continuous_profiler::CapturedFrame staged_frame = {};
#endif // defined(_M_AMD64)
    };

    enum class ProbeStage : uint8_t
    {
        None,
        HeapOrStlGate,
        CanaryDss,
#if defined(_M_AMD64)
        RtlLookup,
        ClrIpMap,
        LoaderLock,
        RtlUnwind,
#endif
    };

    // Common park-then-latch transport shared by both ScheduleXxx() entry
    // points. Copies req by value under mutex_; no allocation on the
    // scheduling (suspending) thread.
    bool Schedule(const ProbeRequest& req);

    void WorkerLoop();
    bool RunChecks(ProbeRequest& req) noexcept;          // dispatch on req.kind; may stage outputs
    bool RunCanaryChecks(ThreadID canary) noexcept;      // heap envelope + optional canary DSS
#if defined(_M_AMD64)
    bool RunRtlFrame0Checks(ProbeRequest& req) noexcept; // cooperative RTL frame-0 (x64 only)
#endif

    void SetStage(ProbeStage stage) noexcept
    {
        active_stage_.store(stage, std::memory_order_relaxed);
    }

    ProbeStage GetStage() const noexcept
    {
        return active_stage_.load(std::memory_order_relaxed);
    }

    static ProbeResult TimeoutResultForStage(ProbeStage stage) noexcept;

    IProfilerApi*             api_;
    std::chrono::milliseconds park_timeout_;
    std::chrono::milliseconds probe_timeout_;

    mutable std::mutex      mutex_;
    std::condition_variable cv_;
    State                   state_ = State::Idle;

    ProbeRequest req_;

    bool abandon_ = false;

    // Final verdict. Written under mutex_ by AwaitProbeResult() on timeout
    // or by WorkerLoop() on normal completion.
    ProbeResult result_ = ProbeResult::Failed;

    // Worker-stage breadcrumb used only to classify timeout results.
    std::atomic<ProbeStage> active_stage_{ProbeStage::None};

    std::unique_ptr<std::thread> worker_;
};

} // namespace ProfilerStackCapture

#endif // defined(_WIN32)