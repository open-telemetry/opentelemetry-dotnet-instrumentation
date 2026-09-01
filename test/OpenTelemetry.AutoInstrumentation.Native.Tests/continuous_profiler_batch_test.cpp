// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#include "pch.h"
#include "../../src/OpenTelemetry.AutoInstrumentation.Native/continuous_profiler.h"

#include <cstdint>
#include <string>
#include <type_traits>
#include <vector>

namespace
{

constexpr std::size_t kTestSamplesBufferMaximumSize = 200 * 1024;

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
        unsigned int  interval = 0;
        while (ContinuousProfilerReadThreadSamplesV2(sizeof(output), output, &interval) > 0)
        {
        }
    }

    static void Record(const unsigned char value, const unsigned int interval)
    {
        ThreadSamplingRecordProducedThreadSample(new std::vector<unsigned char>{value}, interval);
    }

    static void Record(const std::initializer_list<unsigned char> values, const unsigned int interval)
    {
        ThreadSamplingRecordProducedThreadSample(new std::vector<unsigned char>(values), interval);
    }
};

class ContinuousProfilerAuxiliaryBufferTest : public ::testing::Test
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
        std::vector<unsigned char> output(kTestSamplesBufferMaximumSize);
        ContinuousProfilerReadAllocationSamples(static_cast<std::int32_t>(output.size()), output.data());
        SelectiveSamplerReadThreadSamples(static_cast<std::int32_t>(output.size()), output.data());
    }
};

} // namespace

TEST_F(ContinuousProfilerBatchTest, ExtendedReaderReturnsTheIntervalStoredWithEachBatch)
{
    using ExtendedReader = std::int32_t(STDAPICALLTYPE*)(std::int32_t, unsigned char*, std::uint32_t*);
    static_assert(std::is_same<decltype(&ContinuousProfilerReadThreadSamplesV2), ExtendedReader>::value,
                  "The V2 reader ABI changed.");

    Record(0x11, 1000u);
    Record(0x22, 2000u);

    unsigned char output[1];
    unsigned int  interval = 0;

    ASSERT_EQ(1, ContinuousProfilerReadThreadSamplesV2(sizeof(output), output, &interval));
    ASSERT_EQ(0x11, output[0]);
    ASSERT_EQ(1000u, interval);

    ASSERT_EQ(1, ContinuousProfilerReadThreadSamplesV2(sizeof(output), output, &interval));
    ASSERT_EQ(0x22, output[0]);
    ASSERT_EQ(2000u, interval);

    interval = 99u;
    ASSERT_EQ(0, ContinuousProfilerReadThreadSamplesV2(sizeof(output), output, &interval));
    ASSERT_EQ(0u, interval);
}

TEST_F(ContinuousProfilerBatchTest, LegacyReaderKeepsItsAbiAndConsumesTheSameQueue)
{
    using LegacyReader = std::int32_t (*)(std::int32_t, unsigned char*);
    static_assert(std::is_same<decltype(&ContinuousProfilerReadThreadSamples), LegacyReader>::value,
                  "The legacy reader ABI changed.");

    Record(0x11, 1000u);
    Record(0x22, 2000u);

    unsigned char output[1];
    ASSERT_EQ(1, ContinuousProfilerReadThreadSamples(sizeof(output), output));
    ASSERT_EQ(0x11, output[0]);

    unsigned int interval = 0;
    ASSERT_EQ(1, ContinuousProfilerReadThreadSamplesV2(sizeof(output), output, &interval));
    ASSERT_EQ(0x22, output[0]);
    ASSERT_EQ(2000u, interval);
}

TEST_F(ContinuousProfilerBatchTest, InvalidExtendedReadDoesNotConsumeThePendingBatch)
{
    Record(0x11, 1000u);

    unsigned char output[1];
    unsigned int  interval = 99u;
    ASSERT_EQ(0, ContinuousProfilerReadThreadSamplesV2(0, output, &interval));
    ASSERT_EQ(0u, interval);

    ASSERT_EQ(0, ContinuousProfilerReadThreadSamplesV2(sizeof(output), output, nullptr));

    ASSERT_EQ(1, ContinuousProfilerReadThreadSamplesV2(sizeof(output), output, &interval));
    ASSERT_EQ(0x11, output[0]);
    ASSERT_EQ(1000u, interval);
}

TEST_F(ContinuousProfilerBatchTest, UndersizedExtendedReadDropsTheWholeBatchWithoutReturningAPrefix)
{
    Record({0x11, 0x22}, 1000u);

    unsigned char output   = 0xEE;
    unsigned int  interval = 99u;
    ASSERT_EQ(0, ContinuousProfilerReadThreadSamplesV2(sizeof(output), &output, &interval));
    ASSERT_EQ(0xEE, output);
    ASSERT_EQ(0u, interval);

    unsigned char remaining[2] = {0xEE, 0xEE};
    interval                   = 99u;
    ASSERT_EQ(0, ContinuousProfilerReadThreadSamplesV2(sizeof(remaining), remaining, &interval));
    ASSERT_EQ(0u, interval);
    ASSERT_TRUE(ThreadSamplingShouldProduceThreadSample());
}

TEST_F(ContinuousProfilerBatchTest, UndersizedLegacyReadDropsTheWholeBatchWithoutReturningAPrefix)
{
    Record({0x11, 0x22}, 1000u);

    unsigned char output = 0xEE;
    ASSERT_EQ(0, ContinuousProfilerReadThreadSamples(sizeof(output), &output));
    ASSERT_EQ(0xEE, output);

    unsigned char remaining[2] = {0xEE, 0xEE};
    unsigned int  interval     = 99u;
    ASSERT_EQ(0, ContinuousProfilerReadThreadSamplesV2(sizeof(remaining), remaining, &interval));
    ASSERT_EQ(0u, interval);
    ASSERT_TRUE(ThreadSamplingShouldProduceThreadSample());
}

TEST_F(ContinuousProfilerBatchTest, OverflowedWriterDoesNotPublishAMalformedBatch)
{
    continuous_profiler::ContinuousProfiler profiler;
    profiler.AllocateBuffer();

    ASSERT_NE(nullptr, profiler.cur_cpu_writer_);
    profiler.cur_cpu_writer_->StartBatch();

    continuous_profiler::ThreadState state;
    profiler.cur_cpu_writer_->StartSample(&state, {});

    const trace::WSTRING largeFrame(512, static_cast<WCHAR>('x'));
    for (std::uint32_t index = 1; index <= 300; index++)
    {
        profiler.cur_cpu_writer_
            ->RecordFrame(continuous_profiler::FunctionIdentifier::Managed(static_cast<mdToken>(index), 1, true),
                          largeFrame);
    }

    profiler.cur_cpu_writer_->EndSample();
    profiler.cur_cpu_writer_->EndBatch();
    profiler.cur_cpu_writer_->WriteFinalStats({});
    profiler.PublishBuffer(1000u);

    std::vector<unsigned char> output(kTestSamplesBufferMaximumSize + 2048, 0xEE);
    unsigned int               interval = 99u;
    ASSERT_EQ(0, ContinuousProfilerReadThreadSamplesV2(static_cast<int32_t>(output.size()), output.data(), &interval));
    ASSERT_EQ(0u, interval);
    ASSERT_TRUE(ThreadSamplingShouldProduceThreadSample());
}

TEST_F(ContinuousProfilerBatchTest, FinalMultiByteWriteMarksABatchThatCrossesTheCapacityBoundary)
{
    std::vector<unsigned char>               bytes(kTestSamplesBufferMaximumSize - 1);
    continuous_profiler::ThreadSamplesBuffer writer(&bytes);

    writer.WriteFinalStats({});

    ASSERT_TRUE(writer.IsOverflowed());
}

TEST_F(ContinuousProfilerBatchTest, BackPressureDropsAWholeBatchWithoutMismatchingMetadata)
{
    Record(0x11, 1000u);
    Record(0x22, 2000u);
    ASSERT_FALSE(ThreadSamplingShouldProduceThreadSample());

    Record(0x33, 3000u);

    unsigned char output[1];
    unsigned int  interval = 0;
    ASSERT_EQ(1, ContinuousProfilerReadThreadSamplesV2(sizeof(output), output, &interval));
    ASSERT_EQ(0x11, output[0]);
    ASSERT_EQ(1000u, interval);

    ASSERT_EQ(1, ContinuousProfilerReadThreadSamplesV2(sizeof(output), output, &interval));
    ASSERT_EQ(0x22, output[0]);
    ASSERT_EQ(2000u, interval);

    ASSERT_EQ(0, ContinuousProfilerReadThreadSamplesV2(sizeof(output), output, &interval));
    ASSERT_EQ(0u, interval);
    ASSERT_TRUE(ThreadSamplingShouldProduceThreadSample());
}

TEST_F(ContinuousProfilerAuxiliaryBufferTest, UndersizedAllocationReadDropsTheWholeSampleWithoutReturningAPrefix)
{
    unsigned char sample[2] = {0x11, 0x22};
    AllocationSamplingAppendToBuffer(static_cast<std::int32_t>(sizeof(sample)), sample);

    unsigned char output = 0xEE;
    ASSERT_EQ(0, ContinuousProfilerReadAllocationSamples(static_cast<std::int32_t>(sizeof(output)), &output));
    ASSERT_EQ(0xEE, output);

    unsigned char remaining[2] = {0xEE, 0xEE};
    ASSERT_EQ(0, ContinuousProfilerReadAllocationSamples(static_cast<std::int32_t>(sizeof(remaining)), remaining));
    ASSERT_TRUE(AllocationSamplingShouldProduceSample());
}

TEST_F(ContinuousProfilerAuxiliaryBufferTest, AllocationBackPressureSkipsCaptureUntilTheBufferIsRead)
{
    std::vector<unsigned char> oversizedSample(kTestSamplesBufferMaximumSize + 1, 0x33);
    AllocationSamplingAppendToBuffer(static_cast<std::int32_t>(oversizedSample.size()), oversizedSample.data());

    ASSERT_FALSE(AllocationSamplingShouldProduceSample());

    std::vector<unsigned char> output(kTestSamplesBufferMaximumSize);
    ASSERT_EQ(0, ContinuousProfilerReadAllocationSamples(static_cast<std::int32_t>(output.size()), output.data()));
    ASSERT_TRUE(AllocationSamplingShouldProduceSample());
}

TEST_F(ContinuousProfilerAuxiliaryBufferTest, UndersizedSelectiveReadDropsTheWholeBatchWithoutReturningAPrefix)
{
    unsigned char sample[2] = {0x11, 0x22};
    SelectiveSamplingRecordProducedThreadSample(static_cast<std::int32_t>(sizeof(sample)), sample);

    unsigned char output = 0xEE;
    ASSERT_EQ(0, SelectiveSamplerReadThreadSamples(static_cast<std::int32_t>(sizeof(output)), &output));
    ASSERT_EQ(0xEE, output);

    unsigned char remaining[2] = {0xEE, 0xEE};
    ASSERT_EQ(0, SelectiveSamplerReadThreadSamples(static_cast<std::int32_t>(sizeof(remaining)), remaining));
    ASSERT_TRUE(SelectiveSamplingShouldProduceThreadSample());
}
