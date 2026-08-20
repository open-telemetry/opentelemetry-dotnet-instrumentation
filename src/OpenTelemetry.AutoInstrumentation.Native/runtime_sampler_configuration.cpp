// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#include "runtime_sampler_configuration.h"

namespace continuous_profiler
{
namespace
{

std::optional<std::uint32_t> Normalize(const std::uint32_t value) noexcept
{
    return value == 0 ? std::nullopt : std::optional<std::uint32_t>{value};
}

bool HasNonZeroReservedField(const RuntimeSamplerConfigurationV1& configuration) noexcept
{
    return configuration.reserved0 != 0 || configuration.reserved1 != 0 || configuration.reserved2 != 0;
}

RuntimeSamplerApplyResult ValidateConfiguration(const RuntimeSamplerConfiguration&    configuration,
                                                const RuntimeSamplerAllocationSupport allocationSupport) noexcept
{
    const auto& periodicInterval  = configuration.periodicThreadSamplingIntervalMilliseconds;
    const auto& selectiveInterval = configuration.selectiveThreadSamplingIntervalMilliseconds;

    if (periodicInterval.has_value() && selectiveInterval.has_value() &&
        (periodicInterval.value() <= selectiveInterval.value() ||
         periodicInterval.value() % selectiveInterval.value() != 0))
    {
        return RuntimeSamplerApplyResult::RejectedInvalidConfiguration;
    }

    if (configuration.maxAllocationSamplesPerMinute.has_value() &&
        allocationSupport != RuntimeSamplerAllocationSupport::Supported)
    {
        return RuntimeSamplerApplyResult::RejectedUnsupportedRuntime;
    }

    return RuntimeSamplerApplyResult::Applied;
}

RuntimeSamplerConfigurationV1 EncodeRuntimeSamplerConfigurationV1(
    const RuntimeSamplerConfiguration& configuration) noexcept
{
    return {sizeof(RuntimeSamplerConfigurationV1),
            kRuntimeSamplerSchemaVersionV1,
            configuration.periodicThreadSamplingIntervalMilliseconds.value_or(0),
            configuration.selectiveThreadSamplingIntervalMilliseconds.value_or(0),
            configuration.maxAllocationSamplesPerMinute.value_or(0),
            0,
            0,
            0};
}

} // namespace

bool RuntimeSamplerConfiguration::HasThreadSampling() const noexcept
{
    return periodicThreadSamplingIntervalMilliseconds.has_value() ||
           selectiveThreadSamplingIntervalMilliseconds.has_value();
}

bool RuntimeSamplerConfiguration::HasAllocationSampling() const noexcept
{
    return maxAllocationSamplesPerMinute.has_value();
}

bool RuntimeSamplerConfiguration::operator==(const RuntimeSamplerConfiguration& other) const noexcept
{
    return periodicThreadSamplingIntervalMilliseconds == other.periodicThreadSamplingIntervalMilliseconds &&
           selectiveThreadSamplingIntervalMilliseconds == other.selectiveThreadSamplingIntervalMilliseconds &&
           maxAllocationSamplesPerMinute == other.maxAllocationSamplesPerMinute;
}

bool RuntimeSamplerConfiguration::operator!=(const RuntimeSamplerConfiguration& other) const noexcept
{
    return !(*this == other);
}

RuntimeSamplerApplyResult DecodeRuntimeSamplerConfigurationV1(
    const RuntimeSamplerConfigurationV1*  configuration,
    const RuntimeSamplerAllocationSupport allocationSupport,
    RuntimeSamplerConfiguration&          decodedConfiguration) noexcept
{
    if (configuration == nullptr)
    {
        return RuntimeSamplerApplyResult::RejectedInvalidArgument;
    }

    // structureSize is the only field read until the caller has declared the
    // complete V1 payload. This prevents an undersized payload from being decoded.
    if (configuration->structureSize != sizeof(RuntimeSamplerConfigurationV1))
    {
        return RuntimeSamplerApplyResult::RejectedUnsupportedSchema;
    }

    if (configuration->schemaVersion != kRuntimeSamplerSchemaVersionV1)
    {
        return RuntimeSamplerApplyResult::RejectedUnsupportedSchema;
    }

    if (HasNonZeroReservedField(*configuration))
    {
        return RuntimeSamplerApplyResult::RejectedReservedField;
    }

    RuntimeSamplerConfiguration candidate{Normalize(configuration->periodicThreadSamplingIntervalMilliseconds),
                                          Normalize(configuration->selectiveThreadSamplingIntervalMilliseconds),
                                          Normalize(configuration->maxAllocationSamplesPerMinute)};

    const auto validationResult = ValidateConfiguration(candidate, allocationSupport);
    if (validationResult != RuntimeSamplerApplyResult::Applied)
    {
        return validationResult;
    }

    decodedConfiguration = candidate;
    return RuntimeSamplerApplyResult::Applied;
}

RuntimeSamplerStateQueryResult EncodeRuntimeSamplerStateV1(const RuntimeSamplerConfiguration& configuration,
                                                           const std::uint64_t                generation,
                                                           RuntimeSamplerStateV1*             state) noexcept
{
    if (state == nullptr)
    {
        return RuntimeSamplerStateQueryResult::InvalidArgument;
    }

    if (state->structureSize != sizeof(RuntimeSamplerStateV1))
    {
        return RuntimeSamplerStateQueryResult::UnsupportedSchema;
    }

    if (state->schemaVersion != kRuntimeSamplerSchemaVersionV1)
    {
        return RuntimeSamplerStateQueryResult::UnsupportedSchema;
    }

    if (HasNonZeroReservedField(state->activeConfiguration))
    {
        return RuntimeSamplerStateQueryResult::ReservedFieldNonZero;
    }

    RuntimeSamplerStateV1 encodedState{sizeof(RuntimeSamplerStateV1), kRuntimeSamplerSchemaVersionV1, generation,
                                       EncodeRuntimeSamplerConfigurationV1(configuration)};

    *state = encodedState;
    return RuntimeSamplerStateQueryResult::Succeeded;
}

} // namespace continuous_profiler
