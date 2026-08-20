#include "pch.h"
#include "../../src/OpenTelemetry.AutoInstrumentation.Native/continuous_profiler.h"

#include <algorithm>
#include <chrono>
#include <cstdint>
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
        continuous_profiler::ContinuousProfiler::ClearGlobalInfo();
        continuous_profiler::RemoveSelectiveSamplingTrace({kTestTraceIdHigh, kTestTraceIdLow});
    }

    void TearDown() override
    {
        continuous_profiler::ContinuousProfiler::ClearGlobalInfo();
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

TEST(ContinuousProfilerConfigurationTest, ThreadSamplingConfigurationIsIdempotentAndRetainsInterval)
{
    continuous_profiler::ContinuousProfiler profiler;

    ASSERT_FALSE(profiler.SetThreadSamplingEnabled(true));
    ASSERT_FALSE(profiler.IsThreadSamplingThreadRunning());

    ASSERT_TRUE(profiler.SetThreadSamplingInterval(10000u));
    ASSERT_TRUE(profiler.SetThreadSamplingInterval(10000u));
    ASSERT_FALSE(profiler.GetThreadSamplingConfiguration().threadSamplingInterval.has_value());

    ASSERT_TRUE(profiler.SetThreadSamplingEnabled(true));
    ASSERT_TRUE(profiler.IsThreadSamplingThreadRunning());
    auto configuration = profiler.GetThreadSamplingConfiguration();
    ASSERT_EQ(10000u, configuration.threadSamplingInterval.value());

    ASSERT_TRUE(profiler.SetThreadSamplingInterval(1234u));
    ASSERT_TRUE(profiler.IsThreadSamplingThreadRunning());
    configuration = profiler.GetThreadSamplingConfiguration();
    ASSERT_EQ(1234u, configuration.threadSamplingInterval.value());
    ASSERT_EQ(1234u, profiler.GetConfiguredThreadSamplingInterval());

    ASSERT_FALSE(profiler.SetThreadSamplingInterval(0));
    configuration = profiler.GetThreadSamplingConfiguration();
    ASSERT_EQ(1234u, configuration.threadSamplingInterval.value());

    ASSERT_TRUE(profiler.SetThreadSamplingEnabled(true));
    ASSERT_EQ(1234u, profiler.GetThreadSamplingConfiguration().threadSamplingInterval.value());

    ASSERT_TRUE(profiler.SetThreadSamplingEnabled(false));
    ASSERT_FALSE(profiler.IsThreadSamplingThreadRunning());
    ASSERT_FALSE(profiler.GetThreadSamplingConfiguration().threadSamplingInterval.has_value());
    ASSERT_EQ(1234u, profiler.GetConfiguredThreadSamplingInterval());

    ASSERT_TRUE(profiler.SetThreadSamplingEnabled(false));
    ASSERT_FALSE(profiler.IsThreadSamplingThreadRunning());

    ASSERT_TRUE(profiler.SetThreadSamplingEnabled(true));
    ASSERT_TRUE(profiler.IsThreadSamplingThreadRunning());
    ASSERT_EQ(1234u, profiler.GetThreadSamplingConfiguration().threadSamplingInterval.value());

    ASSERT_TRUE(profiler.SetThreadSamplingEnabled(false));
}

TEST(ContinuousProfilerConfigurationTest, CpuAndSnapshotConfigurationRemainIndependentAndIdempotent)
{
    continuous_profiler::ContinuousProfiler profiler;

    ASSERT_TRUE(profiler.SetThreadSamplingInterval(1000u));
    ASSERT_TRUE(profiler.SetThreadSamplingEnabled(true));
    ASSERT_TRUE(profiler.SetThreadSamplingEnabled(true));
    ASSERT_TRUE(profiler.IsThreadSamplingThreadRunning());

    ASSERT_FALSE(profiler.SetSelectedThreadSamplingInterval(1000u));
    ASSERT_FALSE(profiler.SetSelectedThreadSamplingInterval(600u));
    ASSERT_EQ(0u, profiler.GetConfiguredSelectedThreadSamplingInterval());
    ASSERT_TRUE(profiler.SetSelectedThreadSamplingInterval(100u));
    ASSERT_TRUE(profiler.SetSelectedThreadSamplingEnabled(true));
    ASSERT_TRUE(profiler.SetSelectedThreadSamplingEnabled(true));
    auto configuration = profiler.GetThreadSamplingConfiguration();
    ASSERT_EQ(1000u, configuration.threadSamplingInterval.value());
    ASSERT_EQ(100u, configuration.selectedThreadsSamplingInterval.value());

    ASSERT_TRUE(profiler.SetSelectedThreadSamplingEnabled(false));
    ASSERT_TRUE(profiler.SetSelectedThreadSamplingEnabled(false));
    ASSERT_TRUE(profiler.IsThreadSamplingThreadRunning());
    configuration = profiler.GetThreadSamplingConfiguration();
    ASSERT_EQ(1000u, configuration.threadSamplingInterval.value());
    ASSERT_FALSE(configuration.selectedThreadsSamplingInterval.has_value());
    ASSERT_EQ(100u, profiler.GetConfiguredSelectedThreadSamplingInterval());

    ASSERT_TRUE(profiler.SetSelectedThreadSamplingEnabled(true));
    ASSERT_FALSE(profiler.SetThreadSamplingInterval(100u));
    ASSERT_FALSE(profiler.SetThreadSamplingInterval(150u));
    ASSERT_EQ(1000u, profiler.GetConfiguredThreadSamplingInterval());

    ASSERT_TRUE(profiler.SetThreadSamplingEnabled(false));
    ASSERT_TRUE(profiler.SetThreadSamplingEnabled(false));
    ASSERT_TRUE(profiler.IsThreadSamplingThreadRunning());
    configuration = profiler.GetThreadSamplingConfiguration();
    ASSERT_FALSE(configuration.threadSamplingInterval.has_value());
    ASSERT_EQ(100u, configuration.selectedThreadsSamplingInterval.value());

    ASSERT_TRUE(profiler.SetSelectedThreadSamplingEnabled(false));
    ASSERT_FALSE(profiler.IsThreadSamplingThreadRunning());
}

TEST(ContinuousProfilerConfigurationTest, ShutdownIsIdempotentAndRejectsReconfiguration)
{
    continuous_profiler::ContinuousProfiler profiler;

    ASSERT_TRUE(profiler.SetSelectedThreadSamplingInterval(100u));
    ASSERT_TRUE(profiler.SetSelectedThreadSamplingEnabled(true));
    ASSERT_TRUE(profiler.SetThreadSamplingInterval(1000u));
    ASSERT_TRUE(profiler.SetThreadSamplingEnabled(true));
    ASSERT_TRUE(profiler.IsThreadSamplingThreadRunning());

    profiler.Shutdown();

    ASSERT_TRUE(profiler.IsShutdownRequested());
    ASSERT_FALSE(profiler.IsThreadSamplingThreadRunning());
    ASSERT_EQ(0u, profiler.GetAllocationSamplingRate());
    ASSERT_FALSE(profiler.SetThreadSamplingEnabled(true));
    ASSERT_FALSE(profiler.SetThreadSamplingInterval(1000u));
    ASSERT_FALSE(profiler.SetAllocationSamplingConfiguration(true, 100u));
    ASSERT_FALSE(profiler.SetSelectedThreadSamplingEnabled(true));
    ASSERT_FALSE(profiler.SetSelectedThreadSamplingInterval(100u));
    ASSERT_FALSE(profiler.SetAllocationSamplingEnabled(true));
    ASSERT_FALSE(profiler.SetMaxMemorySamplesPerMinute(100u));

    // The rollback is intentionally idempotent and permanently fail-closed.
    profiler.Shutdown();
    ASSERT_FALSE(profiler.IsThreadSamplingThreadRunning());
    ASSERT_EQ(0u, profiler.GetAllocationSamplingRate());
}

TEST(ContinuousProfilerConfigurationTest, AllocationSamplingValidatesConfigurationAndUnsupportedRuntime)
{
    continuous_profiler::ContinuousProfiler profiler;

    ASSERT_FALSE(profiler.SetMaxMemorySamplesPerMinute(0u));
    ASSERT_EQ(0u, profiler.GetConfiguredMaxMemorySamplesPerMinute());
    ASSERT_FALSE(profiler.SetAllocationSamplingEnabled(true));
    ASSERT_TRUE(profiler.SetAllocationSamplingEnabled(false));
    ASSERT_FALSE(profiler.SetAllocationSamplingConfiguration(true, 0u));

    ASSERT_TRUE(profiler.SetMaxMemorySamplesPerMinute(100u));
    ASSERT_EQ(100u, profiler.GetConfiguredMaxMemorySamplesPerMinute());
    ASSERT_FALSE(profiler.SetAllocationSamplingEnabled(true));
    ASSERT_EQ(0u, profiler.GetAllocationSamplingRate());
    ASSERT_EQ(100u, profiler.GetConfiguredMaxMemorySamplesPerMinute());

    ASSERT_TRUE(profiler.SetMaxMemorySamplesPerMinute(200u));
    ASSERT_EQ(200u, profiler.GetConfiguredMaxMemorySamplesPerMinute());
    ASSERT_TRUE(profiler.SetAllocationSamplingEnabled(false));

    ASSERT_FALSE(profiler.SetAllocationSamplingConfiguration(true, 100u));
    ASSERT_EQ(0u, profiler.GetAllocationSamplingRate());
    ASSERT_TRUE(profiler.SetAllocationSamplingConfiguration(false, 0u));
}

TEST(ContinuousProfilerBufferTest, NativeReadersPreserveBatchMetadataAndLegacyAbi)
{
    unsigned char output[1];
    unsigned int  samplingInterval;
    while (ContinuousProfilerReadThreadSamplesV2(sizeof(output), output, &samplingInterval) > 0)
    {
    }
    ASSERT_EQ(0u, samplingInterval);

    ThreadSamplingRecordProducedThreadSample(new std::vector<unsigned char>{0x11}, 10000u);
    ThreadSamplingRecordProducedThreadSample(new std::vector<unsigned char>{0x22}, 20000u);
    ASSERT_FALSE(ThreadSamplingShouldProduceThreadSample());

    ASSERT_EQ(1, ContinuousProfilerReadThreadSamplesV2(sizeof(output), output, &samplingInterval));
    ASSERT_EQ(0x11, output[0]);
    ASSERT_EQ(10000u, samplingInterval);

    ThreadSamplingRecordProducedThreadSample(new std::vector<unsigned char>{0x33}, 30000u);
    ASSERT_FALSE(ThreadSamplingShouldProduceThreadSample());

    ASSERT_EQ(1, ContinuousProfilerReadThreadSamples(sizeof(output), output));
    ASSERT_EQ(0x22, output[0]);

    ASSERT_EQ(1, ContinuousProfilerReadThreadSamplesV2(sizeof(output), output, &samplingInterval));
    ASSERT_EQ(0x33, output[0]);
    ASSERT_EQ(30000u, samplingInterval);

    ASSERT_EQ(0, ContinuousProfilerReadThreadSamplesV2(sizeof(output), output, &samplingInterval));
    ASSERT_EQ(0u, samplingInterval);
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

TEST_F(SelectiveSamplingPreparationTest, GlobalInfoPublicationIsExplicitAndClearRemovesSelectiveState)
{
    continuous_profiler::ContinuousProfiler::ClearGlobalInfo();

    continuous_profiler::ContinuousProfiler profiler{};
    const auto                              now = std::chrono::steady_clock::now();
    profiler.nextOutdatedEntriesScan            = now + std::chrono::minutes(1);

    // SelectiveSamplingStart only uses the pointer as a publication gate. Keeping
    // this deliberately non-dereferenceable makes an accidental CLR call fail fast.
    profiler.SetGlobalInfo7(reinterpret_cast<ICorProfilerInfo7*>(std::uintptr_t{1}));
    SelectiveSamplingStart(kTestTraceIdHigh, kTestTraceIdLow);
    ASSERT_FALSE(continuous_profiler::TryPrepareSelectedThreadSampling(&profiler, now));

    profiler.PublishGlobalInfo();
    SelectiveSamplingStart(kTestTraceIdHigh, kTestTraceIdLow);
    ASSERT_TRUE(continuous_profiler::TryPrepareSelectedThreadSampling(&profiler, now));

    continuous_profiler::ContinuousProfiler::ClearGlobalInfo();
    ASSERT_FALSE(continuous_profiler::TryPrepareSelectedThreadSampling(&profiler, now));
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
