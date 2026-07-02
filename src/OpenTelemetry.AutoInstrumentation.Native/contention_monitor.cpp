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
    bool       already_waiting_on_this_lock = false;
    const auto prior                        = waits_for_.find(contender_os_tid);
    if (prior != waits_for_.end())
    {
        if (prior->second == lock_object_id)
        {
            already_waiting_on_this_lock = true; // duplicate Start; do not double-count
        }
        else
        {
            const auto old_lock = locks_.find(prior->second);
            if (old_lock != locks_.end() && old_lock->second.concurrency > 0)
            {
                old_lock->second.concurrency--;
                old_lock->second.last_mutation = now;
            }
        }
    }

    waits_for_[contender_os_tid] = lock_object_id;

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

    const uint64_t lock_object_id = edge->second;
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

    const uint64_t lock_object_id = edge->second;
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

std::vector<ContentionGroup> ContentionMonitor::SnapshotGroups()
{
    std::lock_guard<std::mutex> guard(graph_lock_);

    // One pass over waits_for_ bucketing contender OS tids by the lock they wait
    // on. This materializes, on demand, the inverse index we deliberately do not
    // store as state.
    std::unordered_map<uint64_t, std::vector<uint64_t>> by_lock;
    for (const auto& [contender_os_tid, lock_object_id] : waits_for_)
    {
        by_lock[lock_object_id].push_back(contender_os_tid);
    }

    std::vector<ContentionGroup> groups;
    groups.reserve(by_lock.size());
    for (auto& [lock_object_id, contenders] : by_lock)
    {
        ContentionGroup group;
        group.lock_object_id    = lock_object_id;
        group.contender_os_tids = std::move(contenders);

        const auto it = locks_.find(lock_object_id);
        if (it != locks_.end())
        {
            group.owner_os_tid            = it->second.owner_os_tid;
            group.accumulated_duration_ns = it->second.accumulated_duration_ns;
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
        const auto lit = locks_.find(wit->second);
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
                // Back-edge onto the current path: the cycle is path[pit..end].
                std::vector<uint64_t> cycle(path.begin() + static_cast<std::ptrdiff_t>(pit->second), path.end());
                for (const uint64_t tid : cycle)
                {
                    const auto wit = waits_for_.find(tid);
                    if (wit != waits_for_.end())
                    {
                        cyclic_locks.insert(wit->second);
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
            const auto lit = locks_.find(wit->second);
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

std::vector<std::vector<uint64_t>> ContentionMonitor::OnSamplerTick(std::chrono::steady_clock::duration max_idle)
{
    // Do NOT hold graph_lock_ across these calls: SweepStaleLocks and DetectDeadlocks each
    // acquire it internally and the mutex is non-recursive, so nesting would self-deadlock.
    SweepStaleLocks(max_idle);
    return DetectDeadlocks();
}

} // namespace continuous_profiler