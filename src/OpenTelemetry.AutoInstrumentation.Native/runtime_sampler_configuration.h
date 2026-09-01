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
struct RuntimeSamplerConfigurationV1
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

    bool operator==(const RuntimeSamplerConfigurationV1& other) const noexcept;
    bool operator!=(const RuntimeSamplerConfigurationV1& other) const noexcept;
};

struct RuntimeSamplerStateV1
{
    uint32_t                      structureSize;
    uint32_t                      authority;
    RuntimeSamplerConfigurationV1 committedConfiguration;
};
#pragma pack(pop)

struct RuntimeSamplerState
{
    RuntimeSamplerAuthority       authority{RuntimeSamplerAuthority::None};
    RuntimeSamplerConfigurationV1 configuration{sizeof(RuntimeSamplerConfigurationV1), 0, 0, 0};
};

RuntimeSamplerStateQueryResult EncodeRuntimeSamplerStateV1(const RuntimeSamplerState& state,
                                                           RuntimeSamplerStateV1*     encoded) noexcept;

static_assert(std::is_standard_layout_v<RuntimeSamplerConfigurationV1>);
static_assert(std::is_trivially_copyable_v<RuntimeSamplerConfigurationV1>);
static_assert(sizeof(RuntimeSamplerConfigurationV1) == 16);
static_assert(offsetof(RuntimeSamplerConfigurationV1, structureSize) == 0);
static_assert(offsetof(RuntimeSamplerConfigurationV1, cpuSamplingIntervalMilliseconds) == 4);
static_assert(offsetof(RuntimeSamplerConfigurationV1, selectiveThreadSamplingIntervalMilliseconds) == 8);
static_assert(offsetof(RuntimeSamplerConfigurationV1, maxAllocationSamplesPerMinute) == 12);

static_assert(std::is_standard_layout_v<RuntimeSamplerStateV1>);
static_assert(std::is_trivially_copyable_v<RuntimeSamplerStateV1>);
static_assert(sizeof(RuntimeSamplerStateV1) == 24);
static_assert(offsetof(RuntimeSamplerStateV1, structureSize) == 0);
static_assert(offsetof(RuntimeSamplerStateV1, authority) == 4);
static_assert(offsetof(RuntimeSamplerStateV1, committedConfiguration) == 8);

} // namespace continuous_profiler

#endif // OTEL_CLR_PROFILER_RUNTIME_SAMPLER_CONFIGURATION_H_
