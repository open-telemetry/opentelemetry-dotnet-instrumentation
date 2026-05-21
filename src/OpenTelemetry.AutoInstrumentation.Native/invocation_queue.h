// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#ifndef OTEL_PROFILER_INVOCATION_QUEUE_H_
#define OTEL_PROFILER_INVOCATION_QUEUE_H_


#include <atomic>
#include <chrono>
#include <condition_variable>
#include <deque>
#include <functional>
#include <future>
#include <memory>
#include <mutex>
#include <thread>

namespace ProfilerStackCapture
{

enum class InvocationStatus { Invoked, TimedOut };

struct ThreadJoinerOnDelete
{
    void operator()(std::thread* t) const
    {
        if (t)
        {
            if (t->joinable())
                t->join();
            delete t;
        }
    }
};

/// @brief Single-worker background queue used to execute operations with a
///        timeout, detecting deadlocks on locks held by suspended threads.
///
/// The worker is a plain OS thread - never a managed CLR thread - so it is
/// unaffected by CLR SuspendRuntime and by SuspendThread calls made by the
/// profiler against target threads.
class InvocationQueue
{
public:
    InvocationQueue();
    ~InvocationQueue();

    InvocationQueue(const InvocationQueue&)            = delete;
    InvocationQueue& operator=(const InvocationQueue&) = delete;

    /// @brief Dispatches fn to the worker and waits up to timeout for completion.
    /// @return Invoked if fn completed within timeout, TimedOut otherwise.
    InvocationStatus Invoke(const std::function<void()>& fn, std::chrono::milliseconds timeout);

    void Stop();

private:
    struct QueuedInvocation
    {
        std::function<void()> fn;
        std::promise<void>    completedPromise;
    };

    std::deque<std::shared_ptr<QueuedInvocation>>      queue_;
    std::mutex                                         mutex_;
    std::condition_variable                            condVar_;
    std::atomic<bool>                                  stop_{false};
    std::unique_ptr<std::thread, ThreadJoinerOnDelete> worker_;

    void WorkerLoop();
};

} // namespace ProfilerStackCapture

#endif // OTEL_PROFILER_INVOCATION_QUEUE_H_