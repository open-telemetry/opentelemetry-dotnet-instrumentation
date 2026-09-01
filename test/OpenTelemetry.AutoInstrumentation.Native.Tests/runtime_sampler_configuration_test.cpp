// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#include "pch.h"
#include "../../src/OpenTelemetry.AutoInstrumentation.Native/runtime_sampler_configuration.h"

#include <array>
#include <cstddef>
#include <cstdint>
#include <type_traits>

namespace
{

using continuous_profiler::DecodeRuntimeSamplerConfigurationV1;
using continuous_profiler::EncodeRuntimeSamplerStateV1;
using continuous_profiler::kRuntimeSamplerSchemaVersionV1;
using continuous_profiler::RuntimeSamplerAllocationSupport;
using continuous_profiler::RuntimeSamplerApplyResult;
using continuous_profiler::RuntimeSamplerConfiguration;
using continuous_profiler::RuntimeSamplerConfigurationV1;
using continuous_profiler::RuntimeSamplerStateQueryResult;
using continuous_profiler::RuntimeSamplerStateV1;

RuntimeSamplerConfigurationV1 CreateConfigurationV1(const std::uint32_t periodicInterval  = 0,
                                                    const std::uint32_t selectiveInterval = 0,
                                                    const std::uint32_t allocationRate    = 0)
{
    return {sizeof(RuntimeSamplerConfigurationV1),
            kRuntimeSamplerSchemaVersionV1,
            periodicInterval,
            selectiveInterval,
            allocationRate,
            0,
            0,
            0};
}

RuntimeSamplerStateV1 CreateStateV1()
{
    RuntimeSamplerStateV1 state{};
    state.structureSize = sizeof(RuntimeSamplerStateV1);
    state.schemaVersion = kRuntimeSamplerSchemaVersionV1;
    return state;
}

RuntimeSamplerConfiguration CreateSentinelConfiguration()
{
    return {11u, 1u, 7u};
}

void AssertSentinelConfiguration(const RuntimeSamplerConfiguration& configuration)
{
    ASSERT_EQ(11u, configuration.periodicThreadSamplingIntervalMilliseconds.value());
    ASSERT_EQ(1u, configuration.selectiveThreadSamplingIntervalMilliseconds.value());
    ASSERT_EQ(7u, configuration.maxAllocationSamplesPerMinute.value());
}

} // namespace

TEST(RuntimeSamplerConfigurationAbiTest, V1LayoutsAreStableOnEveryNativeArchitecture)
{
    static_assert(std::is_standard_layout<RuntimeSamplerConfigurationV1>::value, "V1 request must be standard layout.");
    static_assert(std::is_trivially_copyable<RuntimeSamplerConfigurationV1>::value,
                  "V1 request must be trivially copyable.");
    static_assert(std::is_standard_layout<RuntimeSamplerStateV1>::value, "V1 state must be standard layout.");
    static_assert(std::is_trivially_copyable<RuntimeSamplerStateV1>::value, "V1 state must be trivially copyable.");

    ASSERT_EQ(32u, sizeof(RuntimeSamplerConfigurationV1));
    ASSERT_EQ(0u, offsetof(RuntimeSamplerConfigurationV1, structureSize));
    ASSERT_EQ(4u, offsetof(RuntimeSamplerConfigurationV1, schemaVersion));
    ASSERT_EQ(8u, offsetof(RuntimeSamplerConfigurationV1, periodicThreadSamplingIntervalMilliseconds));
    ASSERT_EQ(12u, offsetof(RuntimeSamplerConfigurationV1, selectiveThreadSamplingIntervalMilliseconds));
    ASSERT_EQ(16u, offsetof(RuntimeSamplerConfigurationV1, maxAllocationSamplesPerMinute));
    ASSERT_EQ(20u, offsetof(RuntimeSamplerConfigurationV1, reserved0));
    ASSERT_EQ(24u, offsetof(RuntimeSamplerConfigurationV1, reserved1));
    ASSERT_EQ(28u, offsetof(RuntimeSamplerConfigurationV1, reserved2));

    ASSERT_EQ(48u, sizeof(RuntimeSamplerStateV1));
    ASSERT_EQ(0u, offsetof(RuntimeSamplerStateV1, structureSize));
    ASSERT_EQ(4u, offsetof(RuntimeSamplerStateV1, schemaVersion));
    ASSERT_EQ(8u, offsetof(RuntimeSamplerStateV1, generation));
    ASSERT_EQ(16u, offsetof(RuntimeSamplerStateV1, activeConfiguration));

    ASSERT_EQ(4u, sizeof(RuntimeSamplerApplyResult));
    ASSERT_EQ(4u, sizeof(RuntimeSamplerStateQueryResult));
}

TEST(RuntimeSamplerConfigurationAbiTest, ResultValuesRemainStable)
{
    ASSERT_EQ(0, static_cast<std::int32_t>(RuntimeSamplerApplyResult::Applied));
    ASSERT_EQ(1, static_cast<std::int32_t>(RuntimeSamplerApplyResult::NoChange));
    ASSERT_EQ(2, static_cast<std::int32_t>(RuntimeSamplerApplyResult::IgnoredInitialConfiguration));
    ASSERT_EQ(3, static_cast<std::int32_t>(RuntimeSamplerApplyResult::RejectedInvalidArgument));
    ASSERT_EQ(4, static_cast<std::int32_t>(RuntimeSamplerApplyResult::RejectedUnsupportedSchema));
    ASSERT_EQ(5, static_cast<std::int32_t>(RuntimeSamplerApplyResult::RejectedReservedField));
    ASSERT_EQ(6, static_cast<std::int32_t>(RuntimeSamplerApplyResult::RejectedInvalidConfiguration));
    ASSERT_EQ(7, static_cast<std::int32_t>(RuntimeSamplerApplyResult::RejectedUnsupportedRuntime));
    ASSERT_EQ(8, static_cast<std::int32_t>(RuntimeSamplerApplyResult::BootstrapFailed));
    ASSERT_EQ(9, static_cast<std::int32_t>(RuntimeSamplerApplyResult::ActivationFailed));
    ASSERT_EQ(10, static_cast<std::int32_t>(RuntimeSamplerApplyResult::ShuttingDown));

    ASSERT_EQ(0, static_cast<std::int32_t>(RuntimeSamplerStateQueryResult::Succeeded));
    ASSERT_EQ(1, static_cast<std::int32_t>(RuntimeSamplerStateQueryResult::InvalidArgument));
    ASSERT_EQ(2, static_cast<std::int32_t>(RuntimeSamplerStateQueryResult::UnsupportedSchema));
    ASSERT_EQ(3, static_cast<std::int32_t>(RuntimeSamplerStateQueryResult::ReservedFieldNonZero));
}

TEST(RuntimeSamplerConfigurationDecodeTest, ZeroValuesProduceTheCanonicalAllDisabledConfiguration)
{
    const auto                  input   = CreateConfigurationV1();
    RuntimeSamplerConfiguration decoded = CreateSentinelConfiguration();

    const auto result =
        DecodeRuntimeSamplerConfigurationV1(&input, RuntimeSamplerAllocationSupport::Unsupported, decoded);

    ASSERT_EQ(RuntimeSamplerApplyResult::Applied, result);
    ASSERT_FALSE(decoded.periodicThreadSamplingIntervalMilliseconds.has_value());
    ASSERT_FALSE(decoded.selectiveThreadSamplingIntervalMilliseconds.has_value());
    ASSERT_FALSE(decoded.maxAllocationSamplesPerMinute.has_value());
    ASSERT_FALSE(decoded.HasThreadSampling());
    ASSERT_FALSE(decoded.HasAllocationSampling());
}

TEST(RuntimeSamplerConfigurationDecodeTest, CompleteValidConfigurationIsDecodedAsOneCandidate)
{
    const auto                  input = CreateConfigurationV1(1000u, 100u, 200u);
    RuntimeSamplerConfiguration decoded{};

    const auto result =
        DecodeRuntimeSamplerConfigurationV1(&input, RuntimeSamplerAllocationSupport::Supported, decoded);

    ASSERT_EQ(RuntimeSamplerApplyResult::Applied, result);
    ASSERT_EQ(1000u, decoded.periodicThreadSamplingIntervalMilliseconds.value());
    ASSERT_EQ(100u, decoded.selectiveThreadSamplingIntervalMilliseconds.value());
    ASSERT_EQ(200u, decoded.maxAllocationSamplesPerMinute.value());
    ASSERT_TRUE(decoded.HasThreadSampling());
    ASSERT_TRUE(decoded.HasAllocationSampling());
}

TEST(RuntimeSamplerConfigurationDecodeTest, InvalidIntervalPairRejectsTheWholeCandidate)
{
    const std::array<std::array<std::uint32_t, 2>, 3> invalidPairs = {{{100u, 100u}, {50u, 100u}, {1000u, 300u}}};

    for (const auto& pair : invalidPairs)
    {
        SCOPED_TRACE(::testing::Message() << "periodic=" << pair[0] << ", selective=" << pair[1]);
        const auto                  input   = CreateConfigurationV1(pair[0], pair[1], 200u);
        RuntimeSamplerConfiguration decoded = CreateSentinelConfiguration();

        const auto result =
            DecodeRuntimeSamplerConfigurationV1(&input, RuntimeSamplerAllocationSupport::Supported, decoded);

        ASSERT_EQ(RuntimeSamplerApplyResult::RejectedInvalidConfiguration, result);
        AssertSentinelConfiguration(decoded);
    }
}

TEST(RuntimeSamplerConfigurationDecodeTest, EveryNonZeroReservedFieldIsRejectedWithoutChangingOutput)
{
    for (auto reservedIndex = 0u; reservedIndex < 3u; reservedIndex++)
    {
        SCOPED_TRACE(::testing::Message() << "reservedIndex=" << reservedIndex);
        auto input = CreateConfigurationV1(1000u, 100u, 200u);
        if (reservedIndex == 0)
        {
            input.reserved0 = 1;
        }
        else if (reservedIndex == 1)
        {
            input.reserved1 = 1;
        }
        else
        {
            input.reserved2 = 1;
        }

        RuntimeSamplerConfiguration decoded = CreateSentinelConfiguration();
        const auto                  result =
            DecodeRuntimeSamplerConfigurationV1(&input, RuntimeSamplerAllocationSupport::Supported, decoded);

        ASSERT_EQ(RuntimeSamplerApplyResult::RejectedReservedField, result);
        AssertSentinelConfiguration(decoded);
    }
}

TEST(RuntimeSamplerConfigurationDecodeTest, AllocationEnableOnUnsupportedRuntimePreservesOutput)
{
    const auto                  input   = CreateConfigurationV1(1000u, 100u, 200u);
    RuntimeSamplerConfiguration decoded = CreateSentinelConfiguration();

    const auto result =
        DecodeRuntimeSamplerConfigurationV1(&input, RuntimeSamplerAllocationSupport::Unsupported, decoded);

    ASSERT_EQ(RuntimeSamplerApplyResult::RejectedUnsupportedRuntime, result);
    AssertSentinelConfiguration(decoded);
}

TEST(RuntimeSamplerConfigurationDecodeTest, NullAndUnsupportedPayloadHeadersPreserveOutput)
{
    RuntimeSamplerConfiguration decoded = CreateSentinelConfiguration();
    ASSERT_EQ(RuntimeSamplerApplyResult::RejectedInvalidArgument,
              DecodeRuntimeSamplerConfigurationV1(nullptr, RuntimeSamplerAllocationSupport::Supported, decoded));
    AssertSentinelConfiguration(decoded);

    auto input          = CreateConfigurationV1(1000u, 100u, 200u);
    input.structureSize = sizeof(RuntimeSamplerConfigurationV1) - 1;
    ASSERT_EQ(RuntimeSamplerApplyResult::RejectedUnsupportedSchema,
              DecodeRuntimeSamplerConfigurationV1(&input, RuntimeSamplerAllocationSupport::Supported, decoded));
    AssertSentinelConfiguration(decoded);

    input.structureSize = sizeof(RuntimeSamplerConfigurationV1) + 1;
    ASSERT_EQ(RuntimeSamplerApplyResult::RejectedUnsupportedSchema,
              DecodeRuntimeSamplerConfigurationV1(&input, RuntimeSamplerAllocationSupport::Supported, decoded));
    AssertSentinelConfiguration(decoded);

    input.structureSize = sizeof(RuntimeSamplerConfigurationV1);
    input.schemaVersion = kRuntimeSamplerSchemaVersionV1 + 1;
    ASSERT_EQ(RuntimeSamplerApplyResult::RejectedUnsupportedSchema,
              DecodeRuntimeSamplerConfigurationV1(&input, RuntimeSamplerAllocationSupport::Supported, decoded));
    AssertSentinelConfiguration(decoded);
}

TEST(RuntimeSamplerStateEncodeTest, NullAndUnsupportedOutputHeadersAreRejected)
{
    const RuntimeSamplerConfiguration configuration{1000u, 100u, 200u};

    ASSERT_EQ(RuntimeSamplerStateQueryResult::InvalidArgument,
              EncodeRuntimeSamplerStateV1(configuration, 17u, nullptr));

    auto state          = CreateStateV1();
    state.structureSize = sizeof(RuntimeSamplerStateV1) - 1;
    ASSERT_EQ(RuntimeSamplerStateQueryResult::UnsupportedSchema,
              EncodeRuntimeSamplerStateV1(configuration, 17u, &state));
    ASSERT_EQ(0u, state.generation);

    state               = CreateStateV1();
    state.schemaVersion = kRuntimeSamplerSchemaVersionV1 + 1;
    ASSERT_EQ(RuntimeSamplerStateQueryResult::UnsupportedSchema,
              EncodeRuntimeSamplerStateV1(configuration, 17u, &state));
    ASSERT_EQ(0u, state.generation);
}

TEST(RuntimeSamplerStateEncodeTest, NonZeroNestedReservedFieldIsRejectedWithoutWritingState)
{
    const RuntimeSamplerConfiguration configuration{1000u, 100u, 200u};
    auto                              state = CreateStateV1();
    state.generation                        = 23u;
    state.activeConfiguration.reserved1     = 1u;

    const auto result = EncodeRuntimeSamplerStateV1(configuration, 17u, &state);

    ASSERT_EQ(RuntimeSamplerStateQueryResult::ReservedFieldNonZero, result);
    ASSERT_EQ(23u, state.generation);
    ASSERT_EQ(1u, state.activeConfiguration.reserved1);
}

TEST(RuntimeSamplerStateEncodeTest, SuccessWritesOneNormalizedCoherentSnapshot)
{
    const RuntimeSamplerConfiguration configuration{1000u, std::nullopt, 200u};
    auto                              state = CreateStateV1();

    const auto result = EncodeRuntimeSamplerStateV1(configuration, 17u, &state);

    ASSERT_EQ(RuntimeSamplerStateQueryResult::Succeeded, result);
    ASSERT_EQ(sizeof(RuntimeSamplerStateV1), state.structureSize);
    ASSERT_EQ(kRuntimeSamplerSchemaVersionV1, state.schemaVersion);
    ASSERT_EQ(17u, state.generation);
    ASSERT_EQ(sizeof(RuntimeSamplerConfigurationV1), state.activeConfiguration.structureSize);
    ASSERT_EQ(kRuntimeSamplerSchemaVersionV1, state.activeConfiguration.schemaVersion);
    ASSERT_EQ(1000u, state.activeConfiguration.periodicThreadSamplingIntervalMilliseconds);
    ASSERT_EQ(0u, state.activeConfiguration.selectiveThreadSamplingIntervalMilliseconds);
    ASSERT_EQ(200u, state.activeConfiguration.maxAllocationSamplesPerMinute);
    ASSERT_EQ(0u, state.activeConfiguration.reserved0);
    ASSERT_EQ(0u, state.activeConfiguration.reserved1);
    ASSERT_EQ(0u, state.activeConfiguration.reserved2);
}
