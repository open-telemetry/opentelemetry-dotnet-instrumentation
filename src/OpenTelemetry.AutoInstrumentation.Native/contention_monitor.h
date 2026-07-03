// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0
#pragma once

#include <chrono>
#include <cstdint>
#include <mutex>
#include <unordered_map>
#include <unordered_set>
#include <vector>

namespace continuous_profiler
{
// Map 2 value (lockId -> LockEntry). Holds only what CANNOT be derived from
// waits_for_. The per-lock contender list is the inverse index of waits_for_
// ({ T : waits_for_[T] == L }) and is materialized on demand in SnapshotGroups,
// never stored.
struct LockEntry
{
    // L -> T (last known owner). Irreducible: owner is NOT derivable from
    // waits_for_, which records waiters, not owners.
    uint64_t owner_os_tid = 0;

    // Participant count: (owner ? 1 : 0) + number of current waiters on L.
    // Invariant while owned: concurrency == 1 + |{ T : waits_for_[T] == L }|,
    // i.e. a cache of "1 + inverse-index size".
    // Maintained at EXACTLY three sites:
    //   - OnContentionStart: new entry -> seed 2 (owner + first contender);
    //                        existing entry -> ++concurrency.
    //   - OnContentionStop:  --concurrency (after reverse-resolving the lock).
    //   - OnThreadDestroyed: --concurrency for a waiter that died (missed-Stop backstop).
    // The +1 for the owner is load-bearing: it keeps a single-waiter DEADLOCK at
    // concurrency==2 (retained) while a resolved lock drains to 1 (collectible).
    uint32_t concurrency = 0;

    // Tick-accumulated "observed stuck time" for ongoing contention/deadlock.
    // NOTE: distinct from the ContentionStop payload DurationNs (sum of COMPLETED
    // episodes). Do not conflate the two in one field if convoy stats are added later.
    double   accumulated_duration_ns = 0.0;

    uint32_t cycle_seen_scans = 0; // persistence debounce for deadlock reporting

    std::chrono::steady_clock::time_point first_seen{};
    std::chrono::steady_clock::time_point last_mutation{}; // drives the stale gate
};

// On-demand materialized view for rendering (phase 2). The contender vector is
// built by one pass over waits_for_; it lives only in the snapshot.
struct ContentionGroup
{
    uint64_t              lock_object_id = 0;
    uint64_t              owner_os_tid   = 0;
    std::vector<uint64_t> contender_os_tids;
    double                accumulated_duration_ns = 0.0;
};

// Phase 3: Per-waiter wait edge with clock start time. The stall clock MUST be
// per-waiter (not lock-level) to avoid hot-lock false positives: a lock contended
// thousands of times with each wait < min_stall must not surface as a stall.
struct WaitEdge
{
    uint64_t                              lock_object_id;
    std::chrono::steady_clock::time_point wait_start; // when THIS waiter began waiting
};

// Phase 3: A waiter observed to be stalled at snapshot time.
struct StalledWaiter
{
    uint64_t os_tid;
    double   observed_wait_ns; // now - wait_start
};

// Phase 3: A group of waiters stalled on one lock, with convoy severity metrics.
struct StalledGroup
{
    uint64_t                   lock_object_id;
    uint64_t                   owner_os_tid;            // best-effort, may be 0
    std::vector<StalledWaiter> waiters;
    double                     max_observed_wait_ns;
    double                     accumulated_completed_ns; // LockEntry.accumulated_duration_ns
    bool                       is_deadlock = false;      // dedup: set by OnSamplerTick cross-ref
};

// Phase 3: Combined sampler tick result. Both rendering paths coexist:
// - Deadlock path (UNCHANGED): renders confirmed cycles keyed by min_tid.
// - Stall path (NEW): renders non-deadlock stalled groups keyed by lock_object_id.
// is_deadlock dedups the two so each lock is rendered exactly once.
struct SamplerTickResult
{
    std::vector<StalledGroup>          stalled_groups;  // rendered by ProjectStalledGroups (skips is_deadlock)
    std::vector<std::vector<uint64_t>> deadlock_cycles; // rendered by ProjectDeadlockCycles (UNCHANGED)
};

// Self-contained. Owns its own mutex. Never acquires sampler/suspension locks.
class ContentionMonitor
{
public:
    // 81 ContentionStart, on the contender thread. owner_os_tid from payload.LockOwnerThreadID.
    // New lock -> concurrency seeded to 2; existing -> ++concurrency.
    void OnContentionStart(uint64_t contender_os_tid, uint64_t owner_os_tid, uint64_t lock_object_id);

    // 91 ContentionStop, on the contender thread. Lock id is NOT in the Stop payload;
    // recovered by reverse-resolving contender_os_tid through waits_for_, then a single
    // erase, --concurrency, owner transfer, and duration accumulation.
    void OnContentionStop(uint64_t contender_os_tid, double duration_ns);

    // MANDATORY missed-Stop backstop: decrement the waited-on lock and erase the edge.
    void OnThreadDestroyed(uint64_t os_tid);

    // Phase 3: Snapshot all stalled groups (groups with max_observed_wait >= min_stall).
    // Under-lock. One pass over waits_for_ bucketing by lock_object_id; per waiter
    // observed = now - wait_start; keep group iff max_observed >= min_stall. No
    // persistence gate: elapsed wall-clock IS the evidence (self-validating).
    std::vector<StalledGroup> SnapshotStalledGroups(std::chrono::steady_clock::duration min_stall);

    // Time-based GC. Deletes entries that are BOTH stale (now - last_mutation > max_idle)
    // AND drained (concurrency <= 1). Both conjuncts are required: stale-alone deletes
    // deadlocks; drained-alone churns recurring locks and loses their metrics.
    void SweepStaleLocks(std::chrono::steady_clock::duration max_idle);

    // DFS for a back-edge over T -> L -> owner(L), using waits_for_ + owner only;
    // requires persistence across scans (cycle_seen_scans) before reporting.
    std::vector<std::vector<uint64_t>> DetectDeadlocks();

    // Phase 3 driver for the periodic sampler tick: sweep stale locks, detect deadlocks
    // (ONCE per tick), snapshot stalled groups, and cross-reference to set is_deadlock.
    // Returns BOTH rendering paths. MUST NOT be called while holding graph_lock_ - the
    // mutex is non-recursive and callees re-lock internally, so nesting would self-deadlock.
    SamplerTickResult OnSamplerTick(std::chrono::steady_clock::duration min_stall,
                                    std::chrono::steady_clock::duration max_idle);

private:
    // Derive the set of lock IDs participating in confirmed deadlock cycles. Used to
    // set is_deadlock on StalledGroups so the stall path can skip them (dedup). Locks
    // the graph. Called ONCE per tick after DetectDeadlocks; does NOT re-walk.
    std::unordered_set<uint64_t> LockIdsForCycles(const std::vector<std::vector<uint64_t>>& cycles);

    std::mutex                            graph_lock_;
    std::unordered_map<uint64_t, WaitEdge> waits_for_; // Map 1: osTid -> WaitEdge (T -> L + wait_start)
    std::unordered_map<uint64_t, LockEntry> locks_;     // Map 2: lockId -> LockEntry (L -> owner + metrics)
};
} // namespace continuous_profiler