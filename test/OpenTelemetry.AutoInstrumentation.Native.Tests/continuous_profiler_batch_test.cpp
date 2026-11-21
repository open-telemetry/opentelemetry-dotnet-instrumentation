#include "pch.h"

#include "../../src/OpenTelemetry.AutoInstrumentation.Native/continuous_profiler.h"

#include <cstdint>
#include <cstdlib>
#include <string>
#include <type_traits>
#include <vector>

namespace
{

class ContinuousProfilerBatchTest : public ::testing::Test
{
protected:
    void SetUp() override
    {
        Drain();
    }

    void TearDown() override
    {
        Drain();
    }

    static void Drain()
    {
        unsigned char output[8];
        uint32_t      interval = 0;
        while (ContinuousProfilerReadThreadSamplesV2(sizeof(output), output, &interval) > 0)
        {
        }
        std::vector<unsigned char> selectiveOutput(200 * 1024);
        SelectiveSamplerReadThreadSamples(static_cast<int32_t>(selectiveOutput.size()), selectiveOutput.data());
    }

    static void Record(const std::initializer_list<unsigned char> values, const uint32_t interval)
    {
        ThreadSamplingRecordProducedThreadSample(new std::vector<unsigned char>(values), interval);
    }
};

class ContinuousProfilerBatchDeathTest : public ContinuousProfilerBatchTest
{
protected:
    void SetUp() override
    {
        ContinuousProfilerBatchTest::SetUp();
        // Shutdown permanently closes admission, so publication must be tested in a fresh process.
        previousDeathTestStyle_                 = ::testing::FLAGS_gtest_death_test_style;
        ::testing::FLAGS_gtest_death_test_style = "threadsafe";
    }

    void TearDown() override
    {
        ::testing::FLAGS_gtest_death_test_style = previousDeathTestStyle_;
        ContinuousProfilerBatchTest::TearDown();
    }

    template <typename Action>
    static void RunInChild(Action action)
    {
        action();
        std::exit(::testing::Test::HasFailure() ? EXIT_FAILURE : EXIT_SUCCESS);
    }

    std::string previousDeathTestStyle_;
};

} // namespace

TEST_F(ContinuousProfilerBatchTest, V2ReturnsTheIntervalCapturedWithEachBatch)
{
    using Reader = int32_t(STDAPICALLTYPE*)(int32_t, unsigned char*, uint32_t*);
    static_assert(std::is_same_v<decltype(&ContinuousProfilerReadThreadSamplesV2), Reader>);

    Record({0x11}, 1000);
    Record({0x22}, 2000);

    unsigned char output   = 0;
    uint32_t      interval = 0;
    ASSERT_EQ(1, ContinuousProfilerReadThreadSamplesV2(sizeof(output), &output, &interval));
    EXPECT_EQ(0x11, output);
    EXPECT_EQ(1000u, interval);

    ASSERT_EQ(1, ContinuousProfilerReadThreadSamplesV2(sizeof(output), &output, &interval));
    EXPECT_EQ(0x22, output);
    EXPECT_EQ(2000u, interval);
}

TEST_F(ContinuousProfilerBatchTest, LegacyAndV2ReadersConsumeTheSameFifo)
{
    using LegacyReader = int32_t (*)(int32_t, unsigned char*);
    static_assert(std::is_same_v<decltype(&ContinuousProfilerReadThreadSamples), LegacyReader>);

    Record({0x11}, 1000);
    Record({0x22}, 2000);

    unsigned char output = 0;
    ASSERT_EQ(1, ContinuousProfilerReadThreadSamples(sizeof(output), &output));
    EXPECT_EQ(0x11, output);

    uint32_t interval = 0;
    ASSERT_EQ(1, ContinuousProfilerReadThreadSamplesV2(sizeof(output), &output, &interval));
    EXPECT_EQ(0x22, output);
    EXPECT_EQ(2000u, interval);
}

TEST_F(ContinuousProfilerBatchTest, InvalidV2ReadDoesNotConsumeThePendingBatch)
{
    Record({0x11}, 1000);

    unsigned char output   = 0;
    uint32_t      interval = 99;
    EXPECT_EQ(0, ContinuousProfilerReadThreadSamplesV2(0, &output, &interval));
    EXPECT_EQ(0u, interval);
    EXPECT_EQ(0, ContinuousProfilerReadThreadSamplesV2(sizeof(output), &output, nullptr));

    ASSERT_EQ(1, ContinuousProfilerReadThreadSamplesV2(sizeof(output), &output, &interval));
    EXPECT_EQ(0x11, output);
    EXPECT_EQ(1000u, interval);
}

TEST_F(ContinuousProfilerBatchTest, UndersizedReadDropsAWholeBatchWithoutReturningAPrefix)
{
    Record({0x11, 0x22}, 1000);

    unsigned char output   = 0xEE;
    uint32_t      interval = 99;
    EXPECT_EQ(0, ContinuousProfilerReadThreadSamplesV2(sizeof(output), &output, &interval));
    EXPECT_EQ(0xEE, output);
    EXPECT_EQ(0u, interval);

    unsigned char complete[2] = {0xEE, 0xEE};
    EXPECT_EQ(0, ContinuousProfilerReadThreadSamplesV2(sizeof(complete), complete, &interval));
}

TEST_F(ContinuousProfilerBatchTest, ProducerRefillDoesNotOvertakeTheOlderSecondBatch)
{
    Record({0x11}, 1000);
    Record({0x22}, 2000);

    unsigned char output   = 0;
    uint32_t      interval = 0;
    ASSERT_EQ(1, ContinuousProfilerReadThreadSamplesV2(sizeof(output), &output, &interval));
    ASSERT_EQ(0x11, output);

    Record({0x33}, 3000);

    ASSERT_EQ(1, ContinuousProfilerReadThreadSamplesV2(sizeof(output), &output, &interval));
    EXPECT_EQ(0x22, output);
    EXPECT_EQ(2000u, interval);
    ASSERT_EQ(1, ContinuousProfilerReadThreadSamplesV2(sizeof(output), &output, &interval));
    EXPECT_EQ(0x33, output);
    EXPECT_EQ(3000u, interval);
}

TEST_F(ContinuousProfilerBatchTest, SelectiveBatchesRetainTheirOwnIntervalsInACombinedRead)
{
    for (const uint32_t interval : {40u, 80u})
    {
        std::vector<unsigned char>               bytes;
        continuous_profiler::ThreadSamplesBuffer writer(&bytes);
        writer.StartSelectedThreadsBatch(interval);
        writer.EndSelectedThreadsBatch();
        ASSERT_FALSE(writer.IsOverflowed());
        SelectiveSamplingRecordProducedThreadSample(static_cast<int32_t>(bytes.size()), bytes.data());
    }

    const std::vector<unsigned char> expected = {0x0A, 0, 0, 0, 1, 0, 0, 0, 40, 0x0B,
                                                 0x0A, 0, 0, 0, 1, 0, 0, 0, 80, 0x0B};
    std::vector<unsigned char>       output(expected.size());
    ASSERT_EQ(expected.size(), SelectiveSamplerReadThreadSamples(static_cast<int32_t>(output.size()), output.data()));
    EXPECT_EQ(expected, output);
}

TEST_F(ContinuousProfilerBatchDeathTest, CpuBatchRetainsItsIntervalFromItsConfigurationSnapshot)
{
    EXPECT_EXIT(RunInChild(
                    []
                    {
                        continuous_profiler::ClrAllocationSamplingSessionProvider allocationSessions(nullptr);
                        continuous_profiler::ContinuousProfiler                   profiler(allocationSessions);
                        profiler.StageThreadSamplingConfiguration(400, 40);
                        const auto configuration = profiler.PullThreadSamplingConfiguration();

                        // A committed update must not change the intervals of an already admitted capture cycle.
                        profiler.StageThreadSamplingConfiguration(800, 80);
                        profiler.AllocateBuffer();
                        profiler.cur_cpu_writer_->StartBatch();
                        profiler.cur_cpu_writer_->EndBatch();
                        profiler.PublishBuffer(configuration.cpuSamplingIntervalMilliseconds);

                        std::vector<unsigned char> output(14);
                        uint32_t                   cpuInterval = 0;
                        ASSERT_EQ(output.size(),
                                  ContinuousProfilerReadThreadSamplesV2(static_cast<int32_t>(output.size()),
                                                                        output.data(), &cpuInterval));
                        EXPECT_EQ(400u, cpuInterval);
                        EXPECT_EQ((std::vector<unsigned char>{0x01, 0, 0, 0, 1}),
                                  (std::vector<unsigned char>(output.begin(), output.begin() + 5)));
                        // The batch ends after the 8-byte timestamp, independently of the platform's long width.
                        EXPECT_EQ((std::vector<unsigned char>{0x06}),
                                  (std::vector<unsigned char>(output.begin() + 13, output.end())));
                    }),
                ::testing::ExitedWithCode(EXIT_SUCCESS), "");
}

TEST_F(ContinuousProfilerBatchTest, CpuBatchHeaderContainsOnlyVersionAndCaptureTimestamp)
{
    std::vector<unsigned char>               bytes;
    continuous_profiler::ThreadSamplesBuffer writer(&bytes);
    writer.StartBatch(0x0102030405060708);
    writer.EndBatch();

    // Protocol timestamps have a fixed 8-byte width on both Windows and Unix.
    EXPECT_EQ((std::vector<unsigned char>{0x01, 0, 0, 0, 1, 1, 2, 3, 4, 5, 6, 7, 8, 0x06}), bytes);
}

TEST_F(ContinuousProfilerBatchTest, OverflowedWriterDoesNotPublishAMalformedBatch)
{
    continuous_profiler::ClrAllocationSamplingSessionProvider allocationSessions(nullptr);
    continuous_profiler::ContinuousProfiler                   profiler(allocationSessions);
    profiler.AllocateBuffer();
    ASSERT_NE(nullptr, profiler.cur_cpu_writer_);

    profiler.cur_cpu_writer_->StartBatch();
    continuous_profiler::ThreadState state;
    profiler.cur_cpu_writer_->StartSample(&state, {});

    const trace::WSTRING largeFrame(512, static_cast<WCHAR>('x'));
    for (uint32_t index = 1; index <= 300; index++)
    {
        profiler.cur_cpu_writer_
            ->RecordFrame(continuous_profiler::FunctionIdentifier::Managed(static_cast<mdToken>(index), 1, true),
                          largeFrame);
    }

    profiler.cur_cpu_writer_->EndSample();
    profiler.cur_cpu_writer_->EndBatch();
    profiler.cur_cpu_writer_->WriteFinalStats({});
    profiler.PublishBuffer(1000);

    std::vector<unsigned char> output(202 * 1024, 0xEE);
    uint32_t                   interval = 99;
    EXPECT_EQ(0, ContinuousProfilerReadThreadSamplesV2(static_cast<int32_t>(output.size()), output.data(), &interval));
    EXPECT_EQ(0u, interval);
}

TEST_F(ContinuousProfilerBatchTest, ShutdownDiscardsAnUnpublishedCpuBatch)
{
    continuous_profiler::ClrAllocationSamplingSessionProvider allocationSessions(nullptr);
    continuous_profiler::ContinuousProfiler                   profiler(allocationSessions);
    profiler.AllocateBuffer();
    ASSERT_NE(nullptr, profiler.cur_cpu_writer_);
    profiler.cur_cpu_writer_->StartBatch();
    profiler.cur_cpu_writer_->EndBatch();

    profiler.Shutdown();
    profiler.PublishBuffer(1000);

    unsigned char output[64] = {};
    uint32_t      interval   = 99;
    EXPECT_EQ(0, ContinuousProfilerReadThreadSamplesV2(sizeof(output), output, &interval));
    EXPECT_EQ(0u, interval);
}
