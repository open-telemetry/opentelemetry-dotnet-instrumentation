// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#include "runtime_sampler_controller.h"

namespace continuous_profiler
{

RuntimeSamplerController::RuntimeSamplerController(IRuntimeSamplerLifecycle& lifecycle) noexcept : lifecycle_(lifecycle)
{
}

RuntimeSamplerController::~RuntimeSamplerController()
{
    Shutdown();
}

bool RuntimeSamplerController::Prepare() noexcept
{
    std::lock_guard<std::mutex> guard(mutex_);
    return !shutdown_requested_ && EnsureBootstrappedLocked();
}

RuntimeSamplerApplyResult RuntimeSamplerController::ApplyConfiguration(
    const RuntimeSamplerConfiguration& configuration) noexcept
{
    std::lock_guard<std::mutex> guard(mutex_);
    return ApplyConfigurationLocked(configuration, false);
}

RuntimeSamplerApplyResult RuntimeSamplerController::ApplyInitialConfiguration(
    const RuntimeSamplerConfiguration& configuration) noexcept
{
    std::lock_guard<std::mutex> guard(mutex_);
    return ApplyConfigurationLocked(configuration, true);
}

RuntimeSamplerControllerState RuntimeSamplerController::GetState() const noexcept
{
    std::lock_guard<std::mutex> guard(mutex_);
    return {active_configuration_, generation_};
}

void RuntimeSamplerController::Shutdown() noexcept
{
    std::lock_guard<std::mutex> guard(mutex_);
    if (shutdown_requested_)
    {
        return;
    }

    shutdown_requested_ = true;
    if (bootstrap_attempted_)
    {
        lifecycle_.ShutdownSampling();
    }
}

RuntimeSamplerApplyResult RuntimeSamplerController::ApplyConfigurationLocked(
    const RuntimeSamplerConfiguration& configuration, const bool establishInitialConfiguration) noexcept
{
    if (shutdown_requested_)
    {
        return RuntimeSamplerApplyResult::ShuttingDown;
    }

    if (establishInitialConfiguration && configuration_established_)
    {
        return RuntimeSamplerApplyResult::IgnoredInitialConfiguration;
    }

    // A first, explicitly supplied all-disabled initial configuration is an
    // established state even though it equals the controller's zero state.
    const bool establishesExplicitAllDisabledConfiguration =
        !configuration_established_ && !configuration.HasThreadSampling() && !configuration.HasAllocationSampling();

    if (!establishesExplicitAllDisabledConfiguration && configuration == active_configuration_)
    {
        return RuntimeSamplerApplyResult::NoChange;
    }

    if (configuration.HasAllocationSampling() && !lifecycle_.IsAllocationSamplingSupported())
    {
        return RuntimeSamplerApplyResult::RejectedUnsupportedRuntime;
    }

    const bool requiresBootstrap = configuration.HasThreadSampling() || configuration.HasAllocationSampling();
    if (requiresBootstrap && !EnsureBootstrappedLocked())
    {
        return RuntimeSamplerApplyResult::BootstrapFailed;
    }

    // Once bootstrap has succeeded, even an all-disabled candidate must reach
    // the lifecycle so that already-created producers are quiesced.
    if (bootstrap_succeeded_ && !lifecycle_.ApplyConfiguration(configuration))
    {
        return RuntimeSamplerApplyResult::ActivationFailed;
    }

    active_configuration_      = configuration;
    configuration_established_ = true;
    generation_++;
    return RuntimeSamplerApplyResult::Applied;
}

bool RuntimeSamplerController::EnsureBootstrappedLocked() noexcept
{
    if (!bootstrap_attempted_)
    {
        bootstrap_attempted_ = true;
        bootstrap_succeeded_ = lifecycle_.Bootstrap();
    }

    return bootstrap_succeeded_;
}

} // namespace continuous_profiler
