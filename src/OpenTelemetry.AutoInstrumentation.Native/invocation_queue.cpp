// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#include "invocation_queue.h"

namespace ProfilerStackCapture
{

InvocationQueue::InvocationQueue()
{
    worker_ = std::unique_ptr<std::thread, ThreadJoinerOnDelete>(new std::thread(&InvocationQueue::WorkerLoop, this));
}

InvocationQueue::~InvocationQueue()
{
    Stop();
}

void InvocationQueue::Stop()
{
    bool expected = false;
    if (stop_.compare_exchange_strong(expected, true))
        condVar_.notify_all();
}

InvocationStatus InvocationQueue::Invoke(const std::function<void()>& fn, std::chrono::milliseconds timeout)
{
    if (stop_.load())
        return InvocationStatus::TimedOut;

    auto item = std::make_shared<QueuedInvocation>();
    item->fn  = fn;
    auto fut  = item->completedPromise.get_future();
    {
        std::lock_guard<std::mutex> lock(mutex_);
        queue_.push_back(item);
    }
    condVar_.notify_one();
    return fut.wait_for(timeout) == std::future_status::ready ? InvocationStatus::Invoked : InvocationStatus::TimedOut;
}

void InvocationQueue::WorkerLoop()
{
    for (;;)
    {
        std::shared_ptr<QueuedInvocation> item;
        {
            std::unique_lock<std::mutex> lock(mutex_);
            condVar_.wait(lock, [this]() { return stop_.load() || !queue_.empty(); });
            if (stop_.load())
                break;
            if (!queue_.empty())
            {
                item = queue_.front();
                queue_.pop_front();
            }
            else
                continue;
        }
        try
        {
            item->fn();
        }
        catch (...)
        {
        }
        item->completedPromise.set_value();
    }
}

} // namespace ProfilerStackCapture
