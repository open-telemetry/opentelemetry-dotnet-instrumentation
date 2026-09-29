/*
 * Copyright The OpenTelemetry Authors
 * SPDX-License-Identifier: Apache-2.0
 */

#include "runtime_sampler_configuration.h"

namespace continuous_profiler
{

bool RuntimeSamplerConfiguration::IsValid() const noexcept
{
    if (cpuSamplingIntervalMilliseconds != 0 && selectiveThreadSamplingIntervalMilliseconds != 0 &&
        (cpuSamplingIntervalMilliseconds <= selectiveThreadSamplingIntervalMilliseconds ||
         cpuSamplingIntervalMilliseconds % selectiveThreadSamplingIntervalMilliseconds != 0))
    {
        return false;
    }

    return true;
}

std::chrono::milliseconds RuntimeSamplerConfiguration::CpuSamplingInterval() const noexcept
{
    return std::chrono::milliseconds(cpuSamplingIntervalMilliseconds);
}

std::chrono::milliseconds RuntimeSamplerConfiguration::SelectiveThreadSamplingInterval() const noexcept
{
    return std::chrono::milliseconds(selectiveThreadSamplingIntervalMilliseconds);
}

uint32_t RuntimeSamplerConfiguration::MaxAllocationSamplesPerMinute() const noexcept
{
    return maxAllocationSamplesPerMinute;
}

bool RuntimeSamplerConfiguration::CpuEnabled() const noexcept
{
    return cpuSamplingIntervalMilliseconds != 0;
}

bool RuntimeSamplerConfiguration::SelectiveEnabled() const noexcept
{
    return selectiveThreadSamplingIntervalMilliseconds != 0;
}

bool RuntimeSamplerConfiguration::ThreadSamplingEnabled() const noexcept
{
    return CpuEnabled() || SelectiveEnabled();
}

bool RuntimeSamplerConfiguration::AllocationEnabled() const noexcept
{
    return maxAllocationSamplesPerMinute != 0;
}

bool RuntimeSamplerConfiguration::AnyFeatureEnabled() const noexcept
{
    return ThreadSamplingEnabled() || AllocationEnabled();
}

bool RuntimeSamplerConfiguration::operator==(const RuntimeSamplerConfiguration& other) const noexcept
{
    return cpuSamplingIntervalMilliseconds == other.cpuSamplingIntervalMilliseconds &&
           selectiveThreadSamplingIntervalMilliseconds == other.selectiveThreadSamplingIntervalMilliseconds &&
           maxAllocationSamplesPerMinute == other.maxAllocationSamplesPerMinute;
}

bool RuntimeSamplerConfiguration::operator!=(const RuntimeSamplerConfiguration& other) const noexcept
{
    return !(*this == other);
}

RuntimeSamplerStateQueryResult EncodeRuntimeSamplerState(const RuntimeSamplerControllerState& state,
                                                         RuntimeSamplerState*                 encoded) noexcept
{
    if (encoded == nullptr)
    {
        return RuntimeSamplerStateQueryResult::InvalidArgument;
    }

    if (encoded->structureSize != sizeof(RuntimeSamplerState))
    {
        return RuntimeSamplerStateQueryResult::UnsupportedLayout;
    }

    *encoded = {sizeof(RuntimeSamplerState), static_cast<uint32_t>(state.authority), state.configuration};
    return RuntimeSamplerStateQueryResult::Succeeded;
}

} // namespace continuous_profiler
