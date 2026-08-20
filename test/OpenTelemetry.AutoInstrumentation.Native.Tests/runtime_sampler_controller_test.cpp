// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#include "pch.h"
#include "../../src/OpenTelemetry.AutoInstrumentation.Native/runtime_sampler_controller.h"

#include <cstdint>
#include <vector>

namespace
{

using continuous_profiler::IRuntimeSamplerLifecycle;
using continuous_profiler::RuntimeSamplerApplyResult;
using continuous_profiler::RuntimeSamplerConfiguration;
using continuous_profiler::RuntimeSamplerController;

RuntimeSamplerConfiguration AllDisabled()
{
    return {};
}

RuntimeSamplerConfiguration PeriodicSampling(const std::uint32_t interval)
{
    return {interval, std::nullopt, std::nullopt};
}

RuntimeSamplerConfiguration AllSamplingModes()
{
    return {1000u, 100u, 200u};
}

class FakeRuntimeSamplerLifecycle final : public IRuntimeSamplerLifecycle
{
public:
    bool        allocationSamplingSupported = true;
    bool        bootstrapSucceeds           = true;
    bool        activationSucceeds          = true;
    mutable int allocationSupportQueryCount = 0;
    int         bootstrapCount              = 0;
    int         applyCount                  = 0;
    int         shutdownCount               = 0;

    std::vector<RuntimeSamplerConfiguration> appliedConfigurations;

    bool IsAllocationSamplingSupported() const noexcept override
    {
        allocationSupportQueryCount++;
        return allocationSamplingSupported;
    }

    bool Bootstrap() noexcept override
    {
        bootstrapCount++;
        return bootstrapSucceeds;
    }

    bool ApplyConfiguration(const RuntimeSamplerConfiguration& configuration) noexcept override
    {
        applyCount++;
        appliedConfigurations.push_back(configuration);
        return activationSucceeds;
    }

    void ShutdownSampling() noexcept override
    {
        shutdownCount++;
    }
};

void AssertState(const RuntimeSamplerController&    controller,
                 const std::uint64_t                expectedGeneration,
                 const RuntimeSamplerConfiguration& expectedConfiguration)
{
    const auto state = controller.GetState();
    ASSERT_EQ(expectedGeneration, state.generation);
    ASSERT_TRUE(state.configuration == expectedConfiguration);
}

} // namespace

TEST(RuntimeSamplerControllerTest, AllDisabledInitialConfigurationCommitsWithoutBootstrap)
{
    FakeRuntimeSamplerLifecycle lifecycle;
    RuntimeSamplerController    controller(lifecycle);

    AssertState(controller, 0u, AllDisabled());

    ASSERT_EQ(RuntimeSamplerApplyResult::Applied, controller.ApplyInitialConfiguration(AllDisabled()));
    AssertState(controller, 1u, AllDisabled());
    ASSERT_EQ(0, lifecycle.bootstrapCount);
    ASSERT_EQ(0, lifecycle.applyCount);
}

TEST(RuntimeSamplerControllerTest, PrepareBootstrapsFoundationWithoutApplyingAConfiguration)
{
    FakeRuntimeSamplerLifecycle lifecycle;
    RuntimeSamplerController    controller(lifecycle);

    ASSERT_TRUE(controller.Prepare());
    ASSERT_TRUE(controller.Prepare());

    AssertState(controller, 0u, AllDisabled());
    ASSERT_EQ(1, lifecycle.bootstrapCount);
    ASSERT_EQ(0, lifecycle.applyCount);

    controller.Shutdown();
    ASSERT_EQ(1, lifecycle.shutdownCount);
}

TEST(RuntimeSamplerControllerTest, FailedPrepareIsStickyAndRemainsFailClosed)
{
    FakeRuntimeSamplerLifecycle lifecycle;
    lifecycle.bootstrapSucceeds = false;
    RuntimeSamplerController controller(lifecycle);

    ASSERT_FALSE(controller.Prepare());
    lifecycle.bootstrapSucceeds = true;
    ASSERT_FALSE(controller.Prepare());
    ASSERT_EQ(RuntimeSamplerApplyResult::BootstrapFailed, controller.ApplyConfiguration(PeriodicSampling(1000u)));

    AssertState(controller, 0u, AllDisabled());
    ASSERT_EQ(1, lifecycle.bootstrapCount);
    ASSERT_EQ(0, lifecycle.applyCount);

    controller.Shutdown();
    ASSERT_EQ(1, lifecycle.shutdownCount);
}

TEST(RuntimeSamplerControllerTest, ExplicitAllDisabledConfigurationPreventsALaterLegacySeed)
{
    FakeRuntimeSamplerLifecycle lifecycle;
    RuntimeSamplerController    controller(lifecycle);

    ASSERT_EQ(RuntimeSamplerApplyResult::Applied, controller.ApplyConfiguration(AllDisabled()));
    ASSERT_EQ(RuntimeSamplerApplyResult::IgnoredInitialConfiguration,
              controller.ApplyInitialConfiguration(PeriodicSampling(1000u)));

    AssertState(controller, 1u, AllDisabled());
    ASSERT_EQ(0, lifecycle.bootstrapCount);
    ASSERT_EQ(0, lifecycle.applyCount);
}

TEST(RuntimeSamplerControllerTest, BootstrapIsLazyAndRunsOnlyOnce)
{
    FakeRuntimeSamplerLifecycle lifecycle;
    RuntimeSamplerController    controller(lifecycle);

    ASSERT_EQ(RuntimeSamplerApplyResult::Applied, controller.ApplyInitialConfiguration(AllDisabled()));
    ASSERT_EQ(RuntimeSamplerApplyResult::Applied, controller.ApplyConfiguration(PeriodicSampling(1000u)));
    ASSERT_EQ(RuntimeSamplerApplyResult::Applied, controller.ApplyConfiguration(PeriodicSampling(2000u)));

    ASSERT_EQ(1, lifecycle.bootstrapCount);
    ASSERT_EQ(2, lifecycle.applyCount);
    ASSERT_EQ(2u, lifecycle.appliedConfigurations.size());
    ASSERT_TRUE(lifecycle.appliedConfigurations[0] == PeriodicSampling(1000u));
    ASSERT_TRUE(lifecycle.appliedConfigurations[1] == PeriodicSampling(2000u));
    AssertState(controller, 3u, PeriodicSampling(2000u));
}

TEST(RuntimeSamplerControllerTest, FailedBootstrapIsStickyAndFailsClosed)
{
    FakeRuntimeSamplerLifecycle lifecycle;
    lifecycle.bootstrapSucceeds = false;
    RuntimeSamplerController controller(lifecycle);

    ASSERT_EQ(RuntimeSamplerApplyResult::Applied, controller.ApplyInitialConfiguration(AllDisabled()));
    ASSERT_EQ(RuntimeSamplerApplyResult::BootstrapFailed, controller.ApplyConfiguration(PeriodicSampling(1000u)));

    lifecycle.bootstrapSucceeds = true;
    ASSERT_EQ(RuntimeSamplerApplyResult::BootstrapFailed, controller.ApplyConfiguration(PeriodicSampling(1000u)));

    ASSERT_EQ(1, lifecycle.bootstrapCount);
    ASSERT_EQ(0, lifecycle.applyCount);
    AssertState(controller, 1u, AllDisabled());
}

TEST(RuntimeSamplerControllerTest, ChangedAndUnchangedCandidatesHaveDeterministicGenerations)
{
    FakeRuntimeSamplerLifecycle lifecycle;
    RuntimeSamplerController    controller(lifecycle);

    ASSERT_EQ(RuntimeSamplerApplyResult::Applied, controller.ApplyInitialConfiguration(AllDisabled()));
    ASSERT_EQ(RuntimeSamplerApplyResult::NoChange, controller.ApplyConfiguration(AllDisabled()));
    AssertState(controller, 1u, AllDisabled());

    ASSERT_EQ(RuntimeSamplerApplyResult::Applied, controller.ApplyConfiguration(PeriodicSampling(1000u)));
    AssertState(controller, 2u, PeriodicSampling(1000u));

    ASSERT_EQ(RuntimeSamplerApplyResult::NoChange, controller.ApplyConfiguration(PeriodicSampling(1000u)));
    AssertState(controller, 2u, PeriodicSampling(1000u));

    ASSERT_EQ(RuntimeSamplerApplyResult::Applied, controller.ApplyConfiguration(PeriodicSampling(2000u)));
    AssertState(controller, 3u, PeriodicSampling(2000u));

    ASSERT_EQ(1, lifecycle.bootstrapCount);
    ASSERT_EQ(2, lifecycle.applyCount);
}

TEST(RuntimeSamplerControllerTest, ActivationFailurePreservesLastKnownGoodConfiguration)
{
    FakeRuntimeSamplerLifecycle lifecycle;
    RuntimeSamplerController    controller(lifecycle);

    ASSERT_EQ(RuntimeSamplerApplyResult::Applied, controller.ApplyInitialConfiguration(PeriodicSampling(1000u)));
    AssertState(controller, 1u, PeriodicSampling(1000u));

    lifecycle.activationSucceeds = false;
    ASSERT_EQ(RuntimeSamplerApplyResult::ActivationFailed, controller.ApplyConfiguration(PeriodicSampling(2000u)));
    AssertState(controller, 1u, PeriodicSampling(1000u));

    lifecycle.activationSucceeds = true;
    ASSERT_EQ(RuntimeSamplerApplyResult::Applied, controller.ApplyConfiguration(PeriodicSampling(2000u)));
    AssertState(controller, 2u, PeriodicSampling(2000u));

    ASSERT_EQ(1, lifecycle.bootstrapCount);
    ASSERT_EQ(3, lifecycle.applyCount);
}

TEST(RuntimeSamplerControllerTest, FirstSuccessfulInitialConfigurationWins)
{
    FakeRuntimeSamplerLifecycle lifecycle;
    RuntimeSamplerController    controller(lifecycle);

    ASSERT_EQ(RuntimeSamplerApplyResult::Applied, controller.ApplyInitialConfiguration(PeriodicSampling(1000u)));
    ASSERT_EQ(RuntimeSamplerApplyResult::IgnoredInitialConfiguration,
              controller.ApplyInitialConfiguration(PeriodicSampling(2000u)));

    AssertState(controller, 1u, PeriodicSampling(1000u));
    ASSERT_EQ(1, lifecycle.bootstrapCount);
    ASSERT_EQ(1, lifecycle.applyCount);
}

TEST(RuntimeSamplerControllerTest, FailedInitialActivationDoesNotConsumeTheInitialSlot)
{
    FakeRuntimeSamplerLifecycle lifecycle;
    lifecycle.activationSucceeds = false;
    RuntimeSamplerController controller(lifecycle);

    ASSERT_EQ(RuntimeSamplerApplyResult::ActivationFailed,
              controller.ApplyInitialConfiguration(PeriodicSampling(1000u)));
    AssertState(controller, 0u, AllDisabled());

    lifecycle.activationSucceeds = true;
    ASSERT_EQ(RuntimeSamplerApplyResult::Applied, controller.ApplyInitialConfiguration(PeriodicSampling(2000u)));
    AssertState(controller, 1u, PeriodicSampling(2000u));

    ASSERT_EQ(1, lifecycle.bootstrapCount);
    ASSERT_EQ(2, lifecycle.applyCount);
}

TEST(RuntimeSamplerControllerTest, AllDisabledInitialAfterBootstrapIsEstablishedOnlyWhenLifecycleAcceptsIt)
{
    FakeRuntimeSamplerLifecycle lifecycle;
    lifecycle.activationSucceeds = false;
    RuntimeSamplerController controller(lifecycle);

    ASSERT_EQ(RuntimeSamplerApplyResult::ActivationFailed,
              controller.ApplyInitialConfiguration(PeriodicSampling(1000u)));
    ASSERT_EQ(RuntimeSamplerApplyResult::ActivationFailed, controller.ApplyInitialConfiguration(AllDisabled()));
    AssertState(controller, 0u, AllDisabled());

    lifecycle.activationSucceeds = true;
    ASSERT_EQ(RuntimeSamplerApplyResult::Applied, controller.ApplyInitialConfiguration(AllDisabled()));

    AssertState(controller, 1u, AllDisabled());
    ASSERT_EQ(1, lifecycle.bootstrapCount);
    ASSERT_EQ(3, lifecycle.applyCount);
    ASSERT_TRUE(lifecycle.appliedConfigurations.back() == AllDisabled());
}

TEST(RuntimeSamplerControllerTest, UnsupportedAllocationIsRejectedBeforeBootstrap)
{
    FakeRuntimeSamplerLifecycle lifecycle;
    lifecycle.allocationSamplingSupported = false;
    RuntimeSamplerController controller(lifecycle);

    ASSERT_EQ(RuntimeSamplerApplyResult::RejectedUnsupportedRuntime, controller.ApplyConfiguration(AllSamplingModes()));

    AssertState(controller, 0u, AllDisabled());
    ASSERT_EQ(0, lifecycle.bootstrapCount);
    ASSERT_EQ(0, lifecycle.applyCount);
}

TEST(RuntimeSamplerControllerTest, DisablingAfterBootstrapIsForwardedToTheLifecycle)
{
    FakeRuntimeSamplerLifecycle lifecycle;
    RuntimeSamplerController    controller(lifecycle);

    ASSERT_EQ(RuntimeSamplerApplyResult::Applied, controller.ApplyInitialConfiguration(PeriodicSampling(1000u)));
    ASSERT_EQ(RuntimeSamplerApplyResult::Applied, controller.ApplyConfiguration(AllDisabled()));

    ASSERT_EQ(1, lifecycle.bootstrapCount);
    ASSERT_EQ(2, lifecycle.applyCount);
    ASSERT_TRUE(lifecycle.appliedConfigurations.back() == AllDisabled());
    AssertState(controller, 2u, AllDisabled());
}

TEST(RuntimeSamplerControllerTest, ShutdownIsTerminalAndIdempotent)
{
    FakeRuntimeSamplerLifecycle lifecycle;
    RuntimeSamplerController    controller(lifecycle);

    ASSERT_EQ(RuntimeSamplerApplyResult::Applied, controller.ApplyInitialConfiguration(PeriodicSampling(1000u)));
    controller.Shutdown();
    controller.Shutdown();

    ASSERT_EQ(1, lifecycle.shutdownCount);
    ASSERT_EQ(RuntimeSamplerApplyResult::ShuttingDown, controller.ApplyConfiguration(AllDisabled()));
    ASSERT_EQ(RuntimeSamplerApplyResult::ShuttingDown, controller.ApplyInitialConfiguration(PeriodicSampling(2000u)));
    AssertState(controller, 1u, PeriodicSampling(1000u));
    ASSERT_EQ(1, lifecycle.bootstrapCount);
    ASSERT_EQ(1, lifecycle.applyCount);
    ASSERT_EQ(1, lifecycle.shutdownCount);
}

TEST(RuntimeSamplerControllerTest, ShutdownBeforeBootstrapHasNoProducerLifecycleSideEffect)
{
    FakeRuntimeSamplerLifecycle lifecycle;
    RuntimeSamplerController    controller(lifecycle);

    controller.Shutdown();
    controller.Shutdown();

    ASSERT_EQ(0, lifecycle.bootstrapCount);
    ASSERT_EQ(0, lifecycle.applyCount);
    ASSERT_EQ(0, lifecycle.shutdownCount);
    ASSERT_EQ(RuntimeSamplerApplyResult::ShuttingDown, controller.ApplyInitialConfiguration(PeriodicSampling(1000u)));
    AssertState(controller, 0u, AllDisabled());
}
