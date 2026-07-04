// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#include "contention_monitor.h"

#include <cstdint>
#include "logger.h"
#include <unordered_map>
#include <unordered_set>
#include <utility>

namespace continuous_profiler
{

void ContentionMonitor::OnContentionStart(uint64_t contender_os_tid, uint64_t owner_os_tid, uint64_t lock_object_id)
{
    if (contender_os_tid == 0 || lock_object_id == 0)
    {
        // Nothing actionable without both a contender identity and a lock identity.
        return;
    }
    trace::Logger::Debug("OnContentionStart: contender=", contender_os_tid, " owner=", owner_os_tid,
                         " lock=", lock_object_id);

    std::lock_guard<std::mutex> guard(graph_lock_);
    const auto                  now = std::chrono::steady_clock::now();

    // Defensive reconciliation of a stale wait edge for this thread. Normally a
    // thread blocked on Monitor.Enter(A) cannot reach Monitor.Enter(B) until it
    // acquires A (which fires Stop A), so waits_for_[contender] is absent here.
    // It can survive only if Stop A was dropped (EventPipe buffer pressure). In
    // that case retire the old edge's bookkeeping so the overwrite below cannot
    // leak the old lock's count. This is the lazy equivalent of the missed Stop.
    // this is to build robustness against EventPipe buffer pressure and dropped events.
    // this maintain invariant : a thread can wait on at most one lock at a time, and a lock has at most one owner.
    // deadlock detection relies on this invariant, so we must maintain it even in the face of dropped events.
    bool       already_waiting_on_this_lock = false;
    const auto prior                        = waits_for_.find(contender_os_tid);
    if (prior != waits_for_.end())
    {
        if (prior->second.lock_object_id == lock_object_id)
        {
            already_waiting_on_this_lock = true; // duplicate Start; do not double-count
        }
        else
        {
            // no more waiting on the old lock: decrement its concurrency and retire the edge.
            const auto old_lock = locks_.find(prior->second.lock_object_id);
            if (old_lock != locks_.end() && old_lock->second.concurrency > 0)
            {
                old_lock->second.concurrency--;
                old_lock->second.last_mutation = now; // refresh last_mutation to avoid stale sweep
            }
        }
    }

    // Phase 3: Capture per-waiter wait_start. CRITICAL: if already_waiting_on_this_lock,
    // do NOT reset wait_start - that would erase a real long stall. Set wait_start ONLY
    // when the edge is newly created OR changes to a different lock.
    WaitEdge edge;
    edge.lock_object_id = lock_object_id;
    if (already_waiting_on_this_lock)
    {
        // Preserve existing wait_start to honor the original stall clock.
        edge.wait_start = prior->second.wait_start;
    }
    else
    {
        // New edge or changed lock: start the stall clock now.
        edge.wait_start = now;
    }
    waits_for_[contender_os_tid] = edge;

    const auto it = locks_.find(lock_object_id);
    if (it == locks_.end())
    {
        // New lock: seed 2 = owner + first contender. The +1 for the owner is
        // load-bearing: it keeps a single-waiter deadlock at concurrency 2
        // (retained by the sweep) while a resolved lock drains to 1 (collectible).
        LockEntry entry;
        entry.owner_os_tid  = owner_os_tid;
        entry.concurrency   = 2;
        entry.first_seen    = now;
        entry.last_mutation = now;
        locks_.emplace(lock_object_id, std::move(entry));
    }
    else
    {
        if (!already_waiting_on_this_lock)
        {
            it->second.concurrency++;
        }
        if (owner_os_tid != 0)
        {
            it->second.owner_os_tid = owner_os_tid; // refresh best-effort owner
        }
        it->second.last_mutation = now;
    }
}

void ContentionMonitor::OnContentionStop(uint64_t contender_os_tid, double duration_ns)
{
    if (contender_os_tid == 0)
    {
        return;
    }
    trace::Logger::Debug("OnContentionStop: contender=", contender_os_tid, " duration_ns=", duration_ns);
    std::lock_guard<std::mutex> guard(graph_lock_);
    const auto                  now = std::chrono::steady_clock::now();

    // The Stop payload carries no lock id; recover it by reverse-resolving the
    // contender through waits_for_. An absent edge means a missed Start or an
    // attach mid-contention - nothing to reconcile, and skipping the decrement
    // here is what makes the concurrency counter underflow-safe.
    const auto edge = waits_for_.find(contender_os_tid);
    if (edge == waits_for_.end())
    {
        return;
    }

    const uint64_t lock_object_id = edge->second.lock_object_id;
    waits_for_.erase(edge); // single erase: the contender is no longer waiting

    const auto it = locks_.find(lock_object_id);
    if (it != locks_.end())
    {
        if (it->second.concurrency > 0)
        {
            it->second.concurrency--;
        }

        // Best-effort owner transfer. For a blocking Monitor.Enter, Stop means the
        // contender acquired the lock, so it is the new owner. A TryEnter timeout
        // that gave up self-heals on the next ContentionStart, which re-asserts the
        // true owner from its payload.
        it->second.owner_os_tid = contender_os_tid;

        // Phase 1 metric: this is the ContentionStop payload DurationNs (sum of
        // COMPLETED episodes). It is intentionally a different quantity from the
        // tick-accumulated "observed stuck time" a periodic driver may add later.
        it->second.accumulated_duration_ns += duration_ns;
        // TODO Phase 3+: if duration_ns >= min_stall, bump a completed-stall counter

        it->second.last_mutation = now;
    }
}

void ContentionMonitor::OnThreadDestroyed(uint64_t os_tid)
{
    if (os_tid == 0)
    {
        return;
    }

    std::lock_guard<std::mutex> guard(graph_lock_);
    const auto                  now = std::chrono::steady_clock::now();

    // Mandatory missed-Stop backstop. A waiter that died while blocked would
    // otherwise leave its wait edge - and the lock's concurrency - stuck forever,
    // which neither the count gate nor the stale gate could ever collect. Retire
    // the edge and decrement. No owner transfer and no duration: a destroyed
    // waiter did not acquire the lock.
    const auto edge = waits_for_.find(os_tid);
    if (edge == waits_for_.end())
    {
        return;
    }

    const uint64_t lock_object_id = edge->second.lock_object_id;
    waits_for_.erase(edge);

    const auto it = locks_.find(lock_object_id);
    if (it != locks_.end())
    {
        if (it->second.concurrency > 0)
        {
            it->second.concurrency--;
        }
        it->second.last_mutation = now;
    }
}

std::vector<StalledGroup> ContentionMonitor::SnapshotStalledGroups(std::chrono::steady_clock::duration min_stall)
{
    std::lock_guard<std::mutex> guard(graph_lock_);
    const auto                  now = std::chrono::steady_clock::now();

    // One pass over waits_for_ bucketing by lock_object_id. Per waiter: compute
    // observed = now - wait_start; keep the group iff max_observed >= min_stall.
    // Compare in the duration domain (avoid float compares); store observed_wait_ns
    // and max_observed_wait_ns as double ns for reporting.
    std::unordered_map<uint64_t, std::vector<StalledWaiter>>          by_lock;
    std::unordered_map<uint64_t, std::chrono::steady_clock::duration> max_observed_by_lock;

    for (const auto& [waiter_os_tid, edge] : waits_for_)
    {
        const auto observed_duration = now - edge.wait_start;
        if (observed_duration >= min_stall)
        {
            StalledWaiter sw;
            sw.os_tid = waiter_os_tid;
            sw.observed_wait_ns =
                static_cast<double>(std::chrono::duration_cast<std::chrono::nanoseconds>(observed_duration).count());

            by_lock[edge.lock_object_id].push_back(sw);

            // Track max observed duration for this lock (in duration domain).
            auto& current_max = max_observed_by_lock[edge.lock_object_id];
            if (observed_duration > current_max)
            {
                current_max = observed_duration;
            }
        }
    }

    // Build StalledGroups from the bucketed waiters, joining owner_os_tid and
    // accumulated_completed_ns from locks_. Leave is_deadlock at default (false);
    // OnSamplerTick will set it via cross-ref with confirmed cycles.
    std::vector<StalledGroup> groups;
    groups.reserve(by_lock.size());
    for (auto& [lock_object_id, waiters] : by_lock)
    {
        StalledGroup group;
        group.lock_object_id       = lock_object_id;
        group.waiters              = std::move(waiters);
        group.max_observed_wait_ns = static_cast<double>(
            std::chrono::duration_cast<std::chrono::nanoseconds>(max_observed_by_lock[lock_object_id]).count());

        const auto it = locks_.find(lock_object_id);
        if (it != locks_.end())
        {
            group.owner_os_tid             = it->second.owner_os_tid;
            group.accumulated_completed_ns = it->second.accumulated_duration_ns;
        }

        groups.push_back(std::move(group));
    }

    return groups;
}

void ContentionMonitor::SweepStaleLocks(std::chrono::steady_clock::duration max_idle)
{
    std::lock_guard<std::mutex> guard(graph_lock_);
    const auto                  now = std::chrono::steady_clock::now();

    for (auto it = locks_.begin(); it != locks_.end();)
    {
        const bool drained = it->second.concurrency <= 1;
        const bool stale   = (now - it->second.last_mutation) > max_idle;

        // BOTH conjuncts are required. Stale-alone would delete a live deadlock:
        // its Starts froze so it is stale, but it still has a waiter. Drained-alone
        // would churn a recurring lock between bursts and lose its accumulated
        // metrics and owner.
        if (drained && stale)
        {
            it = locks_.erase(it);
        }
        else
        {
            ++it;
        }
    }
}

std::vector<std::vector<uint64_t>> ContentionMonitor::DetectDeadlocks()
{
    std::lock_guard<std::mutex> guard(graph_lock_);

    // The out-degree of every thread node is at most 1: a waiter waits on exactly
    // one lock, and a lock has exactly one owner. So the wait-for graph is a
    // functional graph (each node has at most one successor) and each component
    // contains at most one cycle. We find cycles by walking each chain and
    // watching for a return onto the current path - O(N) with no recursion.

    // successor: the owner thread that 'tid' is directly waiting for, if any.
    auto successor = [this](uint64_t tid, uint64_t& owner_out) -> bool
    {
        const auto wit = waits_for_.find(tid);
        if (wit == waits_for_.end())
        {
            return false; // not waiting -> chain ends
        }
        const auto lit = locks_.find(wit->second.lock_object_id);
        if (lit == locks_.end())
        {
            return false; // lock entry already swept -> chain ends
        }
        const uint64_t owner = lit->second.owner_os_tid;
        if (owner == 0 || owner == tid)
        {
            return false; // unknown owner or self-owned -> chain ends
        }
        owner_out = owner;
        return true;
    };

    std::unordered_set<uint64_t>       finished;     // nodes whose chain is fully explored
    std::unordered_set<uint64_t>       cyclic_locks; // locks participating in a cycle this scan
    std::vector<std::vector<uint64_t>> raw_cycles;   // thread-id cycles found this scan
    // DFS - each vertex has at most one successor, assume all keys of waits_for_ are root vertices
    // each vertex may potentially represent a disjoint wait chain or connected wait chain, we determine
    // deadlocks if there is back edge into the currently walked path. Outer for loop visits each root vertex
    for (const auto& entry : waits_for_)
    {
        const uint64_t start_tid = entry.first;
        if (finished.count(start_tid) != 0)
        {
            continue;
        }

        std::vector<uint64_t>                path;
        std::unordered_map<uint64_t, size_t> position; // tid -> index in path
        uint64_t                             cur = start_tid;

        while (true)
        {
            if (finished.count(cur) != 0)
            {
                break; // walked into already-explored territory: no new cycle here
            }

            const auto pit = position.find(cur);
            if (pit != position.end())
            {
                // Back-edge onto the current path: the cycle is path[pit..end).
                std::vector<uint64_t> cycle(path.begin() + static_cast<std::ptrdiff_t>(pit->second), path.end());
                for (const uint64_t tid : cycle)
                {
                    const auto wit = waits_for_.find(tid);
                    if (wit != waits_for_.end())
                    {
                        cyclic_locks.insert(wit->second.lock_object_id);
                    }
                }
                raw_cycles.push_back(std::move(cycle));
                break;
            }

            uint64_t owner = 0;
            if (!successor(cur, owner))
            {
                break; // chain ends without closing a cycle
            }

            position.emplace(cur, path.size());
            path.push_back(cur);
            cur = owner;
        }

        for (const uint64_t tid : path)
        {
            finished.insert(tid);
        }
    }

    // Persistence bookkeeping. A lock seen in a cycle this scan increments its
    // counter; a lock NOT in a cycle resets to 0. This debounces stale-owner false
    // positives: an owner edge can go stale on an uncontended lock handoff (which
    // raises no event), briefly fabricating a cycle that the next scan clears.
    for (auto& [lock_object_id, lock_entry] : locks_)
    {
        if (cyclic_locks.count(lock_object_id) != 0)
        {
            if (lock_entry.cycle_seen_scans < UINT32_MAX)
            {
                lock_entry.cycle_seen_scans++;
            }
        }
        else
        {
            lock_entry.cycle_seen_scans = 0;
        }
    }

    // Report only cycles whose every lock has persisted for at least this many
    // consecutive scans (observed this scan plus at least one prior). A true
    // deadlock is permanent, so it trivially clears the gate on the second scan.
    // this is to account for spurious events that may cause a false positive in the first scan.
    constexpr uint32_t kPersistenceScans = 2;

    std::vector<std::vector<uint64_t>> confirmed;
    for (auto& cycle : raw_cycles)
    {
        bool persisted = true;
        for (const uint64_t tid : cycle)
        {
            const auto wit = waits_for_.find(tid);
            if (wit == waits_for_.end())
            {
                persisted = false;
                break;
            }
            const auto lit = locks_.find(wit->second.lock_object_id);
            if (lit == locks_.end() || lit->second.cycle_seen_scans < kPersistenceScans)
            {
                persisted = false;
                break;
            }
        }
        if (persisted)
        {
            confirmed.push_back(std::move(cycle));
        }
    }

    return confirmed;
}

std::unordered_set<uint64_t> ContentionMonitor::LockIdsForCycles(const std::vector<std::vector<uint64_t>>& cycles)
{
    std::lock_guard<std::mutex> guard(graph_lock_);

    // Derive the set of lock IDs participating in confirmed deadlock cycles from
    // the SINGLE DetectDeadlocks() result. Do NOT re-run the cycle walk here:
    // that would double-advance the persistence gate (cycle_seen_scans) and
    // confirm deadlocks in 1 tick instead of 2.
    std::unordered_set<uint64_t> deadlock_locks;
    for (const auto& cycle : cycles)
    {
        for (const uint64_t tid : cycle)
        {
            const auto wit = waits_for_.find(tid);
            if (wit != waits_for_.end())
            {
                deadlock_locks.insert(wit->second.lock_object_id);
            }
        }
    }
    return deadlock_locks;
}

SamplerTickResult ContentionMonitor::OnSamplerTick(std::chrono::steady_clock::duration min_stall,
                                                   std::chrono::steady_clock::duration max_idle)
{
    // Do NOT hold graph_lock_ across these calls: each callee acquires it internally
    // and the mutex is non-recursive, so nesting would self-deadlock. Run the deadlock
    // walk EXACTLY ONCE per tick (it mutates cycle_seen_scans); derive the deadlock-lock
    // set from THAT result, never via a second walk (persistence-gate correctness).

    SweepStaleLocks(max_idle);

    SamplerTickResult result;
    result.deadlock_cycles = DetectDeadlocks(); // single walk; owns graph_lock_

    const auto deadlock_locks = LockIdsForCycles(result.deadlock_cycles); // derive from THAT result
    result.stalled_groups     = SnapshotStalledGroups(min_stall);         // owns graph_lock_

    // Cross-reference to set is_deadlock. A lock participating in a confirmed cycle
    // is rendered by the deadlock path; ProjectStalledGroups will skip it (dedup).
    for (auto& group : result.stalled_groups)
    {
        group.is_deadlock = (deadlock_locks.count(group.lock_object_id) != 0);
    }

    return result;
}

} // namespace continuous_profiler