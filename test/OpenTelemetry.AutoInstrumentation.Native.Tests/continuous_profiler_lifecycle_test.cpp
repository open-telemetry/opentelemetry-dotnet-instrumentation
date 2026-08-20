// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#include "pch.h"
#include "../../src/OpenTelemetry.AutoInstrumentation.Native/continuous_profiler.h"

#include <atomic>
#include <condition_variable>
#include <cstdint>
#include <mutex>
#include <thread>
#include <vector>

namespace
{

using continuous_profiler::ContinuousProfiler;
using continuous_profiler::IAllocationSamplingSessionProvider;
using continuous_profiler::IStackWalker;
using continuous_profiler::RuntimeSamplerConfiguration;
using continuous_profiler::StackCaptureRequest;

constexpr std::uint32_t kPeriodicIntervalMilliseconds  = 3'600'000u;
constexpr std::uint32_t kSelectiveIntervalMilliseconds = 1'800'000u;

RuntimeSamplerConfiguration AllDisabled()
{
    return {};
}

RuntimeSamplerConfiguration PeriodicOnly()
{
    return {kPeriodicIntervalMilliseconds, std::nullopt, std::nullopt};
}

RuntimeSamplerConfiguration SelectiveOnly()
{
    return {std::nullopt, kSelectiveIntervalMilliseconds, std::nullopt};
}

RuntimeSamplerConfiguration BothThreadModes()
{
    return {kPeriodicIntervalMilliseconds, kSelectiveIntervalMilliseconds, std::nullopt};
}

RuntimeSamplerConfiguration AllocationOnly(const std::uint32_t samplesPerMinute)
{
    return {std::nullopt, std::nullopt, samplesPerMinute};
}

void AssertConfiguration(const ContinuousProfiler& profiler, const RuntimeSamplerConfiguration& expected)
{
    ASSERT_TRUE(profiler.GetConfiguration() == expected);
}

class FakeStackWalker final : public IStackWalker
{
public:
    HRESULT prepareResult = S_OK;
    int     prepareCalls  = 0;

    HRESULT PrepareForStackWalking() noexcept override
    {
        prepareCalls++;
        return prepareResult;
    }

    HRESULT CaptureStacks(const std::unordered_set<ThreadID>&, StackCaptureRequest*) override
    {
        return S_OK;
    }

    HRESULT ResolveNativeSymbolName(UINT_PTR, trace::WSTRING&) override
    {
        return E_NOTIMPL;
    }
};

struct StartSessionResult
{
    HRESULT           result;
    EVENTPIPE_SESSION session;
};

class FakeAllocationSamplingSessionProvider final : public IAllocationSamplingSessionProvider
{
public:
    std::vector<StartSessionResult> startResults;
    std::vector<HRESULT>            stopResults;
    std::vector<EVENTPIPE_SESSION>  stoppedSessions;
    std::size_t                     startCalls = 0;

    HRESULT StartAllocationSamplingSession(EVENTPIPE_SESSION* session) noexcept override
    {
        if (session == nullptr || startCalls >= startResults.size())
        {
            return E_UNEXPECTED;
        }

        const auto result = startResults[startCalls++];
        *session          = result.session;
        return result.result;
    }

    HRESULT StopAllocationSamplingSession(const EVENTPIPE_SESSION session) noexcept override
    {
        const auto stopCall = stoppedSessions.size();
        stoppedSessions.push_back(session);
        return stopCall < stopResults.size() ? stopResults[stopCall] : E_UNEXPECTED;
    }
};

class BlockingAllocationSamplingSessionProvider final : public IAllocationSamplingSessionProvider
{
public:
    HRESULT StartAllocationSamplingSession(EVENTPIPE_SESSION* session) noexcept override
    {
        std::unique_lock<std::mutex> lock(mutex_);
        startEntered_ = true;
        stateChanged_.notify_all();
        stateChanged_.wait(lock, [this] { return releaseStart_; });
        *session = 606;
        return S_OK;
    }

    HRESULT StopAllocationSamplingSession(const EVENTPIPE_SESSION session) noexcept override
    {
        stoppedSession_.store(session);
        stopCalls_.fetch_add(1);
        return S_OK;
    }

    void WaitUntilStartIsEntered()
    {
        std::unique_lock<std::mutex> lock(mutex_);
        stateChanged_.wait(lock, [this] { return startEntered_; });
    }

    void ReleaseStart()
    {
        {
            std::lock_guard<std::mutex> lock(mutex_);
            releaseStart_ = true;
        }
        stateChanged_.notify_all();
    }

    std::size_t StopCalls() const noexcept
    {
        return stopCalls_.load();
    }

    EVENTPIPE_SESSION StoppedSession() const noexcept
    {
        return stoppedSession_.load();
    }

private:
    std::mutex                     mutex_;
    std::condition_variable        stateChanged_;
    bool                           startEntered_ = false;
    bool                           releaseStart_ = false;
    std::atomic<std::size_t>       stopCalls_{0};
    std::atomic<EVENTPIPE_SESSION> stoppedSession_{0};
};

} // namespace

TEST(ContinuousProfilerLifecycleTest, AllDisabledConfigurationDoesNotCreateAWorker)
{
    ContinuousProfiler profiler;
    FakeStackWalker    stackWalker;
    profiler.SetStackWalker(&stackWalker);

    AssertConfiguration(profiler, AllDisabled());
    ASSERT_FALSE(profiler.IsThreadSamplingThreadRunning());
    ASSERT_TRUE(profiler.ApplyConfiguration(AllDisabled()));
    ASSERT_FALSE(profiler.IsThreadSamplingThreadRunning());
    AssertConfiguration(profiler, AllDisabled());
    ASSERT_EQ(0, stackWalker.prepareCalls);
}

TEST(ContinuousProfilerLifecycleTest, ThreadModesShareOneWorkerAndStopOnlyAfterTheFinalDisable)
{
    ContinuousProfiler profiler;
    FakeStackWalker    stackWalker;
    profiler.SetStackWalker(&stackWalker);

    ASSERT_TRUE(profiler.ApplyConfiguration(PeriodicOnly()));
    ASSERT_TRUE(profiler.IsThreadSamplingThreadRunning());
    AssertConfiguration(profiler, PeriodicOnly());
    ASSERT_EQ(1, stackWalker.prepareCalls);

    ASSERT_TRUE(profiler.ApplyConfiguration(BothThreadModes()));
    ASSERT_TRUE(profiler.IsThreadSamplingThreadRunning());
    AssertConfiguration(profiler, BothThreadModes());

    ASSERT_TRUE(profiler.ApplyConfiguration(SelectiveOnly()));
    ASSERT_TRUE(profiler.IsThreadSamplingThreadRunning());
    AssertConfiguration(profiler, SelectiveOnly());

    ASSERT_TRUE(profiler.ApplyConfiguration(AllDisabled()));
    ASSERT_FALSE(profiler.IsThreadSamplingThreadRunning());
    AssertConfiguration(profiler, AllDisabled());

    ASSERT_TRUE(profiler.ApplyConfiguration(PeriodicOnly()));
    ASSERT_TRUE(profiler.IsThreadSamplingThreadRunning());
    AssertConfiguration(profiler, PeriodicOnly());
    ASSERT_EQ(2, stackWalker.prepareCalls);

    ASSERT_TRUE(profiler.ApplyConfiguration(AllDisabled()));
    ASSERT_FALSE(profiler.IsThreadSamplingThreadRunning());
}

TEST(ContinuousProfilerLifecycleTest, UnsupportedAllocationRollsBackACombinedThreadTransition)
{
    ContinuousProfiler profiler;
    FakeStackWalker    stackWalker;
    profiler.SetStackWalker(&stackWalker);
    const RuntimeSamplerConfiguration unsupportedCandidate{kPeriodicIntervalMilliseconds, std::nullopt, 200u};

    ASSERT_FALSE(profiler.ApplyConfiguration(unsupportedCandidate));

    ASSERT_FALSE(profiler.IsThreadSamplingThreadRunning());
    AssertConfiguration(profiler, AllDisabled());
    ASSERT_FALSE(profiler.allocationSubSampler);
}

TEST(ContinuousProfilerLifecycleTest, StackWalkingPreparationFailureRejectsTheThreadTransition)
{
    ContinuousProfiler profiler;
    FakeStackWalker    stackWalker;
    stackWalker.prepareResult = E_FAIL;
    profiler.SetStackWalker(&stackWalker);

    ASSERT_FALSE(profiler.ApplyConfiguration(PeriodicOnly()));

    ASSERT_EQ(1, stackWalker.prepareCalls);
    ASSERT_FALSE(profiler.IsThreadSamplingThreadRunning());
    AssertConfiguration(profiler, AllDisabled());
}

TEST(ContinuousProfilerLifecycleTest, ShutdownIsTerminalAndIdempotent)
{
    ContinuousProfiler profiler;
    FakeStackWalker    stackWalker;
    profiler.SetStackWalker(&stackWalker);
    ASSERT_TRUE(profiler.ApplyConfiguration(BothThreadModes()));
    ASSERT_TRUE(profiler.IsThreadSamplingThreadRunning());

    profiler.Shutdown();
    profiler.Shutdown();

    ASSERT_TRUE(profiler.IsShutdownRequested());
    ASSERT_FALSE(profiler.IsThreadSamplingThreadRunning());
    AssertConfiguration(profiler, AllDisabled());
    ASSERT_FALSE(profiler.ApplyConfiguration(PeriodicOnly()));
    ASSERT_FALSE(profiler.IsThreadSamplingThreadRunning());
    AssertConfiguration(profiler, AllDisabled());
}

TEST(ContinuousProfilerAllocationLifecycleTest, EnableAndRateUpdateReuseOneEventPipeSession)
{
    FakeAllocationSamplingSessionProvider sessions;
    sessions.startResults = {{S_OK, 101}};
    sessions.stopResults  = {S_OK};
    ContinuousProfiler profiler(&sessions);

    ASSERT_TRUE(profiler.ApplyConfiguration(AllocationOnly(100)));
    ASSERT_EQ(1u, sessions.startCalls);
    ASSERT_TRUE(sessions.stoppedSessions.empty());
    ASSERT_TRUE(profiler.allocationSubSampler);
    AssertConfiguration(profiler, AllocationOnly(100));

    ASSERT_TRUE(profiler.ApplyConfiguration(AllocationOnly(200)));
    ASSERT_EQ(1u, sessions.startCalls);
    ASSERT_TRUE(sessions.stoppedSessions.empty());
    ASSERT_TRUE(profiler.allocationSubSampler);
    AssertConfiguration(profiler, AllocationOnly(200));

    ASSERT_TRUE(profiler.ApplyConfiguration(AllDisabled()));
    ASSERT_EQ((std::vector<EVENTPIPE_SESSION>{101}), sessions.stoppedSessions);
    ASSERT_FALSE(profiler.allocationSubSampler);
    AssertConfiguration(profiler, AllDisabled());
}

TEST(ContinuousProfilerAllocationLifecycleTest, FailedDisableRollsBackAndCanBeRetried)
{
    FakeAllocationSamplingSessionProvider sessions;
    sessions.startResults = {{S_OK, 202}};
    sessions.stopResults  = {E_FAIL, S_OK};
    ContinuousProfiler profiler(&sessions);

    ASSERT_TRUE(profiler.ApplyConfiguration(AllocationOnly(100)));
    ASSERT_FALSE(profiler.ApplyConfiguration(AllDisabled()));
    ASSERT_EQ((std::vector<EVENTPIPE_SESSION>{202}), sessions.stoppedSessions);
    ASSERT_TRUE(profiler.allocationSubSampler);
    AssertConfiguration(profiler, AllocationOnly(100));

    ASSERT_TRUE(profiler.ApplyConfiguration(AllDisabled()));
    ASSERT_EQ((std::vector<EVENTPIPE_SESSION>{202, 202}), sessions.stoppedSessions);
    ASSERT_FALSE(profiler.allocationSubSampler);
    AssertConfiguration(profiler, AllDisabled());
}

TEST(ContinuousProfilerAllocationLifecycleTest, FailedStartIsCleanedUpBeforeReenable)
{
    FakeAllocationSamplingSessionProvider sessions;
    sessions.startResults = {{E_FAIL, 303}, {S_OK, 404}};
    sessions.stopResults  = {E_FAIL, S_OK, S_OK};
    ContinuousProfiler profiler(&sessions);

    ASSERT_FALSE(profiler.ApplyConfiguration(AllocationOnly(100)));
    ASSERT_EQ(1u, sessions.startCalls);
    ASSERT_EQ((std::vector<EVENTPIPE_SESSION>{303}), sessions.stoppedSessions);
    ASSERT_FALSE(profiler.allocationSubSampler);
    AssertConfiguration(profiler, AllDisabled());

    ASSERT_TRUE(profiler.ApplyConfiguration(AllocationOnly(100)));
    ASSERT_EQ(2u, sessions.startCalls);
    ASSERT_EQ((std::vector<EVENTPIPE_SESSION>{303, 303}), sessions.stoppedSessions);
    ASSERT_TRUE(profiler.allocationSubSampler);
    AssertConfiguration(profiler, AllocationOnly(100));

    profiler.Shutdown();
    ASSERT_EQ((std::vector<EVENTPIPE_SESSION>{303, 303, 404}), sessions.stoppedSessions);
}

TEST(ContinuousProfilerAllocationLifecycleTest, SuccessfulStartWithoutASessionIsRejected)
{
    FakeAllocationSamplingSessionProvider sessions;
    sessions.startResults = {{S_OK, 0}};
    ContinuousProfiler profiler(&sessions);

    ASSERT_FALSE(profiler.ApplyConfiguration(AllocationOnly(100)));
    ASSERT_EQ(1u, sessions.startCalls);
    ASSERT_TRUE(sessions.stoppedSessions.empty());
    ASSERT_FALSE(profiler.allocationSubSampler);
    AssertConfiguration(profiler, AllDisabled());
}

TEST(ContinuousProfilerAllocationLifecycleTest, TerminalShutdownStopsTheSessionOnce)
{
    FakeAllocationSamplingSessionProvider sessions;
    sessions.startResults = {{S_OK, 505}};
    sessions.stopResults  = {S_OK};

    {
        ContinuousProfiler profiler(&sessions);
        ASSERT_TRUE(profiler.ApplyConfiguration(AllocationOnly(100)));

        profiler.Shutdown();
        profiler.Shutdown();

        ASSERT_TRUE(profiler.IsShutdownRequested());
        ASSERT_FALSE(profiler.ApplyConfiguration(AllocationOnly(200)));
        AssertConfiguration(profiler, AllDisabled());
    }

    ASSERT_EQ((std::vector<EVENTPIPE_SESSION>{505}), sessions.stoppedSessions);
}

TEST(ContinuousProfilerAllocationLifecycleTest, ConcurrentShutdownStopsTheSessionCreatedByAnInFlightApply)
{
    BlockingAllocationSamplingSessionProvider sessions;
    ContinuousProfiler                        profiler(&sessions);
    bool                                      applySucceeded = false;
    std::thread applyThread([&] { applySucceeded = profiler.ApplyConfiguration(AllocationOnly(100)); });
    sessions.WaitUntilStartIsEntered();

    std::mutex              shutdownStateMutex;
    std::condition_variable shutdownStateChanged;
    bool                    shutdownCallIssued = false;
    bool                    shutdownReturned   = false;
    std::thread             shutdownThread(
        [&]
        {
            {
                std::lock_guard<std::mutex> lock(shutdownStateMutex);
                shutdownCallIssued = true;
            }
            shutdownStateChanged.notify_all();

            profiler.Shutdown();

            {
                std::lock_guard<std::mutex> lock(shutdownStateMutex);
                shutdownReturned = true;
            }
            shutdownStateChanged.notify_all();
        });

    {
        std::unique_lock<std::mutex> lock(shutdownStateMutex);
        shutdownStateChanged.wait(lock, [&] { return shutdownCallIssued; });
        EXPECT_FALSE(shutdownReturned);
    }
    EXPECT_FALSE(profiler.IsShutdownRequested());

    sessions.ReleaseStart();
    applyThread.join();
    shutdownThread.join();

    ASSERT_TRUE(applySucceeded);
    ASSERT_TRUE(shutdownReturned);
    ASSERT_TRUE(profiler.IsShutdownRequested());
    ASSERT_EQ(1u, sessions.StopCalls());
    ASSERT_EQ(606u, sessions.StoppedSession());
    AssertConfiguration(profiler, AllDisabled());
}
