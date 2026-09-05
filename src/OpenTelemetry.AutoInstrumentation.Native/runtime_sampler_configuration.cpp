/*
 * Copyright The OpenTelemetry Authors
 * SPDX-License-Identifier: Apache-2.0
 */

#include "runtime_sampler_configuration.h"

namespace continuous_profiler
{

bool RuntimeSamplerConfigurationV1::IsValid() const noexcept
{
    if (cpuSamplingIntervalMilliseconds != 0 && selectiveThreadSamplingIntervalMilliseconds != 0 &&
        (cpuSamplingIntervalMilliseconds <= selectiveThreadSamplingIntervalMilliseconds ||
         cpuSamplingIntervalMilliseconds % selectiveThreadSamplingIntervalMilliseconds != 0))
    {
        return false;
    }

    return true;
}

std::chrono::milliseconds RuntimeSamplerConfigurationV1::CpuSamplingInterval() const noexcept
{
    return std::chrono::milliseconds(cpuSamplingIntervalMilliseconds);
}

std::chrono::milliseconds RuntimeSamplerConfigurationV1::SelectiveThreadSamplingInterval() const noexcept
{
    return std::chrono::milliseconds(selectiveThreadSamplingIntervalMilliseconds);
}

uint32_t RuntimeSamplerConfigurationV1::MaxAllocationSamplesPerMinute() const noexcept
{
    return maxAllocationSamplesPerMinute;
}

bool RuntimeSamplerConfigurationV1::CpuEnabled() const noexcept
{
    return cpuSamplingIntervalMilliseconds != 0;
}

bool RuntimeSamplerConfigurationV1::SelectiveEnabled() const noexcept
{
    return selectiveThreadSamplingIntervalMilliseconds != 0;
}

bool RuntimeSamplerConfigurationV1::ThreadSamplingEnabled() const noexcept
{
    return CpuEnabled() || SelectiveEnabled();
}

bool RuntimeSamplerConfigurationV1::AllocationEnabled() const noexcept
{
    return maxAllocationSamplesPerMinute != 0;
}

bool RuntimeSamplerConfigurationV1::AnyFeatureEnabled() const noexcept
{
    return ThreadSamplingEnabled() || AllocationEnabled();
}

bool RuntimeSamplerConfigurationV1::operator==(const RuntimeSamplerConfigurationV1& other) const noexcept
{
    return cpuSamplingIntervalMilliseconds == other.cpuSamplingIntervalMilliseconds &&
           selectiveThreadSamplingIntervalMilliseconds == other.selectiveThreadSamplingIntervalMilliseconds &&
           maxAllocationSamplesPerMinute == other.maxAllocationSamplesPerMinute;
}

bool RuntimeSamplerConfigurationV1::operator!=(const RuntimeSamplerConfigurationV1& other) const noexcept
{
    return !(*this == other);
}

RuntimeSamplerStateQueryResult EncodeRuntimeSamplerStateV1(const RuntimeSamplerState& state,
                                                           RuntimeSamplerStateV1*     encoded) noexcept
{
    if (encoded == nullptr)
    {
        return RuntimeSamplerStateQueryResult::InvalidArgument;
    }

    if (encoded->structureSize != sizeof(RuntimeSamplerStateV1))
    {
        return RuntimeSamplerStateQueryResult::UnsupportedLayout;
    }

    *encoded = {sizeof(RuntimeSamplerStateV1), static_cast<uint32_t>(state.authority), state.configuration};
    return RuntimeSamplerStateQueryResult::Succeeded;
}

} // namespace continuous_profiler
