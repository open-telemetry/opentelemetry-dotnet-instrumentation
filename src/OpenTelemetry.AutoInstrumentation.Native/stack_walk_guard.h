// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#pragma once

#if defined(_WIN32)

#include <chrono>
#include <condition_variable>
#include <cstdint>
#include <memory>
#include <mutex>
#include <thread>

#include "profiler_api.h"

namespace ProfilerStackCapture
{

// What the caller wants validated before performing a stack walk. The guard
// composes the requested checks into a fixed sequence executed by a single
// long-lived helper thread.
enum class ProbeKind : uint32_t
{
    None      = 0,
    HeapLock  = 1u << 0, // malloc(64) + free; covers all STL allocs
    Rtl       = 1u << 1, // RtlLookupFunctionEntry; x64 only
    CanaryDSS = 1u << 2, // DoStackSnapshotUnseeded on a canary thread
};

constexpr ProbeKind operator|(ProbeKind a, ProbeKind b)
{
    return static_cast<ProbeKind>(static_cast<uint32_t>(a) | static_cast<uint32_t>(b));
}
constexpr ProbeKind operator&(ProbeKind a, ProbeKind b)
{
    return static_cast<ProbeKind>(static_cast<uint32_t>(a) & static_cast<uint32_t>(b));
}
constexpr inline bool HasProbe(ProbeKind set, ProbeKind one)
{
    return (static_cast<uint32_t>(set) & static_cast<uint32_t>(one)) != 0;
}

// Convenience presets - centralize the matrix.
#if defined(_M_AMD64)
inline constexpr ProbeKind kClrNativeWalkProbes = ProbeKind::HeapLock | ProbeKind::Rtl;
inline constexpr ProbeKind kNetFxSeedlessProbes = ProbeKind::HeapLock | ProbeKind::Rtl | ProbeKind::CanaryDSS;
#else
inline constexpr ProbeKind kClrNativeWalkProbes = ProbeKind::None;                            // unused on x86
inline constexpr ProbeKind kNetFxSeedlessProbes = ProbeKind::HeapLock | ProbeKind::CanaryDSS; // no Rtl on x86
#endif
static_assert(!HasProbe(kClrNativeWalkProbes, ProbeKind::CanaryDSS), "CLR native walk must not request CanaryDSS");
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
// Idle has dual semantics: "ready for a new request" and "previous
// verdict published, waiting to be consumed". This is safe because
// ScheduleProbe() always precedes AwaitProbeResult(): when the latter
// runs, the only non-Stopping terminal state is Idle, and result_ is
// exactly the verdict from the round just scheduled.
//
// On AwaitProbeResult() timeout: abandon_=true, return false. State
// stays Running until the worker returns from the in-flight check. The
// worker then transitions Running -> Idle (publishing a false verdict
// because abandon_ is set). The next ScheduleProbe() sees Idle and
// proceeds.
//
// Synchronization primitives are std::mutex + std::condition_variable,
// which on MSVC/Win10 are SRWLOCK + CONDITION_VARIABLE (WaitOnAddress-based)
// and perform NO heap allocation, NO _LOCK_DEBUG acquisition, and NO
// ProcessLocksList CS on the wait path. Safe to use even when another
// thread in this DLL is suspended holding the CRT heap CS.
class StackWalkGuard
{
public:
    StackWalkGuard(IProfilerApi*             profilerApi,
                   std::chrono::milliseconds park_timeout,
                   std::chrono::milliseconds probe_timeout);
    ~StackWalkGuard();

    StackWalkGuard(const StackWalkGuard&)            = delete;
    StackWalkGuard& operator=(const StackWalkGuard&) = delete;

    // Phase 1. Stage a new probe request and wake the worker. Blocks up
    // to park_timeout if the worker is still finishing a previous
    // (possibly abandoned) probe. Safe to call with target threads
    // already suspended: this method only acquires mutex_ and signals
    // cv_ (SRWLOCK + WaitOnAddress on Win10+), and performs no heap or
    // STL container operations on the caller's thread. Returns false on
    // park_timeout or shutdown.
    bool ScheduleProbe(ProbeKind flags, ThreadID canary);

    // Phase 2. Wait up to probe_timeout for the worker's verdict.
    // Returns true iff every requested check completed successfully. On
    // timeout, sets the abandon flag and returns false; the worker will
    // exit its probe loop at the next check boundary (after the
    // currently blocked check, if any, unblocks). Safe to call with
    // target threads suspended (same rationale as ScheduleProbe: only
    // mutex_ + cv_ are touched on the caller's thread). The next
    // ScheduleProbe() may block until the worker fully returns to Idle.
    bool AwaitProbeResult();

    // True if the worker is Idle (i.e., ScheduleProbe() would not
    // block). Optional helper for diagnostics; not required for
    // correctness.
    bool IsIdle() const noexcept;

private:
    enum class State : uint8_t
    {
        Idle,      // ready for a new request OR verdict published & not yet consumed
        Scheduled, // caller has staged a request; worker hasn't picked up yet
        Running,   // worker is executing probe checks
        Stopping,  // shutdown in progress
    };

    void WorkerLoop();
    bool RunChecks(ProbeKind flags, ThreadID canary, const std::atomic<bool>*) noexcept;

    IProfilerApi*             api_;
    std::chrono::milliseconds park_timeout_;
    std::chrono::milliseconds probe_timeout_;

    mutable std::mutex      mutex_;
    std::condition_variable cv_;
    State                   state_ = State::Idle;

    // Latched request data, valid when state_ >= Scheduled.
    ProbeKind req_flags_  = ProbeKind::None;
    ThreadID  req_canary_ = 0;

    // Caller -> worker abandonment signal. Set when AwaitProbeResult()
    // times out so the worker won't publish a true verdict for the
    // abandoned round.
    bool abandon_ = false;

    // Worker -> caller verdict, valid when state_ == Idle following a
    // Running -> Idle transition.
    bool result_ = false;

    std::unique_ptr<std::thread> worker_;
};

} // namespace ProfilerStackCapture

#endif // defined(_WIN32)