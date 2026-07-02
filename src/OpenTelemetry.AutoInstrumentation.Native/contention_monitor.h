// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0
#pragma once

#include <chrono>
#include <cstdint>
#include <mutex>
#include <unordered_map>
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

    // Materialize current groups for rendering: one pass over waits_for_ bucketed by
    // lock id, joined with locks_[L].owner_os_tid. Under-lock snapshot.
    std::vector<ContentionGroup> SnapshotGroups();

    // Time-based GC. Deletes entries that are BOTH stale (now - last_mutation > max_idle)
    // AND drained (concurrency <= 1). Both conjuncts are required: stale-alone deletes
    // deadlocks; drained-alone churns recurring locks and loses their metrics.
    void SweepStaleLocks(std::chrono::steady_clock::duration max_idle);

    // DFS for a back-edge over T -> L -> owner(L), using waits_for_ + owner only;
    // requires persistence across scans (cycle_seen_scans) before reporting.
    std::vector<std::vector<uint64_t>> DetectDeadlocks();

    // Phase 2 driver for the periodic sampler tick: sweep stale locks, then return any
    // confirmed deadlock cycles. MUST NOT be called while holding graph_lock_ - the mutex
    // is non-recursive and both callees re-lock internally, so nesting would self-deadlock.
    std::vector<std::vector<uint64_t>> OnSamplerTick(std::chrono::steady_clock::duration max_idle);

private:
    std::mutex                              graph_lock_;
    std::unordered_map<uint64_t, uint64_t>  waits_for_; // Map 1: osTid -> lockId (T -> L)
    std::unordered_map<uint64_t, LockEntry> locks_;     // Map 2: lockId -> LockEntry (L -> owner + metrics)
};
} // namespace continuous_profiler