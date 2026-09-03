#include "pch.h"

#include "../../src/OpenTelemetry.AutoInstrumentation.Native/continuous_profiler.h"
#include "../../src/OpenTelemetry.AutoInstrumentation.Native/runtime_sampler_configuration.h"
#include "../../src/OpenTelemetry.AutoInstrumentation.Native/runtime_sampler_service.h"

#include <future>
#include <type_traits>
#include <vector>

using namespace continuous_profiler;

namespace
{

RuntimeSamplerConfigurationV1 Configuration(const uint32_t cpu, const uint32_t selective, const uint32_t allocation)
{
    return {sizeof(RuntimeSamplerConfigurationV1), cpu, selective, allocation};
}

RuntimeSamplerApplyResult Apply(RuntimeSamplerService&               service,
                                const RuntimeSamplerAuthority        authority,
                                const RuntimeSamplerConfigurationV1& configuration)
{
    return service.ApplyConfigurationV1(authority, configuration).result;
}

class FakeAllocationSamplingSessionProvider final : public IAllocationSamplingSessionProvider
{
public:
    std::vector<HRESULT>           startResults;
    std::vector<EVENTPIPE_SESSION> sessions;
    std::vector<HRESULT>           stopResults;
    std::vector<EVENTPIPE_SESSION> stoppedSessions;
    size_t                         startCalls = 0;
    std::function<void()>          onStop;

    HRESULT StartAllocationSamplingSession(EVENTPIPE_SESSION* session) noexcept override
    {
        if (session == nullptr || startCalls >= startResults.size())
        {
            return E_UNEXPECTED;
        }

        *session = sessions[startCalls];
        return startResults[startCalls++];
    }

    HRESULT StopAllocationSamplingSession(const EVENTPIPE_SESSION session) noexcept override
    {
        if (onStop)
        {
            onStop();
        }

        const auto call = stoppedSessions.size();
        stoppedSessions.push_back(session);
        return call < stopResults.size() ? stopResults[call] : E_UNEXPECTED;
    }
};

static_assert(!std::is_default_constructible_v<ContinuousProfiler>);
static_assert(std::is_constructible_v<ContinuousProfiler, IAllocationSamplingSessionProvider&>);

} // namespace

TEST(RuntimeSamplerServiceTest, ClrAllocationSessionProviderRejectsMissingProfilerInfo)
{
    ClrAllocationSamplingSessionProvider sessions(nullptr);
    EVENTPIPE_SESSION                    session = 42;

    EXPECT_EQ(E_NOINTERFACE, sessions.StartAllocationSamplingSession(&session));
    EXPECT_EQ(0u, session);
    EXPECT_EQ(E_NOINTERFACE, sessions.StopAllocationSamplingSession(42));
}

TEST(RuntimeSamplerServiceTest, DisabledConfigurationDoesNotRequireClrCapabilities)
{
    RuntimeSamplerService service(nullptr, nullptr, RuntimeType::Unknown);

    EXPECT_EQ(RuntimeSamplerApplyResult::Applied,
              Apply(service, RuntimeSamplerAuthority::ControlPlane, Configuration(0, 0, 0)));
    EXPECT_FALSE(service.GetState().configuration.AnyFeatureEnabled());
    EXPECT_FALSE(service.HasSamplingInfrastructure());
}

TEST(RuntimeSamplerServiceTest, InvalidConfigurationIsRejectedBeforeAuthorityCommit)
{
    RuntimeSamplerService service(nullptr, nullptr, RuntimeType::Unknown);

    EXPECT_EQ(RuntimeSamplerApplyResult::RejectedInvalidConfiguration,
              Apply(service, RuntimeSamplerAuthority::ControlPlane, Configuration(1000, 30, 0)));
    EXPECT_EQ(RuntimeSamplerAuthority::None, service.GetState().authority);
    EXPECT_FALSE(service.HasSamplingInfrastructure());
}

TEST(RuntimeSamplerServiceTest, FirstSuccessfulSeedWins)
{
    RuntimeSamplerService service(nullptr, nullptr, RuntimeType::Unknown);

    ASSERT_EQ(RuntimeSamplerApplyResult::Applied,
              Apply(service, RuntimeSamplerAuthority::Seed, Configuration(0, 0, 0)));
    EXPECT_EQ(RuntimeSamplerApplyResult::IgnoredSeedAlreadyCommitted,
              Apply(service, RuntimeSamplerAuthority::Seed, Configuration(1000, 0, 0)));

    const auto state = service.GetState();
    EXPECT_EQ(RuntimeSamplerAuthority::Seed, state.authority);
    EXPECT_EQ(0u, state.configuration.CpuSamplingInterval().count());
    EXPECT_FALSE(service.HasSamplingInfrastructure());
}

TEST(RuntimeSamplerServiceTest, ExactlyOneConcurrentSeedCommits)
{
    RuntimeSamplerService                               service(nullptr, nullptr, RuntimeType::Unknown);
    const auto                                          disabled = Configuration(0, 0, 0);
    std::promise<void>                                  start;
    auto                                                startSignal = start.get_future().share();
    std::vector<std::future<RuntimeSamplerApplyResult>> attempts;

    for (size_t i = 0; i < 8; i++)
    {
        attempts.emplace_back(std::async(std::launch::async,
                                         [&service, disabled, startSignal]
                                         {
                                             startSignal.wait();
                                             return Apply(service, RuntimeSamplerAuthority::Seed, disabled);
                                         }));
    }

    start.set_value();
    size_t applied = 0;
    for (auto& attempt : attempts)
    {
        const auto result = attempt.get();
        applied += result == RuntimeSamplerApplyResult::Applied ? 1 : 0;
        EXPECT_TRUE(result == RuntimeSamplerApplyResult::Applied ||
                    result == RuntimeSamplerApplyResult::IgnoredSeedAlreadyCommitted);
    }

    EXPECT_EQ(1u, applied);
    EXPECT_EQ(RuntimeSamplerAuthority::Seed, service.GetState().authority);
}

TEST(RuntimeSamplerServiceTest, ControlPlaneCanCommitBeforeSeed)
{
    RuntimeSamplerService service(nullptr, nullptr, RuntimeType::Unknown);

    ASSERT_EQ(RuntimeSamplerApplyResult::Applied,
              Apply(service, RuntimeSamplerAuthority::ControlPlane, Configuration(0, 0, 0)));
    EXPECT_EQ(RuntimeSamplerApplyResult::IgnoredLowerAuthority,
              Apply(service, RuntimeSamplerAuthority::Seed, Configuration(1000, 0, 0)));

    EXPECT_EQ(RuntimeSamplerAuthority::ControlPlane, service.GetState().authority);
    EXPECT_FALSE(service.HasSamplingInfrastructure());
}

TEST(RuntimeSamplerServiceTest, ControlPlanePromotesAnIdenticalSeedConfiguration)
{
    RuntimeSamplerService service(nullptr, nullptr, RuntimeType::Unknown);
    const auto            disabled = Configuration(0, 0, 0);

    ASSERT_EQ(RuntimeSamplerApplyResult::Applied, Apply(service, RuntimeSamplerAuthority::Seed, disabled));
    EXPECT_EQ(RuntimeSamplerApplyResult::Applied, Apply(service, RuntimeSamplerAuthority::ControlPlane, disabled));
    EXPECT_EQ(RuntimeSamplerApplyResult::NoChange, Apply(service, RuntimeSamplerAuthority::ControlPlane, disabled));

    EXPECT_EQ(RuntimeSamplerAuthority::ControlPlane, service.GetState().authority);
}

TEST(RuntimeSamplerServiceTest, InvalidControlPlaneConfigurationPreservesTheLastKnownGoodState)
{
    RuntimeSamplerService service(nullptr, nullptr, RuntimeType::Unknown);
    const auto            disabled = Configuration(0, 0, 0);

    ASSERT_EQ(RuntimeSamplerApplyResult::Applied, Apply(service, RuntimeSamplerAuthority::ControlPlane, disabled));
    EXPECT_EQ(RuntimeSamplerApplyResult::RejectedInvalidConfiguration,
              Apply(service, RuntimeSamplerAuthority::ControlPlane, Configuration(1000, 30, 0)));

    const auto state = service.GetState();
    EXPECT_EQ(RuntimeSamplerAuthority::ControlPlane, state.authority);
    EXPECT_EQ(disabled, state.configuration);
}

TEST(RuntimeSamplerServiceTest, ActivationFailureDoesNotConsumeAuthority)
{
    RuntimeSamplerService service(nullptr, nullptr, RuntimeType::Unknown);

    EXPECT_EQ(RuntimeSamplerApplyResult::ActivationFailed,
              Apply(service, RuntimeSamplerAuthority::Seed, Configuration(1000, 0, 0)));
    EXPECT_EQ(RuntimeSamplerAuthority::None, service.GetState().authority);
    EXPECT_FALSE(service.HasSamplingInfrastructure());

    EXPECT_EQ(RuntimeSamplerApplyResult::Applied,
              Apply(service, RuntimeSamplerAuthority::Seed, Configuration(0, 0, 0)));
    EXPECT_EQ(RuntimeSamplerAuthority::Seed, service.GetState().authority);
}

TEST(RuntimeSamplerServiceTest, SamplerPullsTheMostRecentlyStagedThreadConfiguration)
{
    FakeAllocationSamplingSessionProvider sessions;
    ContinuousProfiler                    profiler(sessions);

    profiler.StageThreadSamplingConfiguration(1000, 0);
    profiler.StageThreadSamplingConfiguration(200, 20);

    const auto configuration = profiler.PullThreadSamplingConfiguration();

    EXPECT_EQ(200u, configuration.cpuSamplingIntervalMilliseconds);
    EXPECT_EQ(20u, configuration.selectiveSamplingIntervalMilliseconds);
}

TEST(RuntimeSamplerServiceTest, AllocationSamplerObservesAZeroTargetPublishedAtRuntime)
{
    AllocationSubSampler sampler(1000, 60);

    sampler.SetTargetPerCycle(0);

    EXPECT_FALSE(sampler.ShouldSample());
}

TEST(RuntimeSamplerServiceTest, AllocationSessionStartsWithAdmissionClosedAndRateChangesReuseIt)
{
    FakeAllocationSamplingSessionProvider sessions;
    sessions.startResults = {S_OK};
    sessions.sessions     = {101};
    sessions.stopResults  = {S_OK};
    ContinuousProfiler profiler(sessions);

    ASSERT_TRUE(profiler.StartAllocationSamplingSession());
    ASSERT_NE(nullptr, profiler.allocationSubSampler);
    auto* const sampler = profiler.allocationSubSampler.get();
    EXPECT_EQ(0u, sampler->TargetPerCycle());
    EXPECT_EQ(1u, sessions.startCalls);

    profiler.UpdateAllocationSamplingTarget(100);
    EXPECT_EQ(100u, sampler->TargetPerCycle());
    ASSERT_TRUE(profiler.StartAllocationSamplingSession());
    EXPECT_EQ(sampler, profiler.allocationSubSampler.get());
    EXPECT_EQ(1u, sessions.startCalls);

    profiler.UpdateAllocationSamplingTarget(200);
    EXPECT_EQ(200u, sampler->TargetPerCycle());
}

TEST(RuntimeSamplerServiceTest, FailedAllocationStartKeepsAdmissionClosedAndCanBeRetried)
{
    FakeAllocationSamplingSessionProvider sessions;
    sessions.startResults = {E_FAIL, S_OK};
    sessions.sessions     = {0, 202};
    sessions.stopResults  = {S_OK};
    ContinuousProfiler profiler(sessions);

    EXPECT_FALSE(profiler.StartAllocationSamplingSession());
    ASSERT_NE(nullptr, profiler.allocationSubSampler);
    auto* const sampler = profiler.allocationSubSampler.get();
    EXPECT_EQ(0u, sampler->TargetPerCycle());

    EXPECT_TRUE(profiler.StartAllocationSamplingSession());
    EXPECT_EQ(sampler, profiler.allocationSubSampler.get());
    EXPECT_EQ(2u, sessions.startCalls);
    EXPECT_EQ(0u, sampler->TargetPerCycle());
}

TEST(RuntimeSamplerServiceTest, FailedAllocationStopRetainsSessionForRetryAndAdmissionRemainsClosed)
{
    FakeAllocationSamplingSessionProvider sessions;
    sessions.startResults = {S_OK};
    sessions.sessions     = {303};
    sessions.stopResults  = {E_FAIL, S_OK};
    ContinuousProfiler profiler(sessions);

    ASSERT_TRUE(profiler.StartAllocationSamplingSession());
    profiler.UpdateAllocationSamplingTarget(100);

    profiler.UpdateAllocationSamplingTarget(0);
    EXPECT_FALSE(profiler.StopAllocationSamplingSession());
    EXPECT_EQ(0u, profiler.allocationSubSampler->TargetPerCycle());
    EXPECT_EQ((std::vector<EVENTPIPE_SESSION>{303}), sessions.stoppedSessions);

    EXPECT_TRUE(profiler.StopAllocationSamplingSession());
    EXPECT_EQ((std::vector<EVENTPIPE_SESSION>{303, 303}), sessions.stoppedSessions);
}

TEST(RuntimeSamplerServiceTest, ShutdownClosesAllocationAdmissionBeforeSynchronouslyStoppingEventPipe)
{
    FakeAllocationSamplingSessionProvider sessions;
    sessions.startResults = {S_OK};
    sessions.sessions     = {404};
    sessions.stopResults  = {S_OK};
    ContinuousProfiler profiler(sessions);

    ASSERT_TRUE(profiler.StartAllocationSamplingSession());
    profiler.UpdateAllocationSamplingTarget(100);

    bool stopObserved = false;
    sessions.onStop   = [&]
    {
        stopObserved = true;
        EXPECT_TRUE(profiler.IsShutdownRequested());
        ASSERT_NE(nullptr, profiler.allocationSubSampler);
        EXPECT_EQ(0u, profiler.allocationSubSampler->TargetPerCycle());
    };

    profiler.Shutdown();

    EXPECT_TRUE(stopObserved);
    EXPECT_EQ((std::vector<EVENTPIPE_SESSION>{404}), sessions.stoppedSessions);
}

TEST(RuntimeSamplerServiceTest, RepeatedShutdownDoesNotRetryClrCallsAfterTerminalCallback)
{
    FakeAllocationSamplingSessionProvider sessions;
    sessions.startResults = {S_OK};
    sessions.sessions     = {404};
    sessions.stopResults  = {E_FAIL, S_OK};
    ContinuousProfiler profiler(sessions);

    ASSERT_TRUE(profiler.StartAllocationSamplingSession());
    profiler.UpdateAllocationSamplingTarget(100);

    profiler.Shutdown();
    EXPECT_EQ(0u, profiler.allocationSubSampler->TargetPerCycle());
    EXPECT_EQ((std::vector<EVENTPIPE_SESSION>{404}), sessions.stoppedSessions);

    profiler.Shutdown();
    EXPECT_EQ((std::vector<EVENTPIPE_SESSION>{404}), sessions.stoppedSessions);
}

TEST(RuntimeSamplerServiceTest, ShutdownIsTerminalAndIdempotent)
{
    RuntimeSamplerService service(nullptr, nullptr, RuntimeType::Unknown);
    const auto            disabled = Configuration(0, 0, 0);

    ASSERT_EQ(RuntimeSamplerApplyResult::Applied, Apply(service, RuntimeSamplerAuthority::ControlPlane, disabled));

    service.Shutdown();
    service.Shutdown();

    EXPECT_EQ(RuntimeSamplerApplyResult::ShuttingDown, Apply(service, RuntimeSamplerAuthority::ControlPlane, disabled));
    EXPECT_EQ(RuntimeSamplerAuthority::ControlPlane, service.GetState().authority);
    EXPECT_FALSE(service.HasSamplingInfrastructure());
}

TEST(RuntimeSamplerServiceTest, CallbacksAreIgnoredAfterShutdown)
{
    RuntimeSamplerService service(nullptr, nullptr, RuntimeType::Unknown);

    service.Shutdown();

    service.OnThreadCreated(1);
    service.OnThreadDestroyed(1);
    service.OnThreadNameChanged(1, 0, nullptr);
    service.OnThreadAssignedToOSThread(1, 2);
    service.OnAllocationTick(0, nullptr);

    EXPECT_EQ(RuntimeSamplerApplyResult::ShuttingDown,
              Apply(service, RuntimeSamplerAuthority::Seed, Configuration(0, 0, 0)));
}

TEST(RuntimeSamplerServiceTest, ApplyReturnsTheStateFromTheSameControllerTransaction)
{
    RuntimeSamplerService service(nullptr, nullptr, RuntimeType::Unknown);
    const auto            disabled = Configuration(0, 0, 0);

    const auto seedOutcome = service.ApplyConfigurationV1(RuntimeSamplerAuthority::Seed, disabled);
    ASSERT_EQ(RuntimeSamplerApplyResult::Applied, seedOutcome.result);
    EXPECT_EQ(RuntimeSamplerAuthority::Seed, seedOutcome.state.authority);
    EXPECT_EQ(disabled, seedOutcome.state.configuration);

    const auto promotionOutcome = service.ApplyConfigurationV1(RuntimeSamplerAuthority::ControlPlane, disabled);
    ASSERT_EQ(RuntimeSamplerApplyResult::Applied, promotionOutcome.result);
    EXPECT_EQ(RuntimeSamplerAuthority::ControlPlane, promotionOutcome.state.authority);
    EXPECT_EQ(disabled, promotionOutcome.state.configuration);
}
