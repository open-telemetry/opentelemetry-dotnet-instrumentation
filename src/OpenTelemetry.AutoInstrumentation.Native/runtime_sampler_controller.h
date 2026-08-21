// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#ifndef OTEL_RUNTIME_SAMPLER_CONTROLLER_H_
#define OTEL_RUNTIME_SAMPLER_CONTROLLER_H_

#include "runtime_sampler_configuration.h"

#include <cstdint>
#include <mutex>

namespace continuous_profiler
{

// Separates configuration ownership from the CLR-specific producer lifecycle.
// Implementations must either apply the complete configuration or return false
// while retaining their previous effective producer state.
class IRuntimeSamplerLifecycle
{
public:
    virtual ~IRuntimeSamplerLifecycle() = default;

    virtual bool IsAllocationSamplingSupported() const noexcept                                = 0;
    virtual bool Bootstrap() noexcept                                                          = 0;
    virtual bool ApplyConfiguration(const RuntimeSamplerConfiguration& previousConfiguration,
                                    const RuntimeSamplerConfiguration& configuration) noexcept = 0;
    virtual void ShutdownSampling() noexcept                                                   = 0;
};

struct RuntimeSamplerControllerState
{
    RuntimeSamplerConfiguration configuration;
    std::uint64_t               generation;
};

// Owns the authoritative runtime sampler configuration. All transitions are
// serialized, and state is published only after the lifecycle accepts the
// complete candidate.
class RuntimeSamplerController
{
public:
    explicit RuntimeSamplerController(IRuntimeSamplerLifecycle& lifecycle) noexcept;
    ~RuntimeSamplerController();

    RuntimeSamplerController(const RuntimeSamplerController&)            = delete;
    RuntimeSamplerController& operator=(const RuntimeSamplerController&) = delete;

    RuntimeSamplerApplyResult     ApplyConfiguration(const RuntimeSamplerConfiguration& configuration) noexcept;
    RuntimeSamplerApplyResult     ApplyInitialConfiguration(const RuntimeSamplerConfiguration& configuration) noexcept;
    RuntimeSamplerControllerState GetState() const noexcept;
    void                          Shutdown() noexcept;

private:
    RuntimeSamplerApplyResult ApplyConfigurationLocked(const RuntimeSamplerConfiguration& configuration,
                                                       bool establishInitialConfiguration) noexcept;
    bool                      EnsureBootstrappedLocked() noexcept;

    IRuntimeSamplerLifecycle&   lifecycle_;
    mutable std::mutex          mutex_;
    RuntimeSamplerConfiguration active_configuration_;
    std::uint64_t               generation_                = 0;
    bool                        configuration_established_ = false;
    bool                        bootstrap_attempted_       = false;
    bool                        bootstrap_succeeded_       = false;
    bool                        shutdown_requested_        = false;
};

} // namespace continuous_profiler

#endif // OTEL_RUNTIME_SAMPLER_CONTROLLER_H_
