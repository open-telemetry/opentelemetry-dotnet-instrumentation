#include "pch.h"
#include "../../src/OpenTelemetry.AutoInstrumentation.Native/continuous_profiler.h"

#include <chrono>
#include <future>
#include <thread>

#ifdef _WIN32
#include <cstdlib>
#include <memory>
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

TEST(ContinuousProfilerConfigurationTest, RuntimeReconfigurationStartsStopsAndDoesNotDuplicateSamplingThread)
{
    continuous_profiler::ContinuousProfiler profiler;

    ASSERT_FALSE(profiler.SetThreadSamplingEnabled(true));
    ASSERT_FALSE(profiler.IsThreadSamplingThreadRunning());

    ASSERT_TRUE(profiler.SetThreadSamplingInterval(10000u));
    const auto preparedConfiguration = profiler.GetThreadSamplingConfiguration();
    ASSERT_TRUE(profiler.SetThreadSamplingInterval(10000u));
    const auto repeatedPreparedConfiguration = profiler.GetThreadSamplingConfiguration();
    ASSERT_EQ(preparedConfiguration.threadSamplingInterval.has_value(),
              repeatedPreparedConfiguration.threadSamplingInterval.has_value());
    ASSERT_EQ(preparedConfiguration.selectedThreadsSamplingInterval.has_value(),
              repeatedPreparedConfiguration.selectedThreadsSamplingInterval.has_value());

    ASSERT_TRUE(profiler.SetThreadSamplingEnabled(true));
    ASSERT_TRUE(profiler.IsThreadSamplingThreadRunning());
    const auto initialGeneration = profiler.GetThreadSamplingThreadGeneration();
    auto       configuration     = profiler.GetThreadSamplingConfiguration();
    ASSERT_EQ(10000u, configuration.threadSamplingInterval.value());

    ASSERT_TRUE(profiler.SetThreadSamplingInterval(1234u));
    ASSERT_TRUE(profiler.IsThreadSamplingThreadRunning());
    ASSERT_EQ(initialGeneration, profiler.GetThreadSamplingThreadGeneration());
    configuration = profiler.GetThreadSamplingConfiguration();
    ASSERT_EQ(1234u, configuration.threadSamplingInterval.value());
    ASSERT_EQ(1234u, profiler.GetConfiguredThreadSamplingInterval());

    ASSERT_FALSE(profiler.SetThreadSamplingInterval(0));
    configuration = profiler.GetThreadSamplingConfiguration();
    ASSERT_EQ(1234u, configuration.threadSamplingInterval.value());

    const auto enabledConfiguration = profiler.GetThreadSamplingConfiguration();
    ASSERT_TRUE(profiler.SetThreadSamplingEnabled(true));
    ASSERT_EQ(initialGeneration, profiler.GetThreadSamplingThreadGeneration());
    const auto repeatedEnabledConfiguration = profiler.GetThreadSamplingConfiguration();
    ASSERT_EQ(enabledConfiguration.threadSamplingInterval.value(),
              repeatedEnabledConfiguration.threadSamplingInterval.value());
    ASSERT_EQ(enabledConfiguration.selectedThreadsSamplingInterval.has_value(),
              repeatedEnabledConfiguration.selectedThreadsSamplingInterval.has_value());

    ASSERT_TRUE(profiler.SetThreadSamplingEnabled(false));
    ASSERT_FALSE(profiler.IsThreadSamplingThreadRunning());
    ASSERT_FALSE(profiler.GetThreadSamplingConfiguration().threadSamplingInterval.has_value());
    ASSERT_EQ(1234u, profiler.GetConfiguredThreadSamplingInterval());

    const auto disabledConfiguration = profiler.GetThreadSamplingConfiguration();
    ASSERT_TRUE(profiler.SetThreadSamplingEnabled(false));
    const auto repeatedDisabledConfiguration = profiler.GetThreadSamplingConfiguration();
    ASSERT_EQ(disabledConfiguration.threadSamplingInterval.has_value(),
              repeatedDisabledConfiguration.threadSamplingInterval.has_value());
    ASSERT_EQ(disabledConfiguration.selectedThreadsSamplingInterval.has_value(),
              repeatedDisabledConfiguration.selectedThreadsSamplingInterval.has_value());

    ASSERT_TRUE(profiler.SetThreadSamplingEnabled(true));
    ASSERT_TRUE(profiler.IsThreadSamplingThreadRunning());
    ASSERT_EQ(initialGeneration + 1, profiler.GetThreadSamplingThreadGeneration());
    ASSERT_EQ(1234u, profiler.GetThreadSamplingConfiguration().threadSamplingInterval.value());

    ASSERT_TRUE(profiler.SetThreadSamplingEnabled(false));
}

TEST(ContinuousProfilerConfigurationTest, RapidUpdatesReloadLatestConfiguration)
{
    continuous_profiler::ContinuousProfiler          profiler;
    continuous_profiler::ThreadSamplingConfiguration configuration;

    ASSERT_TRUE(profiler.TryReloadThreadSamplingConfiguration(configuration));
    ASSERT_FALSE(configuration.threadSamplingInterval.has_value());
    ASSERT_FALSE(configuration.selectedThreadsSamplingInterval.has_value());
    ASSERT_FALSE(profiler.TryReloadThreadSamplingConfiguration(configuration));

    ASSERT_TRUE(profiler.SetThreadSamplingInterval(10000u));

    // Keep the sampling thread in ReserveCapacity so this test is the only consumer of the dirty flag.
    std::unique_lock<std::mutex> samplingThreadGate(profiler.thread_state_lock_);
    ASSERT_TRUE(profiler.SetThreadSamplingEnabled(true));
    ASSERT_TRUE(profiler.SetThreadSamplingInterval(5000u));
    ASSERT_TRUE(profiler.SetThreadSamplingInterval(1234u));

    ASSERT_TRUE(profiler.TryReloadThreadSamplingConfiguration(configuration));
    ASSERT_EQ(1234u, configuration.threadSamplingInterval.value());
    ASSERT_FALSE(configuration.selectedThreadsSamplingInterval.has_value());
    ASSERT_FALSE(profiler.TryReloadThreadSamplingConfiguration(configuration));

    // Leave a pending reload for the real sampling thread before releasing it.
    ASSERT_TRUE(profiler.SetThreadSamplingInterval(4321u));
    ASSERT_TRUE(profiler.SetThreadSamplingInterval(1234u));
    samplingThreadGate.unlock();

    ASSERT_TRUE(profiler.SetThreadSamplingEnabled(false));
}

TEST(ContinuousProfilerConfigurationTest, DisableWakesLongSamplingWaitImmediately)
{
    continuous_profiler::ContinuousProfiler profiler;

    ASSERT_TRUE(profiler.SetThreadSamplingInterval(60000u));
    ASSERT_TRUE(profiler.SetThreadSamplingEnabled(true));
    std::this_thread::sleep_for(std::chrono::milliseconds(100));

    auto samplingDisabled =
        std::async(std::launch::async, [&profiler]() { return profiler.SetThreadSamplingEnabled(false); });
    const auto waitStatus = samplingDisabled.wait_for(std::chrono::seconds(2));
    if (waitStatus != std::future_status::ready)
    {
        // Wake the sampling thread before destroying the future if the assertion fails.
        profiler.Shutdown();
        samplingDisabled.wait();
    }

    ASSERT_EQ(std::future_status::ready, waitStatus);
    if (waitStatus == std::future_status::ready)
    {
        ASSERT_TRUE(samplingDisabled.get());
    }
    ASSERT_FALSE(profiler.IsThreadSamplingThreadRunning());
}

TEST(ContinuousProfilerConfigurationTest, ShutdownWakesLongSamplingWaitImmediately)
{
    continuous_profiler::ContinuousProfiler profiler;

    ASSERT_TRUE(profiler.SetThreadSamplingInterval(60000u));
    ASSERT_TRUE(profiler.SetThreadSamplingEnabled(true));
    std::this_thread::sleep_for(std::chrono::milliseconds(100));

    auto       shutdownCompleted = std::async(std::launch::async, [&profiler]() { profiler.Shutdown(); });
    const auto waitStatus        = shutdownCompleted.wait_for(std::chrono::seconds(2));
    if (waitStatus != std::future_status::ready)
    {
        // A second notification avoids blocking on future destruction if the first wake was lost.
        profiler.Shutdown();
        shutdownCompleted.wait();
    }

    ASSERT_EQ(std::future_status::ready, waitStatus);
    if (waitStatus == std::future_status::ready)
    {
        shutdownCompleted.get();
    }
    ASSERT_TRUE(profiler.IsShutdownRequested());
    ASSERT_FALSE(profiler.IsThreadSamplingThreadRunning());
}

TEST(ContinuousProfilerBufferTest, PreservesBatchOrderAndSamplingIntervalMetadata)
{
    unsigned char output[1];
    unsigned int  samplingInterval;
    while (ThreadSamplingConsumeOneThreadSample(sizeof(output), output, &samplingInterval) > 0)
    {
    }
    ASSERT_EQ(0u, samplingInterval);

    ThreadSamplingRecordProducedThreadSample(new std::vector<unsigned char>{0x11}, 10000u);
    ThreadSamplingRecordProducedThreadSample(new std::vector<unsigned char>{0x22}, 20000u);
    ASSERT_FALSE(ThreadSamplingShouldProduceThreadSample());

    ASSERT_EQ(1, ThreadSamplingConsumeOneThreadSample(sizeof(output), output, &samplingInterval));
    ASSERT_EQ(0x11, output[0]);
    ASSERT_EQ(10000u, samplingInterval);

    ThreadSamplingRecordProducedThreadSample(new std::vector<unsigned char>{0x33}, 30000u);
    ASSERT_FALSE(ThreadSamplingShouldProduceThreadSample());

    ASSERT_EQ(1, ThreadSamplingConsumeOneThreadSample(sizeof(output), output, &samplingInterval));
    ASSERT_EQ(0x22, output[0]);
    ASSERT_EQ(20000u, samplingInterval);

    ASSERT_EQ(1, ThreadSamplingConsumeOneThreadSample(sizeof(output), output, &samplingInterval));
    ASSERT_EQ(0x33, output[0]);
    ASSERT_EQ(30000u, samplingInterval);

    ASSERT_EQ(0, ThreadSamplingConsumeOneThreadSample(sizeof(output), output, &samplingInterval));
    ASSERT_EQ(0u, samplingInterval);
}

TEST(ContinuousProfilerConfigurationTest, CpuReconfigurationDoesNotRestartOrCorruptSelectiveSampling)
{
    continuous_profiler::ContinuousProfiler profiler;

    profiler.ConfigureSelectedThreadSampling(60000u);
    ASSERT_TRUE(profiler.IsThreadSamplingThreadRunning());
    const auto selectiveThreadGeneration = profiler.GetThreadSamplingThreadGeneration();
    const auto selectiveConfiguration    = profiler.GetThreadSamplingConfiguration();

    ASSERT_TRUE(profiler.SetThreadSamplingInterval(120000u));
    const auto preparedMixedConfiguration = profiler.GetThreadSamplingConfiguration();
    ASSERT_EQ(selectiveConfiguration.threadSamplingInterval.has_value(),
              preparedMixedConfiguration.threadSamplingInterval.has_value());
    ASSERT_EQ(selectiveConfiguration.selectedThreadsSamplingInterval.value(),
              preparedMixedConfiguration.selectedThreadsSamplingInterval.value());
    ASSERT_EQ(selectiveThreadGeneration, profiler.GetThreadSamplingThreadGeneration());

    ASSERT_TRUE(profiler.SetThreadSamplingEnabled(true));
    ASSERT_EQ(selectiveThreadGeneration, profiler.GetThreadSamplingThreadGeneration());

    const auto validMixedConfiguration = profiler.GetThreadSamplingConfiguration();
    ASSERT_EQ(120000u, validMixedConfiguration.threadSamplingInterval.value());

    ASSERT_FALSE(profiler.SetThreadSamplingInterval(60000u));
    ASSERT_FALSE(profiler.SetThreadSamplingInterval(150000u));
    const auto configuration = profiler.GetThreadSamplingConfiguration();
    ASSERT_EQ(120000u, profiler.GetConfiguredThreadSamplingInterval());
    ASSERT_EQ(120000u, configuration.threadSamplingInterval.value());
    ASSERT_EQ(validMixedConfiguration.selectedThreadsSamplingInterval.value(),
              configuration.selectedThreadsSamplingInterval.value());

    ASSERT_TRUE(profiler.SetThreadSamplingEnabled(false));
    ASSERT_TRUE(profiler.IsThreadSamplingThreadRunning());
    ASSERT_EQ(selectiveThreadGeneration, profiler.GetThreadSamplingThreadGeneration());
    ASSERT_FALSE(profiler.GetThreadSamplingConfiguration().threadSamplingInterval.has_value());
    ASSERT_EQ(60000u, profiler.GetThreadSamplingConfiguration().selectedThreadsSamplingInterval.value());
}

TEST(ContinuousProfilerConfigurationTest, ConcurrentSettersDoNotCreateDuplicateSamplingThreads)
{
    continuous_profiler::ContinuousProfiler profiler;

    const auto runConcurrently = [](auto&& operation)
    {
        constexpr auto                 callCount = 8;
        std::promise<void>             startPromise;
        auto                           start = startPromise.get_future().share();
        std::vector<std::future<bool>> results;
        results.reserve(callCount);

        for (auto i = 0; i < callCount; i++)
        {
            results.emplace_back(std::async(std::launch::async,
                                            [start, &operation]()
                                            {
                                                start.wait();
                                                return operation();
                                            }));
        }

        startPromise.set_value();
        auto succeeded = true;
        for (auto& result : results)
        {
            succeeded &= result.get();
        }

        return succeeded;
    };

    ASSERT_TRUE(profiler.SetThreadSamplingInterval(10000u));
    ASSERT_TRUE(runConcurrently([&profiler]() { return profiler.SetThreadSamplingEnabled(true); }));
    ASSERT_TRUE(profiler.IsThreadSamplingThreadRunning());
    ASSERT_EQ(1u, profiler.GetThreadSamplingThreadGeneration());

    ASSERT_TRUE(runConcurrently([&profiler]() { return profiler.SetThreadSamplingInterval(1234u); }));
    ASSERT_EQ(1u, profiler.GetThreadSamplingThreadGeneration());
    ASSERT_EQ(1234u, profiler.GetThreadSamplingConfiguration().threadSamplingInterval.value());

    ASSERT_TRUE(runConcurrently([&profiler]() { return profiler.SetThreadSamplingEnabled(false); }));
    ASSERT_FALSE(profiler.IsThreadSamplingThreadRunning());
    ASSERT_EQ(1u, profiler.GetThreadSamplingThreadGeneration());
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
