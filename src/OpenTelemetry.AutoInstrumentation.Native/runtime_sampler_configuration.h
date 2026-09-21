/*
 * Copyright The OpenTelemetry Authors
 * SPDX-License-Identifier: Apache-2.0
 */

#ifndef OTEL_CLR_PROFILER_RUNTIME_SAMPLER_CONFIGURATION_H_
#define OTEL_CLR_PROFILER_RUNTIME_SAMPLER_CONFIGURATION_H_

#include <chrono>
#include <cstddef>
#include <cstdint>
#include <type_traits>

namespace continuous_profiler
{

enum class RuntimeSamplerAuthority : uint32_t
{
    None         = 0,
    Seed         = 1,
    ControlPlane = 2
};

enum class RuntimeSamplerApplyResult : int32_t
{
    // The controller snapshot is committed; asynchronous workers may still be converging to it.
    Applied                      = 0,
    NoChange                     = 1,
    IgnoredSeedAlreadyCommitted  = 2,
    IgnoredLowerAuthority        = 3,
    RejectedInvalidArgument      = 4,
    RejectedUnsupportedLayout    = 5,
    RejectedInvalidConfiguration = 6,
    RejectedUnsupportedRuntime   = 7,
    ActivationFailed             = 8,
    ShuttingDown                 = 9
};

enum class RuntimeSamplerStateQueryResult : int32_t
{
    Succeeded         = 0,
    InvalidArgument   = 1,
    UnsupportedLayout = 2
};

#pragma pack(push, 4)
struct RuntimeSamplerConfiguration
{
    uint32_t structureSize;
    uint32_t cpuSamplingIntervalMilliseconds;
    uint32_t selectiveThreadSamplingIntervalMilliseconds;
    uint32_t maxAllocationSamplesPerMinute;

    bool IsValid() const noexcept;

    std::chrono::milliseconds CpuSamplingInterval() const noexcept;
    std::chrono::milliseconds SelectiveThreadSamplingInterval() const noexcept;
    uint32_t                  MaxAllocationSamplesPerMinute() const noexcept;

    bool CpuEnabled() const noexcept;
    bool SelectiveEnabled() const noexcept;
    bool ThreadSamplingEnabled() const noexcept;
    bool AllocationEnabled() const noexcept;
    bool AnyFeatureEnabled() const noexcept;

    bool operator==(const RuntimeSamplerConfiguration& other) const noexcept;
    bool operator!=(const RuntimeSamplerConfiguration& other) const noexcept;
};

struct RuntimeSamplerState
{
    // Committed controller state. Asynchronous workers may still be converging to this
    // configuration, and disabled infrastructure may remain allocated or await cleanup.
    uint32_t                      structureSize;
    uint32_t                      authority;
    RuntimeSamplerConfiguration committedConfiguration;
};
#pragma pack(pop)

struct RuntimeSamplerControllerState
{
    RuntimeSamplerAuthority       authority{RuntimeSamplerAuthority::None};
    RuntimeSamplerConfiguration configuration{sizeof(RuntimeSamplerConfiguration), 0, 0, 0};
};

RuntimeSamplerStateQueryResult EncodeRuntimeSamplerState(const RuntimeSamplerControllerState& state,
                                                           RuntimeSamplerState*     encoded) noexcept;

static_assert(std::is_standard_layout_v<RuntimeSamplerConfiguration>);
static_assert(std::is_trivially_copyable_v<RuntimeSamplerConfiguration>);
static_assert(sizeof(RuntimeSamplerConfiguration) == 16);
static_assert(offsetof(RuntimeSamplerConfiguration, structureSize) == 0);
static_assert(offsetof(RuntimeSamplerConfiguration, cpuSamplingIntervalMilliseconds) == 4);
static_assert(offsetof(RuntimeSamplerConfiguration, selectiveThreadSamplingIntervalMilliseconds) == 8);
static_assert(offsetof(RuntimeSamplerConfiguration, maxAllocationSamplesPerMinute) == 12);

static_assert(std::is_standard_layout_v<RuntimeSamplerState>);
static_assert(std::is_trivially_copyable_v<RuntimeSamplerState>);
static_assert(sizeof(RuntimeSamplerState) == 24);
static_assert(offsetof(RuntimeSamplerState, structureSize) == 0);
static_assert(offsetof(RuntimeSamplerState, authority) == 4);
static_assert(offsetof(RuntimeSamplerState, committedConfiguration) == 8);

} // namespace continuous_profiler

#endif // OTEL_CLR_PROFILER_RUNTIME_SAMPLER_CONFIGURATION_H_
