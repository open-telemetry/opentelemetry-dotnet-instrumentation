// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#include "pch.h"

#include "../../src/OpenTelemetry.AutoInstrumentation.Native/continuous_profiler.h"
#include "../../src/OpenTelemetry.AutoInstrumentation.Native/stack_capture_types.h"

#include <algorithm>
#include <array>
#include <cstdlib>
#include <functional>
#include <string>
#include <vector>

using namespace continuous_profiler;

namespace
{

constexpr uint64_t kTraceHigh = 0x1011;
constexpr uint64_t kTraceLow  = 0x1022;
constexpr uint64_t kSpanId    = 0x1033;

// These COM test doubles populate only the slots used by CaptureSamples and context tracking.
// IUnknown::Release, ICorProfilerThreadEnum::Next, ICorProfilerInfo::GetCurrentThreadID and
// ICorProfilerInfo4::EnumThreads occupy slots 2, 7, 13 and 71 on both 32-bit and 64-bit platforms.
class CohortThreadEnumerator
{
public:
    void**   vtable     = Methods();
    ThreadID nextThread = 1;

private:
    static ULONG STDMETHODCALLTYPE Release(ICorProfilerThreadEnum*)
    {
        return 1; // The fixture owns the enumerator.
    }

    static HRESULT STDMETHODCALLTYPE Next(ICorProfilerThreadEnum* instance, ULONG count, ThreadID* ids, ULONG* fetched)
    {
        auto* self = reinterpret_cast<CohortThreadEnumerator*>(instance);
        *fetched   = 0;
        while (*fetched < count && self->nextThread <= 2)
        {
            ids[(*fetched)++] = self->nextThread++;
        }
        return *fetched == count ? S_OK : S_FALSE;
    }

    static void** Methods()
    {
        static std::array<void*, 8> methods = []
        {
            std::array<void*, 8> result{};
            result[2] = reinterpret_cast<void*>(&Release);
            result[7] = reinterpret_cast<void*>(&Next);
            return result;
        }();
        return methods.data();
    }
};

class CohortProfilerInfo
{
public:
    void**                 vtable = Methods();
    CohortThreadEnumerator threads;
    size_t                 enumerations = 0;

    ICorProfilerInfo7* GetInterface()
    {
        return reinterpret_cast<ICorProfilerInfo7*>(this);
    }

private:
    static HRESULT STDMETHODCALLTYPE GetCurrentThreadID(ICorProfilerInfo7*, ThreadID* id)
    {
        *id = 1;
        return S_OK;
    }

    static HRESULT STDMETHODCALLTYPE EnumThreads(ICorProfilerInfo7* instance, ICorProfilerThreadEnum** threads)
    {
        auto* self = reinterpret_cast<CohortProfilerInfo*>(instance);
        self->enumerations++;
        self->threads.nextThread = 1;
        *threads                 = reinterpret_cast<ICorProfilerThreadEnum*>(&self->threads);
        return S_OK;
    }

    static void** Methods()
    {
        static std::array<void*, 72> methods = []
        {
            std::array<void*, 72> result{};
            result[13] = reinterpret_cast<void*>(&GetCurrentThreadID);
            result[71] = reinterpret_cast<void*>(&EnumThreads);
            return result;
        }();
        return methods.data();
    }
};

class CohortStackWalker final : public IStackWalker
{
public:
    size_t                       captures = 0;
    std::unordered_set<ThreadID> lastThreads;
    std::function<void()>        onCapture;

    HRESULT CaptureStacks(const std::unordered_set<ThreadID>& threads, StackCaptureRequest* request) override
    {
        captures++;
        lastThreads = threads;
        if (onCapture)
        {
            onCapture();
        }
        for (const auto thread : threads)
        {
            CapturedFrame frame{};
            frame.threadId           = thread;
            frame.instructionPointer = thread;
            frame.isUnmanagedFrame   = true;
            const auto result        = request->onFrame(&frame);
            if (result != S_OK)
            {
                return result;
            }
        }
        return S_OK;
    }

    HRESULT ResolveNativeSymbolName(UINT_PTR, trace::WSTRING& name) override
    {
        name = WStr("CohortFrame");
        return S_OK;
    }

    void RequestShutdown() noexcept override {}
    void WaitForShutdown() noexcept override {}
};

uint64_t ReadInteger(const std::vector<unsigned char>& bytes, size_t& position, size_t width)
{
    uint64_t value = 0;
    while (width-- != 0)
    {
        value = (value << 8) | bytes.at(position++);
    }
    return value;
}

std::vector<uint64_t> ReadSpanIds(const std::vector<unsigned char>& bytes, const bool cpu)
{
    if (bytes.empty())
    {
        return {};
    }
    size_t position = 1;
    EXPECT_EQ(cpu ? 0x01 : 0x0A, bytes.at(0));
    EXPECT_EQ(1u, ReadInteger(bytes, position, 4));
    if (cpu)
    {
        ReadInteger(bytes, position, 8);
    }
    else
    {
        EXPECT_EQ(40u, ReadInteger(bytes, position, 4));
    }

    std::vector<uint64_t> spans;
    while (bytes.at(position) == (cpu ? 0x02 : 0x09))
    {
        position++;
        if (!cpu)
        {
            ReadInteger(bytes, position, 8);
        }
        const auto nameLength = ReadInteger(bytes, position, 2);
        position += static_cast<size_t>(nameLength) * 2; // Native strings contain UTF-16 code units on every platform.
        ReadInteger(bytes, position, 8);
        ReadInteger(bytes, position, 8);
        spans.push_back(ReadInteger(bytes, position, 8));
        while (const auto code = static_cast<int16_t>(ReadInteger(bytes, position, 2)))
        {
            if (code < 0)
            {
                const auto length = ReadInteger(bytes, position, 2);
                position += static_cast<size_t>(length) * 2;
            }
        }
    }
    EXPECT_EQ(cpu ? 0x06 : 0x0B, bytes.at(position));
    std::sort(spans.begin(), spans.end());
    return spans;
}

class SamplingCohortDeathTest : public ::testing::Test
{
protected:
    SamplingCohortDeathTest() : profiler_(sessions_)
    {
        profiler_.SetStackWalker(&walker_);
        profiler_.SetGlobalInfo7(info_.GetInterface());
    }

    void SetUp() override
    {
        // Admission closes permanently for the process. Run each scenario in a fresh child so shutdown tests
        // elsewhere in the suite cannot affect publication or context tracking, including under --gtest_shuffle.
        previousDeathTestStyle_                 = ::testing::FLAGS_gtest_death_test_style;
        ::testing::FLAGS_gtest_death_test_style = "threadsafe";
        Drain();
        RemoveSelectiveSamplingTrace({kTraceHigh, kTraceLow});
        ASSERT_TRUE(TryAddSelectiveSamplingTrace({kTraceHigh, kTraceLow}, std::chrono::steady_clock::now()));
        ContinuousProfilerSetNativeContext(kTraceHigh, kTraceLow, kSpanId);
    }

    void TearDown() override
    {
        profiler_.ThreadDestroyed(1);
        profiler_.ThreadDestroyed(2);
        profiler_.SetGlobalInfo7(nullptr);
        RemoveSelectiveSamplingTrace({kTraceHigh, kTraceLow});
        Drain();
        ::testing::FLAGS_gtest_death_test_style = previousDeathTestStyle_;
    }

    template <typename Action>
    static void RunInChild(Action action)
    {
        action();
        std::exit(::testing::Test::HasFailure() ? EXIT_FAILURE : EXIT_SUCCESS);
    }

    std::vector<unsigned char> Read(const bool cpu)
    {
        std::vector<unsigned char> bytes(200 * 1024);
        const auto                 count =
            cpu ? ContinuousProfilerReadThreadSamplesV2(static_cast<int32_t>(bytes.size()), bytes.data(), &cpuInterval_)
                                : SelectiveSamplerReadThreadSamples(static_cast<int32_t>(bytes.size()), bytes.data());
        bytes.resize(count);
        return bytes;
    }

    void Drain()
    {
        while (!Read(true).empty())
        {
        }
        Read(false);
    }

    void FillCpu()
    {
        for (size_t i = 0; i < 2; i++)
        {
            ThreadSamplingRecordProducedThreadSample(new std::vector<unsigned char>{0xEE}, 400);
        }
        ASSERT_FALSE(ThreadSamplingShouldProduceThreadSample());
    }

    void FillSelective()
    {
        std::vector<unsigned char> oversized(200 * 1024 + 1);
        SelectiveSamplingRecordProducedThreadSample(static_cast<int32_t>(oversized.size()), oversized.data());
        ASSERT_FALSE(SelectiveSamplingShouldProduceThreadSample());
    }

    void Capture(const SamplingType type = SamplingType::Continuous, const unsigned int cpuInterval = 400)
    {
        profiler_.StageThreadSamplingConfiguration(cpuInterval, 40);
        const auto configuration = profiler_.PullThreadSamplingConfiguration();
        stacks_.Reset();
        CaptureSamples(&profiler_, info_.GetInterface(), type, stacks_, configuration);
    }

    ClrAllocationSamplingSessionProvider sessions_{nullptr};
    CohortProfilerInfo                   info_;
    CohortStackWalker                    walker_;
    ContinuousProfiler                   profiler_;
    ThreadStacksBuffer                   stacks_;
    uint32_t                             cpuInterval_ = 0;
    std::string                          previousDeathTestStyle_;
};

} // namespace

TEST_F(SamplingCohortDeathTest, SharedCapturePublishesCpuCohortAndOnlyTheSelectedSubset)
{
    EXPECT_EXIT(RunInChild(
                    [this]
                    {
                        Capture();
                        const auto cpu       = Read(true);
                        const auto selective = Read(false);
                        ASSERT_FALSE(cpu.empty());
                        ASSERT_FALSE(selective.empty());
                        EXPECT_EQ((std::vector<uint64_t>{0, kSpanId}), ReadSpanIds(cpu, true));
                        EXPECT_EQ((std::vector<uint64_t>{kSpanId}), ReadSpanIds(selective, false));
                        EXPECT_EQ(400u, cpuInterval_);
                        EXPECT_EQ(1u, walker_.captures);
                        EXPECT_EQ((std::unordered_set<ThreadID>{1, 2}), walker_.lastThreads);
                        size_t cpuTimestamp       = 5;
                        size_t selectiveTimestamp = 10;
                        EXPECT_EQ(ReadInteger(cpu, cpuTimestamp, 8), ReadInteger(selective, selectiveTimestamp, 8));
                    }),
                ::testing::ExitedWithCode(EXIT_SUCCESS), "");
}

TEST_F(SamplingCohortDeathTest, CpuBackpressureStillCapturesAndPublishesSelectedThreads)
{
    EXPECT_EXIT(RunInChild(
                    [this]
                    {
                        FillCpu();
                        Capture();
                        EXPECT_EQ((std::vector<uint64_t>{kSpanId}), ReadSpanIds(Read(false), false));
                        EXPECT_EQ((std::unordered_set<ThreadID>{1}), walker_.lastThreads);
                        EXPECT_EQ(1u, walker_.captures);
                        EXPECT_EQ(0u, info_.enumerations);
                        EXPECT_EQ((std::vector<unsigned char>{0xEE}), Read(true));
                        EXPECT_EQ((std::vector<unsigned char>{0xEE}), Read(true));
                        EXPECT_TRUE(Read(true).empty());
                    }),
                ::testing::ExitedWithCode(EXIT_SUCCESS), "");
}

TEST_F(SamplingCohortDeathTest, SelectiveBackpressureStillPublishesTheCpuCohort)
{
    EXPECT_EXIT(RunInChild(
                    [this]
                    {
                        FillSelective();
                        Capture();
                        EXPECT_EQ((std::vector<uint64_t>{0, kSpanId}), ReadSpanIds(Read(true), true));
                        EXPECT_TRUE(Read(false).empty());
                        EXPECT_EQ(1u, walker_.captures);
                    }),
                ::testing::ExitedWithCode(EXIT_SUCCESS), "");
}

TEST_F(SamplingCohortDeathTest, BothBuffersFullSkipsTheCapture)
{
    EXPECT_EXIT(RunInChild(
                    [this]
                    {
                        FillCpu();
                        FillSelective();
                        Capture();
                        EXPECT_EQ(0u, walker_.captures);
                        EXPECT_EQ(0u, info_.enumerations);
                    }),
                ::testing::ExitedWithCode(EXIT_SUCCESS), "");
}

TEST_F(SamplingCohortDeathTest, CpuRuntimeTransitionsKeepEverySelectiveOutputOnTheSelectivePath)
{
    EXPECT_EXIT(RunInChild(
                    [this]
                    {
                        for (const auto cpuInterval : {0u, 400u, 0u})
                        {
                            Capture(cpuInterval == 0 ? SamplingType::SelectedThreads : SamplingType::Continuous,
                                    cpuInterval);
                            EXPECT_EQ((std::vector<uint64_t>{kSpanId}), ReadSpanIds(Read(false), false));
                            EXPECT_TRUE(Read(true).empty() == (cpuInterval == 0));
                        }
                        EXPECT_EQ(3u, walker_.captures);
                    }),
                ::testing::ExitedWithCode(EXIT_SUCCESS), "");
}

TEST_F(SamplingCohortDeathTest, ConfigurationChangeDuringCapturePreservesBothOutputIntervals)
{
    EXPECT_EXIT(RunInChild(
                    [this]
                    {
                        walker_.onCapture = [this] { profiler_.StageThreadSamplingConfiguration(800, 80); };
                        Capture();
                        const auto cpu       = Read(true);
                        const auto selective = Read(false);
                        ASSERT_FALSE(cpu.empty());
                        ASSERT_FALSE(selective.empty());
                        EXPECT_EQ(400u, cpuInterval_);
                        EXPECT_EQ((std::vector<uint64_t>{0, kSpanId}), ReadSpanIds(cpu, true));
                        size_t selectiveInterval = 5;
                        EXPECT_EQ(40u, ReadInteger(selective, selectiveInterval, 4));
                    }),
                ::testing::ExitedWithCode(EXIT_SUCCESS), "");
}

TEST_F(SamplingCohortDeathTest, ShutdownDuringCaptureDiscardsBothOutputs)
{
    EXPECT_EXIT(RunInChild(
                    [this]
                    {
                        walker_.onCapture = [this] { profiler_.Shutdown(); };
                        Capture();
                        EXPECT_TRUE(Read(true).empty());
                        EXPECT_TRUE(Read(false).empty());
                    }),
                ::testing::ExitedWithCode(EXIT_SUCCESS), "");
}
