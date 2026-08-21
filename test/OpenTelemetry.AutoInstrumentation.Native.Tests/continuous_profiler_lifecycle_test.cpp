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

RuntimeSamplerConfiguration PeriodicAndAllocation(const std::uint32_t samplesPerMinute)
{
    return {kPeriodicIntervalMilliseconds, std::nullopt, samplesPerMinute};
}

class ConfigurationApplier
{
public:
    explicit ConfigurationApplier(ContinuousProfiler& profiler) : profiler_(profiler) {}

    bool Apply(const RuntimeSamplerConfiguration& candidate)
    {
        if (!profiler_.ApplyConfiguration(current_, candidate))
        {
            return false;
        }

        current_ = candidate;
        return true;
    }

private:
    ContinuousProfiler&         profiler_;
    RuntimeSamplerConfiguration current_;
};

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
    ContinuousProfiler   profiler;
    FakeStackWalker      stackWalker;
    ConfigurationApplier applier(profiler);
    profiler.SetStackWalker(&stackWalker);

    ASSERT_FALSE(profiler.HasThreadSamplingWorker());
    ASSERT_TRUE(applier.Apply(AllDisabled()));
    ASSERT_FALSE(profiler.HasThreadSamplingWorker());
    ASSERT_EQ(0, stackWalker.prepareCalls);
}

TEST(ContinuousProfilerLifecycleTest, ThreadModesShareOneWorkerThatQuiescesAfterTheFinalDisable)
{
    ContinuousProfiler   profiler;
    FakeStackWalker      stackWalker;
    ConfigurationApplier applier(profiler);
    profiler.SetStackWalker(&stackWalker);

    ASSERT_TRUE(applier.Apply(PeriodicOnly()));
    ASSERT_TRUE(profiler.HasThreadSamplingWorker());
    ASSERT_FALSE(profiler.IsThreadSamplingWorkerQuiescent());
    ASSERT_EQ(1, stackWalker.prepareCalls);

    ASSERT_TRUE(applier.Apply(BothThreadModes()));
    ASSERT_TRUE(profiler.HasThreadSamplingWorker());

    ASSERT_TRUE(applier.Apply(SelectiveOnly()));
    ASSERT_TRUE(profiler.HasThreadSamplingWorker());

    ASSERT_TRUE(applier.Apply(AllDisabled()));
    ASSERT_TRUE(profiler.HasThreadSamplingWorker());
    ASSERT_TRUE(profiler.IsThreadSamplingWorkerQuiescent());

    ASSERT_TRUE(applier.Apply(PeriodicOnly()));
    ASSERT_TRUE(profiler.HasThreadSamplingWorker());
    ASSERT_FALSE(profiler.IsThreadSamplingWorkerQuiescent());
    ASSERT_EQ(1, stackWalker.prepareCalls);

    ASSERT_TRUE(applier.Apply(AllDisabled()));
    ASSERT_TRUE(profiler.HasThreadSamplingWorker());
    ASSERT_TRUE(profiler.IsThreadSamplingWorkerQuiescent());
}

TEST(ContinuousProfilerLifecycleTest, UnsupportedAllocationRejectsCombinedConfigurationBeforeWorkerCreation)
{
    ContinuousProfiler   profiler;
    FakeStackWalker      stackWalker;
    ConfigurationApplier applier(profiler);
    profiler.SetStackWalker(&stackWalker);
    const RuntimeSamplerConfiguration unsupportedCandidate{kPeriodicIntervalMilliseconds, std::nullopt, 200u};

    ASSERT_FALSE(applier.Apply(unsupportedCandidate));

    ASSERT_FALSE(profiler.HasThreadSamplingWorker());
    ASSERT_FALSE(profiler.allocationSubSampler);
}

TEST(ContinuousProfilerLifecycleTest, FailedAllocationActivationLeavesTheWorkerQuiescentAndReusable)
{
    FakeAllocationSamplingSessionProvider sessions;
    sessions.startResults = {{E_FAIL, 0}};
    ContinuousProfiler   profiler(&sessions);
    FakeStackWalker      stackWalker;
    ConfigurationApplier applier(profiler);
    profiler.SetStackWalker(&stackWalker);

    ASSERT_FALSE(applier.Apply(PeriodicAndAllocation(100)));

    ASSERT_TRUE(profiler.HasThreadSamplingWorker());
    ASSERT_TRUE(profiler.IsThreadSamplingWorkerQuiescent());
    ASSERT_EQ(1, stackWalker.prepareCalls);

    ASSERT_TRUE(applier.Apply(PeriodicOnly()));
    ASSERT_TRUE(profiler.HasThreadSamplingWorker());
    ASSERT_EQ(1, stackWalker.prepareCalls);
}

TEST(ContinuousProfilerLifecycleTest, StackWalkingPreparationFailureRejectsTheThreadTransition)
{
    ContinuousProfiler   profiler;
    FakeStackWalker      stackWalker;
    ConfigurationApplier applier(profiler);
    stackWalker.prepareResult = E_FAIL;
    profiler.SetStackWalker(&stackWalker);

    ASSERT_FALSE(applier.Apply(PeriodicOnly()));

    ASSERT_EQ(1, stackWalker.prepareCalls);
    ASSERT_FALSE(profiler.HasThreadSamplingWorker());
}

TEST(ContinuousProfilerLifecycleTest, ShutdownIsTerminalAndIdempotent)
{
    ContinuousProfiler   profiler;
    FakeStackWalker      stackWalker;
    ConfigurationApplier applier(profiler);
    profiler.SetStackWalker(&stackWalker);
    ASSERT_TRUE(applier.Apply(BothThreadModes()));
    ASSERT_TRUE(profiler.HasThreadSamplingWorker());

    profiler.Shutdown();
    profiler.Shutdown();

    ASSERT_TRUE(profiler.IsShutdownRequested());
    ASSERT_FALSE(profiler.HasThreadSamplingWorker());
    ASSERT_FALSE(applier.Apply(PeriodicOnly()));
    ASSERT_FALSE(profiler.HasThreadSamplingWorker());
}

TEST(ContinuousProfilerAllocationLifecycleTest, EnableAndRateUpdateReuseOneEventPipeSession)
{
    FakeAllocationSamplingSessionProvider sessions;
    sessions.startResults = {{S_OK, 101}};
    sessions.stopResults  = {S_OK};
    ContinuousProfiler   profiler(&sessions);
    ConfigurationApplier applier(profiler);

    ASSERT_TRUE(applier.Apply(AllocationOnly(100)));
    ASSERT_EQ(1u, sessions.startCalls);
    ASSERT_TRUE(sessions.stoppedSessions.empty());
    ASSERT_TRUE(profiler.allocationSubSampler);

    ASSERT_TRUE(applier.Apply(AllocationOnly(200)));
    ASSERT_EQ(1u, sessions.startCalls);
    ASSERT_TRUE(sessions.stoppedSessions.empty());
    ASSERT_TRUE(profiler.allocationSubSampler);

    ASSERT_TRUE(applier.Apply(AllDisabled()));
    ASSERT_EQ((std::vector<EVENTPIPE_SESSION>{101}), sessions.stoppedSessions);
    ASSERT_FALSE(profiler.allocationSubSampler);
}

TEST(ContinuousProfilerAllocationLifecycleTest, FailedDisableRollsBackAndCanBeRetried)
{
    FakeAllocationSamplingSessionProvider sessions;
    sessions.startResults = {{S_OK, 202}};
    sessions.stopResults  = {E_FAIL, S_OK};
    ContinuousProfiler   profiler(&sessions);
    ConfigurationApplier applier(profiler);

    ASSERT_TRUE(applier.Apply(AllocationOnly(100)));
    ASSERT_FALSE(applier.Apply(AllDisabled()));
    ASSERT_EQ((std::vector<EVENTPIPE_SESSION>{202}), sessions.stoppedSessions);
    ASSERT_TRUE(profiler.allocationSubSampler);

    ASSERT_TRUE(applier.Apply(AllDisabled()));
    ASSERT_EQ((std::vector<EVENTPIPE_SESSION>{202, 202}), sessions.stoppedSessions);
    ASSERT_FALSE(profiler.allocationSubSampler);
}

TEST(ContinuousProfilerAllocationLifecycleTest, FailedStartIsCleanedUpBeforeReenable)
{
    FakeAllocationSamplingSessionProvider sessions;
    sessions.startResults = {{E_FAIL, 303}, {S_OK, 404}};
    sessions.stopResults  = {E_FAIL, S_OK, S_OK};
    ContinuousProfiler   profiler(&sessions);
    ConfigurationApplier applier(profiler);

    ASSERT_FALSE(applier.Apply(AllocationOnly(100)));
    ASSERT_EQ(1u, sessions.startCalls);
    ASSERT_EQ((std::vector<EVENTPIPE_SESSION>{303}), sessions.stoppedSessions);
    ASSERT_FALSE(profiler.allocationSubSampler);

    ASSERT_TRUE(applier.Apply(AllocationOnly(100)));
    ASSERT_EQ(2u, sessions.startCalls);
    ASSERT_EQ((std::vector<EVENTPIPE_SESSION>{303, 303}), sessions.stoppedSessions);
    ASSERT_TRUE(profiler.allocationSubSampler);

    profiler.Shutdown();
    ASSERT_EQ((std::vector<EVENTPIPE_SESSION>{303, 303, 404}), sessions.stoppedSessions);
}

TEST(ContinuousProfilerAllocationLifecycleTest, SuccessfulStartWithoutASessionIsRejected)
{
    FakeAllocationSamplingSessionProvider sessions;
    sessions.startResults = {{S_OK, 0}};
    ContinuousProfiler   profiler(&sessions);
    ConfigurationApplier applier(profiler);

    ASSERT_FALSE(applier.Apply(AllocationOnly(100)));
    ASSERT_EQ(1u, sessions.startCalls);
    ASSERT_TRUE(sessions.stoppedSessions.empty());
    ASSERT_FALSE(profiler.allocationSubSampler);
}

TEST(ContinuousProfilerAllocationLifecycleTest, TerminalShutdownStopsTheSessionOnce)
{
    FakeAllocationSamplingSessionProvider sessions;
    sessions.startResults = {{S_OK, 505}};
    sessions.stopResults  = {S_OK};

    {
        ContinuousProfiler   profiler(&sessions);
        ConfigurationApplier applier(profiler);
        ASSERT_TRUE(applier.Apply(AllocationOnly(100)));

        profiler.Shutdown();
        profiler.Shutdown();

        ASSERT_TRUE(profiler.IsShutdownRequested());
        ASSERT_FALSE(applier.Apply(AllocationOnly(200)));
    }

    ASSERT_EQ((std::vector<EVENTPIPE_SESSION>{505}), sessions.stoppedSessions);
}

TEST(ContinuousProfilerAllocationLifecycleTest, ConcurrentShutdownStopsTheSessionCreatedByAnInFlightApply)
{
    BlockingAllocationSamplingSessionProvider sessions;
    ContinuousProfiler                        profiler(&sessions);
    ConfigurationApplier                      applier(profiler);
    bool                                      applySucceeded = false;
    std::thread                               applyThread([&] { applySucceeded = applier.Apply(AllocationOnly(100)); });
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
}
