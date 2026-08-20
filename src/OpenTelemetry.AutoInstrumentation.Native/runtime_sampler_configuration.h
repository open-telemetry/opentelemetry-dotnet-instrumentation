// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#ifndef OTEL_RUNTIME_SAMPLER_CONFIGURATION_H_
#define OTEL_RUNTIME_SAMPLER_CONFIGURATION_H_

#include <cstddef>
#include <cstdint>
#include <optional>
#include <type_traits>

namespace continuous_profiler
{

constexpr std::uint32_t kRuntimeSamplerSchemaVersionV1 = 1;

// Versioned request payload for the runtime sampler configuration ABI. Numeric
// zero disables the corresponding producer. Reserved fields must be zero.
struct RuntimeSamplerConfigurationV1
{
    std::uint32_t structureSize;
    std::uint32_t schemaVersion;
    std::uint32_t periodicThreadSamplingIntervalMilliseconds;
    std::uint32_t selectiveThreadSamplingIntervalMilliseconds;
    std::uint32_t maxAllocationSamplesPerMinute;
    std::uint32_t reserved0;
    std::uint32_t reserved1;
    std::uint32_t reserved2;
};

// Versioned output payload used by a state-query export. The active
// configuration is one complete normalized snapshot.
struct RuntimeSamplerStateV1
{
    std::uint32_t                 structureSize;
    std::uint32_t                 schemaVersion;
    std::uint64_t                 generation;
    RuntimeSamplerConfigurationV1 activeConfiguration;
};

// These values are part of the exported ABI contract. Do not reorder them.
enum class RuntimeSamplerApplyResult : std::int32_t
{
    Applied                      = 0,
    NoChange                     = 1,
    IgnoredInitialConfiguration  = 2,
    RejectedInvalidArgument      = 3,
    RejectedUnsupportedSchema    = 4,
    RejectedReservedField        = 5,
    RejectedInvalidConfiguration = 6,
    RejectedUnsupportedRuntime   = 7,
    BootstrapFailed              = 8,
    ActivationFailed             = 9,
    ShuttingDown                 = 10
};

// These values are part of the exported ABI contract. Do not reorder them.
enum class RuntimeSamplerStateQueryResult : std::int32_t
{
    Succeeded            = 0,
    InvalidArgument      = 1,
    UnsupportedSchema    = 2,
    ReservedFieldNonZero = 3
};

enum class RuntimeSamplerAllocationSupport : std::uint8_t
{
    Unsupported = 0,
    Supported   = 1
};

// Canonical native value. An empty optional means that the corresponding
// sampling mode is disabled.
struct RuntimeSamplerConfiguration
{
    std::optional<std::uint32_t> periodicThreadSamplingIntervalMilliseconds;
    std::optional<std::uint32_t> selectiveThreadSamplingIntervalMilliseconds;
    std::optional<std::uint32_t> maxAllocationSamplesPerMinute;

    bool HasThreadSampling() const noexcept;
    bool HasAllocationSampling() const noexcept;
    bool operator==(const RuntimeSamplerConfiguration& other) const noexcept;
    bool operator!=(const RuntimeSamplerConfiguration& other) const noexcept;
};

// Decodes and validates one complete candidate without performing any producer
// lifecycle work. The output remains unchanged when the candidate is rejected.
RuntimeSamplerApplyResult DecodeRuntimeSamplerConfigurationV1(
    const RuntimeSamplerConfigurationV1* configuration,
    RuntimeSamplerAllocationSupport      allocationSupport,
    RuntimeSamplerConfiguration&         decodedConfiguration) noexcept;

// Encodes the authoritative native value for a state-query response. The caller
// initializes structureSize and schemaVersion and zeroes all reserved fields in
// activeConfiguration before calling this function.
RuntimeSamplerStateQueryResult EncodeRuntimeSamplerStateV1(const RuntimeSamplerConfiguration& configuration,
                                                           std::uint64_t                      generation,
                                                           RuntimeSamplerStateV1*             state) noexcept;

static_assert(sizeof(std::uint32_t) == 4, "Runtime sampler ABI requires 32-bit uint32_t.");
static_assert(sizeof(std::uint64_t) == 8, "Runtime sampler ABI requires 64-bit uint64_t.");

static_assert(std::is_standard_layout<RuntimeSamplerConfigurationV1>::value,
              "RuntimeSamplerConfigurationV1 must have a stable ABI layout.");
static_assert(std::is_trivially_copyable<RuntimeSamplerConfigurationV1>::value,
              "RuntimeSamplerConfigurationV1 must be trivially copyable.");
static_assert(sizeof(RuntimeSamplerConfigurationV1) == 32, "RuntimeSamplerConfigurationV1 ABI size changed.");
static_assert(offsetof(RuntimeSamplerConfigurationV1, structureSize) == 0,
              "RuntimeSamplerConfigurationV1.structureSize ABI offset changed.");
static_assert(offsetof(RuntimeSamplerConfigurationV1, schemaVersion) == 4,
              "RuntimeSamplerConfigurationV1.schemaVersion ABI offset changed.");
static_assert(offsetof(RuntimeSamplerConfigurationV1, periodicThreadSamplingIntervalMilliseconds) == 8,
              "RuntimeSamplerConfigurationV1.periodicThreadSamplingIntervalMilliseconds ABI offset changed.");
static_assert(offsetof(RuntimeSamplerConfigurationV1, selectiveThreadSamplingIntervalMilliseconds) == 12,
              "RuntimeSamplerConfigurationV1.selectiveThreadSamplingIntervalMilliseconds ABI offset changed.");
static_assert(offsetof(RuntimeSamplerConfigurationV1, maxAllocationSamplesPerMinute) == 16,
              "RuntimeSamplerConfigurationV1.maxAllocationSamplesPerMinute ABI offset changed.");
static_assert(offsetof(RuntimeSamplerConfigurationV1, reserved0) == 20,
              "RuntimeSamplerConfigurationV1.reserved0 ABI offset changed.");
static_assert(offsetof(RuntimeSamplerConfigurationV1, reserved1) == 24,
              "RuntimeSamplerConfigurationV1.reserved1 ABI offset changed.");
static_assert(offsetof(RuntimeSamplerConfigurationV1, reserved2) == 28,
              "RuntimeSamplerConfigurationV1.reserved2 ABI offset changed.");

static_assert(std::is_standard_layout<RuntimeSamplerStateV1>::value,
              "RuntimeSamplerStateV1 must have a stable ABI layout.");
static_assert(std::is_trivially_copyable<RuntimeSamplerStateV1>::value,
              "RuntimeSamplerStateV1 must be trivially copyable.");
static_assert(sizeof(RuntimeSamplerStateV1) == 48, "RuntimeSamplerStateV1 ABI size changed.");
static_assert(offsetof(RuntimeSamplerStateV1, structureSize) == 0,
              "RuntimeSamplerStateV1.structureSize ABI offset changed.");
static_assert(offsetof(RuntimeSamplerStateV1, schemaVersion) == 4,
              "RuntimeSamplerStateV1.schemaVersion ABI offset changed.");
static_assert(offsetof(RuntimeSamplerStateV1, generation) == 8, "RuntimeSamplerStateV1.generation ABI offset changed.");
static_assert(offsetof(RuntimeSamplerStateV1, activeConfiguration) == 16,
              "RuntimeSamplerStateV1.activeConfiguration ABI offset changed.");

static_assert(sizeof(RuntimeSamplerApplyResult) == 4, "RuntimeSamplerApplyResult ABI size changed.");
static_assert(sizeof(RuntimeSamplerStateQueryResult) == 4, "RuntimeSamplerStateQueryResult ABI size changed.");

} // namespace continuous_profiler

#endif // OTEL_RUNTIME_SAMPLER_CONFIGURATION_H_
