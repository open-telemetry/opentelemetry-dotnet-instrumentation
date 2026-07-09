#include "pch.h"
#include "../../src/OpenTelemetry.AutoInstrumentation.Native/continuous_profiler.h"

#ifdef _WIN32
#include <Windows.h>
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
// Memory-safety regression test for continuous_profiler.cpp: ContinuousProfilerSetNativeContext
// must guard against a null profiler_info (its sibling exports do), which is the state before
// profiler initialization / when profiling is disabled. Turned into a deterministic, catchable
// access violation via SEH; a correct implementation returns without faulting.
namespace
{

// SEH wrapper - must not own any C++ objects requiring unwinding.
bool SetNativeContextFaults()
{
    __try
    {
        ContinuousProfilerSetNativeContext(1, 2, 3);
        return false;
    }
    __except (GetExceptionCode() == EXCEPTION_ACCESS_VIOLATION ? EXCEPTION_EXECUTE_HANDLER : EXCEPTION_CONTINUE_SEARCH)
    {
        return true;
    }
}

} // namespace

TEST(ContinuousProfilerSafetyTest, SetNativeContextDoesNotDereferenceNullProfilerInfo)
{
    const bool faulted = SetNativeContextFaults();

    ASSERT_FALSE(faulted) << "ContinuousProfilerSetNativeContext dereferenced a null profiler_info.";
}
#endif
