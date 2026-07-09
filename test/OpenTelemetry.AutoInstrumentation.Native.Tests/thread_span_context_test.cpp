#include "pch.h"
#include "../../src/OpenTelemetry.AutoInstrumentation.Native/continuous_profiler.h"

#ifdef _WIN32
#include <cstdlib>
#include <memory>
#endif

TEST(ThreadSpanContextMapTest, BasicGet)
{
    continuous_profiler::ThreadSpanContextMap      threadSpanContextMap;
    const continuous_profiler::thread_span_context context = {1, 1, 1};
    threadSpanContextMap.Put(1, context);

    ASSERT_EQ(threadSpanContextMap.GetContext(1), context);
}

TEST(ThreadSpanContextMapTest, BasicUpdate)
{
    continuous_profiler::ThreadSpanContextMap      threadSpanContextMap;
    const continuous_profiler::thread_span_context context       = {1, 1, 1};
    const continuous_profiler::thread_span_context other_context = {2, 2, 2};
    threadSpanContextMap.Put(1, context);
    ASSERT_EQ(threadSpanContextMap.GetContext(1), context);

    threadSpanContextMap.Put(1, other_context);
    ASSERT_EQ(threadSpanContextMap.GetContext(1), other_context);
}

TEST(ThreadSpanContextMapTest, ConsistentUpdate)
{
    continuous_profiler::ThreadSpanContextMap      threadSpanContextMap;
    const continuous_profiler::thread_span_context context       = {1, 1, 1};
    const continuous_profiler::thread_span_context other_context = {2, 2, 2};
    threadSpanContextMap.Put(1, context);
    threadSpanContextMap.Put(2, context);
    ASSERT_EQ(threadSpanContextMap.GetContext(1), context);
    ASSERT_EQ(threadSpanContextMap.GetContext(2), context);

    threadSpanContextMap.Put(1, other_context);
    ASSERT_EQ(threadSpanContextMap.GetContext(1), other_context);

    threadSpanContextMap.Remove(context);

    ASSERT_EQ(threadSpanContextMap.GetContext(1), other_context);
    ASSERT_FALSE(threadSpanContextMap.GetContext(2).has_value());
}

TEST(ThreadSpanContextMapTest, RemoveByThreadId)
{
    continuous_profiler::ThreadSpanContextMap      threadSpanContextMap;
    const continuous_profiler::thread_span_context context = {1, 1, 1};
    threadSpanContextMap.Put(1, context);
    ASSERT_EQ(threadSpanContextMap.GetContext(1), context);

    threadSpanContextMap.Remove(1);

    ASSERT_FALSE(threadSpanContextMap.GetContext(1).has_value());
}

TEST(ThreadSpanContextMapTest, RemoveBySpanContext)
{
    continuous_profiler::ThreadSpanContextMap      threadSpanContextMap;
    const continuous_profiler::thread_span_context context = {1, 1, 1};
    threadSpanContextMap.Put(1, context);
    threadSpanContextMap.Put(2, context);
    ASSERT_EQ(threadSpanContextMap.GetContext(1), context);
    ASSERT_EQ(threadSpanContextMap.GetContext(2), context);

    threadSpanContextMap.Remove(context);

    ASSERT_FALSE(threadSpanContextMap.GetContext(1).has_value());
    ASSERT_FALSE(threadSpanContextMap.GetContext(2).has_value());
}

#ifdef _WIN32
// Memory-safety regression test for continuous_profiler.cpp: AllocationTick must validate the
// event length before indexing; a too-short payload underflows `data[dataLen - 8]` (unsigned)
// into an out-of-bounds read.
//
// The call runs in a death-test subprocess: a correct AllocationTick returns cleanly and the
// child exits 0. If a regression reintroduces the out-of-bounds read, the child crashes and the
// ExitedWithCode(0) assertion fails - keeping the crash (and AllocationTick's still-held
// std::shared_lock) isolated to the child instead of destabilizing this test process.
namespace
{

// Runs in the death-test child; kept as a helper so the EXPECT_EXIT statement has no bare commas
// (the preprocessor would otherwise treat them as macro-argument separators).
[[noreturn]] void RunAllocationTickWithShortPayload()
{
    continuous_profiler::ContinuousProfiler profiler;
    // Force the sub-sampler to accept this event so AllocationTick reaches the parse
    // (target-per-cycle >= 1 makes the first ShouldSample() return true).
    profiler.allocationSubSampler = std::make_unique<continuous_profiler::AllocationSubSampler>(1000u, 60u);

    const unsigned char data[4] = {0, 0, 0, 0};
    profiler.AllocationTick(4u, data);

    std::exit(0);
}

} // namespace

TEST(ContinuousProfilerSafetyTest, AllocationTickRejectsShortPayloadWithoutReadingPastBuffer)
{
    EXPECT_EXIT(RunAllocationTickWithShortPayload(), ::testing::ExitedWithCode(0), "");
}
#endif
