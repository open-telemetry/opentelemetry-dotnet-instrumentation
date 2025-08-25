#include "pch.h"
#include "../../src/OpenTelemetry.AutoInstrumentation.Native/continuous_profiler.h"

static continuous_profiler::thread_span_context defaultContext;

TEST(ThreadSpanContextMapTest, BasicGet)
{
    continuous_profiler::ThreadSpanContextMap      threadSpanContextMap;
    const continuous_profiler::thread_span_context context = {1, 1, 1};
    threadSpanContextMap.Put(1, context);

    ASSERT_EQ(threadSpanContextMap.Get(1), context);
}

TEST(ThreadSpanContextMapTest, BasicUpdate)
{
    continuous_profiler::ThreadSpanContextMap      threadSpanContextMap;
    const continuous_profiler::thread_span_context context       = {1, 1, 1};
    const continuous_profiler::thread_span_context other_context = {2, 2, 2};
    threadSpanContextMap.Put(1, context);
    ASSERT_EQ(threadSpanContextMap.Get(1), context);

    threadSpanContextMap.Put(1, other_context);
    ASSERT_EQ(threadSpanContextMap.Get(1), other_context);
}

TEST(ThreadSpanContextMapTest, ConsistentUpdate)
{
    continuous_profiler::ThreadSpanContextMap      threadSpanContextMap;
    const continuous_profiler::thread_span_context context       = {1, 1, 1};
    const continuous_profiler::thread_span_context other_context = {2, 2, 2};
    threadSpanContextMap.Put(1, context);
    threadSpanContextMap.Put(2, context);
    ASSERT_EQ(threadSpanContextMap.Get(1), context);
    ASSERT_EQ(threadSpanContextMap.Get(2), context);

    threadSpanContextMap.Put(1, other_context);
    ASSERT_EQ(threadSpanContextMap.Get(1), other_context);

    threadSpanContextMap.Remove(context);
    ASSERT_EQ(threadSpanContextMap.Get(1), other_context);
    ASSERT_EQ(threadSpanContextMap.Get(2), defaultContext);
}

TEST(ThreadSpanContextMapTest, RemoveByThreadId)
{
    continuous_profiler::ThreadSpanContextMap      threadSpanContextMap;
    const continuous_profiler::thread_span_context context = {1, 1, 1};
    threadSpanContextMap.Put(1, context);
    ASSERT_EQ(threadSpanContextMap.Get(1), context);

    threadSpanContextMap.Remove(1);

    ASSERT_EQ(threadSpanContextMap.Get(1), defaultContext);
}

TEST(ThreadSpanContextMapTest, RemoveBySpanContext)
{
    continuous_profiler::ThreadSpanContextMap      threadSpanContextMap;
    const continuous_profiler::thread_span_context context = {1, 1, 1};
    threadSpanContextMap.Put(1, context);
    threadSpanContextMap.Put(2, context);
    ASSERT_EQ(threadSpanContextMap.Get(1), context);
    ASSERT_EQ(threadSpanContextMap.Get(2), context);

    threadSpanContextMap.Remove(context);

    ASSERT_EQ(threadSpanContextMap.Get(1), defaultContext);
    ASSERT_EQ(threadSpanContextMap.Get(2), defaultContext);
}