#include "pch.h"

#include "../../src/OpenTelemetry.AutoInstrumentation.Native/continuous_profiler.h"

#include <cstdint>
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
    }

    static void Record(const std::initializer_list<unsigned char> values, const uint32_t interval)
    {
        ThreadSamplingRecordProducedThreadSample(new std::vector<unsigned char>(values), interval);
    }
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
