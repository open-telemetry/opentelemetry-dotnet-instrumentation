// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#include "stack_walk_guard.h"

#if defined(_WIN32)

#include <cstdlib>
#include <vector>
#include "logger.h"

namespace ProfilerStackCapture
{

namespace
{
// Anchor used by the RTL lookup probe. noinline + side effect forces
// the compiler to emit a real function (and thus a .pdata entry on
// x64) that RtlLookupFunctionEntry can resolve.
__declspec(noinline) void RtlLookupAnchor() noexcept
{
    static volatile int sink = 0;
    ++sink;
}
} // namespace

StackWalkGuard::StackWalkGuard(IProfilerApi*             profilerApi,
                               std::chrono::milliseconds park_timeout,
                               std::chrono::milliseconds probe_timeout)
    : api_(profilerApi), park_timeout_(park_timeout), probe_timeout_(probe_timeout)
{
    worker_ = std::make_unique<std::thread>([this]() { WorkerLoop(); });
}

StackWalkGuard::~StackWalkGuard()
{
    {
        std::lock_guard<std::mutex> lk(mutex_);
        state_ = State::Stopping;
    }
    cv_.notify_all();
    if (worker_ && worker_->joinable())
        worker_->join();
    worker_.reset();
}

bool StackWalkGuard::IsIdle() const noexcept
{
    std::lock_guard<std::mutex> lk(mutex_);
    return state_ == State::Idle;
}

bool StackWalkGuard::ScheduleProbe(ProbeKind flags, ThreadID canary)
{
    if (HasProbe(flags, ProbeKind::CanaryDSS) && !canary)
    {
        trace::Logger::Debug("[StackWalkGuard] CanaryDSS requested but canary invalid");
        return false;
    }

    std::unique_lock<std::mutex> lk(mutex_);

    // Wait for the worker to be Idle. Idle covers both "fresh" and
    // "previous verdict published but not yet consumed" - either way the
    // worker is quiescent and any stale result_ is about to be overwritten
    // below. If the worker is still Running (e.g., draining an abandoned
    // previous round whose in-flight check has not returned yet), park
    // until it reaches Idle or park_timeout_ elapses.
    const bool ready =
        cv_.wait_for(lk, park_timeout_, [this] { return state_ == State::Idle || state_ == State::Stopping; });

    if (!ready || state_ == State::Stopping)
    {
        trace::Logger::Warn("[StackWalkGuard] ScheduleProbe: worker still busy; skip round");
        return false;
    }

    // Latch the request and hand off to the worker. Any stale verdict
    // sitting in result_ from an abandoned previous round is overwritten
    // here.
    req_flags_  = flags;
    req_canary_ = canary;
    abandon_    = false;
    result_     = false;
    state_      = State::Scheduled;
    cv_.notify_one();
    return true;
}

bool StackWalkGuard::AwaitProbeResult()
{
    std::unique_lock<std::mutex> lk(mutex_);

    // Wait for the worker to publish a verdict (state Running -> Idle) or
    // for shutdown. Note: when AwaitProbeResult() is entered, state_ is
    // one of {Scheduled, Running, Idle} - Idle here means the worker
    // raced ahead and already published, which we accept immediately.
    const bool done =
        cv_.wait_for(lk, probe_timeout_, [this] { return state_ == State::Idle || state_ == State::Stopping; });

    if (!done || state_ == State::Stopping)
    {
        // Timeout. Tell the worker to publish a false verdict when it
        // eventually unblocks. State stays Running until the worker
        // returns from the in-flight check; the next ScheduleProbe()
        // will block (up to park_timeout) for it to reach Idle.
        abandon_ = true;
        trace::Logger::Warn("[StackWalkGuard] AwaitProbeResult: composite probe timed out after ",
                            probe_timeout_.count(), "ms");
        return false;
    }

    // Verdict consumed. State is already Idle; no further transition
    // needed, but wake any ScheduleProbe() that may be parked on
    // park_timeout.
    const bool verdict = result_;
    cv_.notify_all();
    return verdict;
}

void StackWalkGuard::WorkerLoop()
{
    trace::Logger::Info("StackWalkGuard worker started");

    for (;;)
    {
        ProbeKind flags  = ProbeKind::None;
        ThreadID  canary = 0;

        // Wait for a new request (or shutdown).
        {
            std::unique_lock<std::mutex> lk(mutex_);
            cv_.wait(lk, [this] { return state_ == State::Scheduled || state_ == State::Stopping; });

            if (state_ == State::Stopping)
                break;

            flags    = req_flags_;
            canary   = req_canary_;
            abandon_ = false;
            state_   = State::Running;
        }

        // Run probes OUTSIDE the lock. The abandon flag is observed at
        // each check boundary; the in-flight check itself is not preempted
        // (a malloc on a held heap CS will block until the heap is
        // released).
        const bool ok = RunChecks(flags, canary);

        // Publish verdict and return to Idle. If the caller abandoned us,
        // publish false regardless of what the checks said.
        {
            std::lock_guard<std::mutex> lk(mutex_);
            if (state_ == State::Stopping)
                break;
            result_ = ok && !abandon_;
            state_  = State::Idle;
        }
        cv_.notify_all();

        // Loop back to the top, where we wait for the next Scheduled (or
        // Stopping). No separate post-publish wait is needed because Idle
        // already conveys "verdict ready" to the caller.
    }

    trace::Logger::Info("StackWalkGuard worker exiting");
}

bool StackWalkGuard::RunChecks(ProbeKind flags, ThreadID canary) noexcept
{
    // Snapshot abandon flag between checks. We re-read under the lock
    // because the worker thread is the only consumer and contention is
    // negligible (caller writes abandon_ at most once per round).
    auto abandoned = [this]() -> bool
    {
        std::lock_guard<std::mutex> lk(mutex_);
        return abandon_;
    };

    // 1. Heap CS acquirability. Also covers STL allocations because every
    //    STL container op routes through malloc -> CRT heap CS on /MT.
    if (HasProbe(flags, ProbeKind::HeapLock))
    {
        void* p = std::malloc(64);
        if (!p)
            return false;
        std::free(p);
        {
            std::vector<int> v{1, 2, 3}; // also covers STL debug iterators' CS usage
        }
        if (abandoned())
            return false;
    }

#if defined(_M_AMD64)
    // 2. RTL loader function-table CS acquirability. DSS and our native
    //    unwinder both call RtlLookupFunctionEntry internally.
    if (HasProbe(flags, ProbeKind::Rtl))
    {
        DWORD64           image_base = 0;
        const DWORD64     pc         = reinterpret_cast<DWORD64>(&RtlLookupAnchor);
        PRUNTIME_FUNCTION e          = ::RtlLookupFunctionEntry(pc, &image_base, nullptr);
        (void)e;
        (void)image_base;
        if (abandoned())
            return false;
    }
#endif

    // 3. DSS on the canary. Probe-only sink: abort on the very first frame.
    //    Reaching the callback at all proves DSS acquired the locks it
    //    needs (loader / RTL function table / CLR thread store) without
    //    deadlocking against any thread the caller suspended. Returning a
    //    non-S_OK HRESULT from the callback causes DoStackSnapshot to stop
    //    after frame 0 and return CORPROF_E_STACKSNAPSHOT_ABORTED, which
    //    we treat as a pass below.
    if (HasProbe(flags, ProbeKind::CanaryDSS))
    {
        StackSnapshotCallbackContext sink{};
        bool                         callback_fired = false;
        sink.callback                               = [&callback_fired](StackSnapshotCallbackContext* ctx) -> HRESULT
        {
            // Short-circuit: we only need proof that DSS got far enough to
            // invoke the callback for frame 0. Any non-S_OK return aborts.
            callback_fired = true;
            return S_FALSE;
        };

        HRESULT hr = E_FAIL;
        try
        {
            hr = api_->DoStackSnapshotUnseeded(canary, &sink);
        }
        catch (...)
        {
            trace::Logger::Warn("[StackWalkGuard] CanaryDSS threw");
            return false;
        }

        // Safe iff the callback actually fired AND DSS reported the expected
        // aborted-by-callback status. Treat CORPROF_E_STACKSNAPSHOT_UNSAFE
        // (and anything else) as a hard fail - that is exactly the signal we
        // are probing for.
        const bool dss_ok = callback_fired && hr == CORPROF_E_STACKSNAPSHOT_ABORTED;

        if (!dss_ok)
        {
            trace::Logger::Warn("[StackWalkGuard] CanaryDSS unsafe; hr=0x", std::hex, static_cast<unsigned>(hr));
            return false;
        }
        if (abandoned())
            return false;
    }

    return true;
}

} // namespace ProfilerStackCapture

#endif // defined(_WIN32)