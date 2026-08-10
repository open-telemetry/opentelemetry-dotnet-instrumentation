#include "pch.h"
#include "../../src/OpenTelemetry.AutoInstrumentation.Native/continuous_profiler.h"

#include <algorithm>
#include <chrono>
#include <future>
#include <thread>
#include <vector>

#ifdef _WIN32
#include <cstdlib>
#include <memory>
#include <Windows.h>
#endif

namespace
{

constexpr auto kTestSamplesBufferMaximumSize = 200 * 1024;
constexpr auto kTestTraceIdHigh              = uint64_t{0x11};
constexpr auto kTestTraceIdLow               = uint64_t{0x22};

class SelectiveSamplingBufferTest : public ::testing::Test
{
protected:
    static std::vector<unsigned char> CreateReadBuffer()
    {
        return std::vector<unsigned char>(kTestSamplesBufferMaximumSize);
    }

    static int32_t Drain(std::vector<unsigned char>& buffer)
    {
        return SelectiveSamplerReadThreadSamples(static_cast<int32_t>(buffer.size()), buffer.data());
    }

    void SetUp() override
    {
        auto buffer = CreateReadBuffer();
        Drain(buffer);
    }

    void TearDown() override
    {
        auto buffer = CreateReadBuffer();
        Drain(buffer);
    }
};

class SelectiveSamplingPreparationTest : public SelectiveSamplingBufferTest
{
protected:
    void SetUp() override
    {
        SelectiveSamplingBufferTest::SetUp();
        continuous_profiler::RemoveSelectiveSamplingTrace({kTestTraceIdHigh, kTestTraceIdLow});
    }

    void TearDown() override
    {
        continuous_profiler::RemoveSelectiveSamplingTrace({kTestTraceIdHigh, kTestTraceIdLow});
        SelectiveSamplingBufferTest::TearDown();
    }
};

} // namespace

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

TEST(ContinuousProfilerConfigurationTest, ConfigurationChangesDoNotWakeStopWait)
{
    continuous_profiler::ContinuousProfiler profiler;

    ASSERT_TRUE(profiler.SetThreadSamplingInterval(1000u));
    ASSERT_TRUE(profiler.SetThreadSamplingEnabled(true));

    auto stopWait = std::async(std::launch::async, [&profiler]() { return profiler.WaitForStop(300u); });
    std::this_thread::sleep_for(std::chrono::milliseconds(50));

    ASSERT_TRUE(profiler.SetThreadSamplingInterval(500u));
    ASSERT_TRUE(profiler.SetThreadSamplingInterval(250u));
    ASSERT_EQ(std::future_status::timeout, stopWait.wait_for(std::chrono::milliseconds(100)));
    ASSERT_EQ(std::future_status::ready, stopWait.wait_for(std::chrono::seconds(1)));
    ASSERT_FALSE(stopWait.get());
    ASSERT_EQ(250u, profiler.GetThreadSamplingConfiguration().threadSamplingInterval.value());

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
    ASSERT_EQ(std::future_status::ready, waitStatus);
    if (waitStatus == std::future_status::ready)
    {
        shutdownCompleted.get();
    }
    ASSERT_TRUE(profiler.IsShutdownRequested());
    ASSERT_FALSE(profiler.IsThreadSamplingThreadRunning());
    ASSERT_FALSE(profiler.SetThreadSamplingEnabled(true));
    ASSERT_FALSE(profiler.SetThreadSamplingInterval(1000u));
}

TEST(ContinuousProfilerConfigurationTest, ShutdownStopsMixedCpuAndSelectiveSamplingAndRejectsRestart)
{
    continuous_profiler::ContinuousProfiler profiler;

    profiler.ConfigureSelectedThreadSampling(60000u);
    ASSERT_TRUE(profiler.SetThreadSamplingInterval(120000u));
    ASSERT_TRUE(profiler.SetThreadSamplingEnabled(true));
    ASSERT_TRUE(profiler.IsThreadSamplingThreadRunning());
    ASSERT_EQ(0u, profiler.GetAllocationSamplingRate());

    profiler.Shutdown();

    ASSERT_TRUE(profiler.IsShutdownRequested());
    ASSERT_FALSE(profiler.IsThreadSamplingThreadRunning());
    ASSERT_EQ(0u, profiler.GetAllocationSamplingRate());
    ASSERT_FALSE(profiler.SetThreadSamplingEnabled(true));
    ASSERT_FALSE(profiler.SetThreadSamplingInterval(120000u));
    ASSERT_FALSE(profiler.SetAllocationSamplingConfiguration(true, 100u));

    // The rollback is intentionally idempotent and permanently fail-closed.
    profiler.Shutdown();
    ASSERT_FALSE(profiler.IsThreadSamplingThreadRunning());
    ASSERT_EQ(0u, profiler.GetAllocationSamplingRate());
}

TEST(ContinuousProfilerConfigurationTest, AllocationSamplingRejectsZeroRate)
{
    continuous_profiler::ContinuousProfiler profiler;

    ASSERT_FALSE(profiler.SetAllocationSamplingConfiguration(true, 0u));
    ASSERT_TRUE(profiler.SetAllocationSamplingConfiguration(false, 0u));
}

TEST(ContinuousProfilerConfigurationTest, AllocationSamplingEnableFailsWhenUnsupported)
{
    continuous_profiler::ContinuousProfiler profiler;

    ASSERT_FALSE(profiler.SetAllocationSamplingConfiguration(true, 100u));
    ASSERT_EQ(0u, profiler.GetAllocationSamplingRate());
    ASSERT_TRUE(profiler.SetAllocationSamplingConfiguration(false, 0u));
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

TEST(ContinuousProfilerConfigurationTest, ReenableWhileThreadIsStoppingIsDeferredAndRestartedOnce)
{
    continuous_profiler::ContinuousProfiler profiler;

    ASSERT_TRUE(profiler.SetThreadSamplingInterval(60000u));

    // SamplingThreadMain takes this lock during startup. Holding it keeps the old
    // thread alive until the test has submitted a concurrent re-enable request.
    std::unique_lock<std::mutex> threadStateGate(profiler.thread_state_lock_);
    const auto                   initiallyEnabled = profiler.SetThreadSamplingEnabled(true);
    if (!initiallyEnabled)
    {
        threadStateGate.unlock();
        ASSERT_TRUE(initiallyEnabled);
        return;
    }

    const auto initialGeneration = profiler.GetThreadSamplingThreadGeneration();
    auto       disableResult =
        std::async(std::launch::async, [&profiler]() { return profiler.SetThreadSamplingEnabled(false); });

    const auto stoppingDeadline = std::chrono::steady_clock::now() + std::chrono::seconds(2);
    while (profiler.IsThreadSamplingThreadRunning() && std::chrono::steady_clock::now() < stoppingDeadline)
    {
        std::this_thread::yield();
    }
    const auto stoppingObserved = !profiler.IsThreadSamplingThreadRunning();

    auto enableResult =
        std::async(std::launch::async, [&profiler]() { return profiler.SetThreadSamplingEnabled(true); });
    const auto enableCompletedWhileStopping = enableResult.wait_for(std::chrono::seconds(1));

    threadStateGate.unlock();

    const auto disableCompleted = disableResult.wait_for(std::chrono::seconds(2));
    const auto enableCompleted  = enableResult.wait_for(std::chrono::seconds(2));

    ASSERT_TRUE(stoppingObserved);
    ASSERT_EQ(std::future_status::ready, enableCompletedWhileStopping);
    ASSERT_EQ(std::future_status::ready, disableCompleted);
    ASSERT_EQ(std::future_status::ready, enableCompleted);
    ASSERT_TRUE(disableResult.get());
    ASSERT_TRUE(enableResult.get());
    ASSERT_TRUE(profiler.IsThreadSamplingThreadRunning());
    ASSERT_EQ(initialGeneration + 1, profiler.GetThreadSamplingThreadGeneration());

    ASSERT_TRUE(profiler.SetThreadSamplingEnabled(false));
}

TEST_F(SelectiveSamplingBufferTest, SuccessfulAppendKeepsSamplingAdmissible)
{
    std::vector<unsigned char> sample = {0x11, 0x22};

    ASSERT_TRUE(SelectiveSamplingShouldProduceThreadSample());
    SelectiveSamplingRecordProducedThreadSample(static_cast<int32_t>(sample.size()), sample.data());
    ASSERT_TRUE(SelectiveSamplingShouldProduceThreadSample());

    auto       output   = CreateReadBuffer();
    const auto readSize = Drain(output);

    ASSERT_EQ(sample.size(), static_cast<size_t>(readSize));
    ASSERT_TRUE(std::equal(sample.begin(), sample.end(), output.begin()));
}

TEST_F(SelectiveSamplingBufferTest, ExactFitIsAcceptedAndBlocksSamplingUntilRead)
{
    std::vector<unsigned char> acceptedSample = {0x11, 0x22};
    std::vector<unsigned char> exactFitSample(kTestSamplesBufferMaximumSize - acceptedSample.size(), 0x33);

    SelectiveSamplingRecordProducedThreadSample(static_cast<int32_t>(acceptedSample.size()), acceptedSample.data());
    SelectiveSamplingRecordProducedThreadSample(static_cast<int32_t>(exactFitSample.size()), exactFitSample.data());

    ASSERT_FALSE(SelectiveSamplingShouldProduceThreadSample());

    auto       output   = CreateReadBuffer();
    const auto readSize = Drain(output);

    ASSERT_EQ(kTestSamplesBufferMaximumSize, readSize);
    ASSERT_TRUE(std::equal(acceptedSample.begin(), acceptedSample.end(), output.begin()));
    ASSERT_TRUE(std::equal(exactFitSample.begin(), exactFitSample.end(), output.begin() + acceptedSample.size()));
    ASSERT_TRUE(SelectiveSamplingShouldProduceThreadSample());
}

TEST_F(SelectiveSamplingBufferTest, OverflowBlocksSamplingAndPreservesBufferedDataUntilRead)
{
    std::vector<unsigned char> acceptedSample = {0x11, 0x22};
    std::vector<unsigned char> overflowingSample(kTestSamplesBufferMaximumSize - acceptedSample.size() + 1, 0x33);

    SelectiveSamplingRecordProducedThreadSample(static_cast<int32_t>(acceptedSample.size()), acceptedSample.data());
    SelectiveSamplingRecordProducedThreadSample(static_cast<int32_t>(overflowingSample.size()),
                                                overflowingSample.data());

    ASSERT_FALSE(SelectiveSamplingShouldProduceThreadSample());
    ASSERT_FALSE(SelectiveSamplingShouldProduceThreadSample());

    auto       output   = CreateReadBuffer();
    const auto readSize = Drain(output);

    ASSERT_EQ(acceptedSample.size(), static_cast<size_t>(readSize));
    ASSERT_TRUE(std::equal(acceptedSample.begin(), acceptedSample.end(), output.begin()));
    ASSERT_TRUE(SelectiveSamplingShouldProduceThreadSample());
}

TEST_F(SelectiveSamplingBufferTest, OversizedFirstBatchBlocksSamplingUntilValidEmptyRead)
{
    std::vector<unsigned char> oversizedSample(kTestSamplesBufferMaximumSize + 1, 0x33);
    SelectiveSamplingRecordProducedThreadSample(static_cast<int32_t>(oversizedSample.size()), oversizedSample.data());

    ASSERT_FALSE(SelectiveSamplingShouldProduceThreadSample());

    auto output = CreateReadBuffer();
    ASSERT_EQ(0, Drain(output));
    ASSERT_TRUE(SelectiveSamplingShouldProduceThreadSample());
}

TEST_F(SelectiveSamplingBufferTest, InvalidReadDoesNotClearSaturation)
{
    std::vector<unsigned char> acceptedSample = {0x11, 0x22};
    std::vector<unsigned char> overflowingSample(kTestSamplesBufferMaximumSize - acceptedSample.size() + 1, 0x33);
    SelectiveSamplingRecordProducedThreadSample(static_cast<int32_t>(acceptedSample.size()), acceptedSample.data());
    SelectiveSamplingRecordProducedThreadSample(static_cast<int32_t>(overflowingSample.size()),
                                                overflowingSample.data());

    ASSERT_FALSE(SelectiveSamplingShouldProduceThreadSample());
    ASSERT_EQ(0, SelectiveSamplerReadThreadSamples(0, nullptr));
    ASSERT_FALSE(SelectiveSamplingShouldProduceThreadSample());
}

TEST_F(SelectiveSamplingPreparationTest, SaturationDoesNotPreventOutdatedTraceCleanup)
{
    std::vector<unsigned char> acceptedSample = {0x11, 0x22};
    std::vector<unsigned char> overflowingSample(kTestSamplesBufferMaximumSize - acceptedSample.size() + 1, 0x33);
    SelectiveSamplingRecordProducedThreadSample(static_cast<int32_t>(acceptedSample.size()), acceptedSample.data());
    SelectiveSamplingRecordProducedThreadSample(static_cast<int32_t>(overflowingSample.size()),
                                                overflowingSample.data());
    ASSERT_FALSE(SelectiveSamplingShouldProduceThreadSample());

    continuous_profiler::ContinuousProfiler profiler{};
    const auto                              now = std::chrono::steady_clock::now();
    profiler.nextOutdatedEntriesScan            = now;
    ASSERT_TRUE(continuous_profiler::TryAddSelectiveSamplingTrace({kTestTraceIdHigh, kTestTraceIdLow}, now));

    ASSERT_FALSE(continuous_profiler::TryPrepareSelectedThreadSampling(&profiler, now + std::chrono::minutes(16)));
    ASSERT_FALSE(SelectiveSamplingShouldProduceThreadSample());

    auto output = CreateReadBuffer();
    ASSERT_EQ(acceptedSample.size(), static_cast<size_t>(Drain(output)));
    ASSERT_FALSE(continuous_profiler::TryPrepareSelectedThreadSampling(&profiler, now));
}

TEST_F(SelectiveSamplingPreparationTest, EmptyTraceSetPreventsSelectedThreadSampling)
{
    continuous_profiler::ContinuousProfiler profiler{};
    const auto                              now = std::chrono::steady_clock::now();
    profiler.nextOutdatedEntriesScan            = now + std::chrono::minutes(1);

    ASSERT_TRUE(SelectiveSamplingShouldProduceThreadSample());
    ASSERT_FALSE(continuous_profiler::TryPrepareSelectedThreadSampling(&profiler, now));

    ASSERT_TRUE(continuous_profiler::TryAddSelectiveSamplingTrace({kTestTraceIdHigh, kTestTraceIdLow}, now));
    ASSERT_TRUE(continuous_profiler::TryPrepareSelectedThreadSampling(&profiler, now));
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
