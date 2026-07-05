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

// -------- Phase 4a output structs (NEW: unified component projection) --------
//
// A ContentionComponent is one weakly connected component of the wait-for graph.
// Because the wait-for graph is a functional graph (each thread has out-degree <= 1
// via waits_for_[T] -> owner(waits_for_[T].lock)), each component contains AT MOST
// ONE cycle. This lets us render every "traffic jam" as ONE group:
//   - No cycle: pure convoy, roles = { LockWaiter, LockOwner }.
//   - Cycle present: deadlock + handles, roles = { Deadlocked, BlockedByDeadlock }.
// This dissolves the Phase 3 edge case where a thread could appear in multiple
// per-lock groups (owner of A, waiter of B): union-find puts it in exactly one group.
struct ContentionParticipant
{
    uint64_t os_tid;
    double   observed_wait_ns; // now - wait_start; 0.0 for a non-waiting owner
};

// Phase 4a+: one directed edge of a confirmed deadlock ring. Preserves the ordered
// wait-for relation that DetectDeadlocks() proves but that has_cycle (a bool) and the
// unordered `deadlocked` vector would otherwise discard. Per-edge semantics:
//   os_tid is BLOCKED acquiring lock_object_id, which is currently held by waits_on_os_tid.
// The edges form a CLOSED ring: for a cycle of length k, edge[i].waits_on_os_tid ==
// edge[(i+1) % k].os_tid, and edge[k-1] closes back onto edge[0].os_tid. This is the
// actionable deadlock witness - it names each thread, its successor, AND the exact lock
// whose acquisition order created the cycle, which is what a user needs to fix it.
struct DeadlockCycleEdge
{
    uint64_t os_tid;          // the blocked thread
    uint64_t waits_on_os_tid; // successor in the ring: current owner of lock_object_id
    uint64_t lock_object_id;  // the lock os_tid is blocked acquiring (held by waits_on_os_tid)
};

struct ContentionComponent
{
    // Component identity: min lock_object_id across all locks referenced within
    // this component. Preferred over min(tid) because locks outlive threads, giving
    // stable dashboard aggregation for pure convoys; cycles are permanent so both
    // keys are equally stable there.
    uint64_t component_key = 0;

    // Cycle presence classifies the whole component. True iff any member appears
    // in a confirmed deadlock cycle from DetectDeadlocks() this tick.
    bool has_cycle = false;

    // Cycle case (has_cycle == true): every member is a waiter in a functional
    // graph, so these two vectors partition all members.
    std::vector<ContentionParticipant> deadlocked;          // cycle members
    std::vector<ContentionParticipant> blocked_by_deadlock; // handle members

    // Ordered deadlock witness (populated iff has_cycle; empty otherwise). The SAME threads
    // appear in `deadlocked` above, but there in arbitrary union-find order for metrics;
    // here they are in true wait order as a closed ring, canonicalized to START at the
    // smallest os_tid so the chain is byte-stable across ticks (matching the min_tid tag
    // convention used for aggregation). A component's cycle is UNIQUE (functional graph:
    // out-degree <= 1), so one ring per component is exact - no vector-of-cycles needed.
    // Reconstructed locally in SnapshotContentionComponents by walking
    // successor = owner(waits_for_[t].lock) from the min cycle member until the ring closes;
    // this needs no extra input and no monitor signature change.
    std::vector<DeadlockCycleEdge> cycle_chain;

    // Convoy case (has_cycle == false):
    std::vector<ContentionParticipant> waiters; // members with wait edges
    std::vector<ContentionParticipant> owner;   // 0 or 1 non-waiting owner

    double max_observed_wait_ns     = 0.0;
    double accumulated_completed_ns = 0.0; // sum across all locks in component
};

// Combined sampler tick result. During Phase 4a A/B, BOTH old and new fields are
// populated; the sampler flips between them via a compile-time flag. Retire the
// old fields (stalled_groups, deadlock_cycles) in Phase 4c after validation.
struct SamplerTickResult
{
    std::vector<StalledGroup>          stalled_groups;        // A/B old path
    std::vector<std::vector<uint64_t>> deadlock_cycles;       // A/B old path
    std::vector<ContentionComponent>   contention_components; // Phase 4a NEW path
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

    // Phase 4a: Snapshot unified components of the wait-for graph. deadlock_cycle_tids
    // is the union of confirmed cycle members from THIS tick's DetectDeadlocks(); the
    // component is marked has_cycle iff any member appears in that set. min_stall
    // gates convoy components; cycle components always surface (confirmed cycle IS
    // the evidence). Owns graph_lock_.
    std::vector<ContentionComponent> SnapshotContentionComponents(
        std::chrono::steady_clock::duration min_stall, const std::unordered_set<uint64_t>& deadlock_cycle_tids);

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