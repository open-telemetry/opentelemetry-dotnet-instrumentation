#include "pch.h"

#include "../../src/OpenTelemetry.AutoInstrumentation.Native/cor_profiler.h"
#include "../../src/OpenTelemetry.AutoInstrumentation.Native/runtime_sampler_configuration.h"

#include <cstddef>
#include <type_traits>

#ifdef _WIN32
HINSTANCE DllHandle = nullptr;
#endif

extern "C" std::int32_t STDAPICALLTYPE
ApplyContinuousProfilerConfigurationV1(const continuous_profiler::RuntimeSamplerConfigurationV1* request,
                                       continuous_profiler::RuntimeSamplerAuthority              authority,
                                       continuous_profiler::RuntimeSamplerStateV1*               actualState);
extern "C" std::int32_t STDAPICALLTYPE
GetContinuousProfilerStateV1(continuous_profiler::RuntimeSamplerStateV1* actualState);

using namespace continuous_profiler;
using namespace std::chrono_literals;

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

namespace
{

RuntimeSamplerConfigurationV1 Configuration(const uint32_t cpu, const uint32_t selective, const uint32_t allocation)
{
    return {sizeof(RuntimeSamplerConfigurationV1), cpu, selective, allocation};
}

class NetFrameworkCorProfiler final : public trace::CorProfiler
{
public:
    NetFrameworkCorProfiler()
    {
        info_   = nullptr;
        info12_ = nullptr;
    }
};

} // namespace

TEST(RuntimeSamplerConfigurationTest, ZeroValuesDisableAllFeatures)
{
    const auto configuration = Configuration(0, 0, 0);

    ASSERT_TRUE(configuration.IsValid());

    EXPECT_EQ(0ms, configuration.CpuSamplingInterval());
    EXPECT_EQ(0ms, configuration.SelectiveThreadSamplingInterval());
    EXPECT_EQ(0u, configuration.MaxAllocationSamplesPerMinute());
    EXPECT_FALSE(configuration.ThreadSamplingEnabled());
    EXPECT_FALSE(configuration.AllocationEnabled());
    EXPECT_FALSE(configuration.AnyFeatureEnabled());
}

TEST(RuntimeSamplerConfigurationTest, NonzeroValuesEnableTheirFeatures)
{
    const auto configuration = Configuration(1000, 0, 200);

    ASSERT_TRUE(configuration.IsValid());

    EXPECT_EQ(1000ms, configuration.CpuSamplingInterval());
    EXPECT_EQ(0ms, configuration.SelectiveThreadSamplingInterval());
    EXPECT_EQ(200u, configuration.MaxAllocationSamplesPerMinute());
    EXPECT_TRUE(configuration.CpuEnabled());
    EXPECT_TRUE(configuration.ThreadSamplingEnabled());
    EXPECT_TRUE(configuration.AllocationEnabled());
    EXPECT_TRUE(configuration.AnyFeatureEnabled());
}

TEST(RuntimeSamplerConfigurationTest, SelectiveOnlyConfigurationEnablesThreadSampling)
{
    const auto configuration = Configuration(0, 20, 0);

    ASSERT_TRUE(configuration.IsValid());

    EXPECT_FALSE(configuration.CpuEnabled());
    EXPECT_TRUE(configuration.SelectiveEnabled());
    EXPECT_TRUE(configuration.ThreadSamplingEnabled());
    EXPECT_FALSE(configuration.AllocationEnabled());
}

TEST(RuntimeSamplerConfigurationTest, CpuIntervalMustBeGreaterThanSelectiveInterval)
{
    EXPECT_FALSE(Configuration(20, 20, 0).IsValid());
    EXPECT_FALSE(Configuration(10, 20, 0).IsValid());
}

TEST(RuntimeSamplerConfigurationTest, CpuIntervalMustBeAnExactMultipleOfSelectiveInterval)
{
    EXPECT_FALSE(Configuration(100, 30, 0).IsValid());
    EXPECT_TRUE(Configuration(100, 20, 0).IsValid());
}

TEST(RuntimeSamplerConfigurationTest, V1ConfigurationRepresentsOneCompleteSnapshot)
{
    const auto configuration = Configuration(1000, 20, 200);

    ASSERT_TRUE(configuration.IsValid());
    EXPECT_EQ(1000ms, configuration.CpuSamplingInterval());
    EXPECT_EQ(20ms, configuration.SelectiveThreadSamplingInterval());
    EXPECT_EQ(200u, configuration.MaxAllocationSamplesPerMinute());
}

TEST(RuntimeSamplerConfigurationTest, V1ConfigurationRejectsTheWholeInvalidCandidate)
{
    EXPECT_FALSE(Configuration(1000, 30, 0).IsValid());
}

TEST(RuntimeSamplerConfigurationTest, UnsupportedAllocationIsNormalizedForSeedAndRejectedForControlPlane)
{
    NetFrameworkCorProfiler profiler;
    const auto              configuration = Configuration(0, 0, 200);
    RuntimeSamplerStateV1   state{sizeof(RuntimeSamplerStateV1)};

    EXPECT_EQ(RuntimeSamplerApplyResult::Applied,
              profiler.ApplyContinuousProfilerConfigurationV1(&configuration, RuntimeSamplerAuthority::Seed, &state));
    EXPECT_EQ(static_cast<uint32_t>(RuntimeSamplerAuthority::Seed), state.authority);
    EXPECT_EQ(0u, state.committedConfiguration.maxAllocationSamplesPerMinute);

    EXPECT_EQ(RuntimeSamplerApplyResult::RejectedUnsupportedRuntime,
              profiler.ApplyContinuousProfilerConfigurationV1(&configuration, RuntimeSamplerAuthority::ControlPlane,
                                                              &state));
    EXPECT_EQ(static_cast<uint32_t>(RuntimeSamplerAuthority::Seed), state.authority);
    EXPECT_EQ(0u, state.committedConfiguration.maxAllocationSamplesPerMinute);
}

TEST(RuntimeSamplerConfigurationTest, V1StateEncoderReturnsTheCompleteAuthoritativeState)
{
    const RuntimeSamplerState state{RuntimeSamplerAuthority::ControlPlane, Configuration(1000, 20, 200)};
    RuntimeSamplerStateV1     encoded{sizeof(RuntimeSamplerStateV1)};

    ASSERT_EQ(RuntimeSamplerStateQueryResult::Succeeded, EncodeRuntimeSamplerStateV1(state, &encoded));
    EXPECT_EQ(sizeof(RuntimeSamplerStateV1), encoded.structureSize);
    EXPECT_EQ(static_cast<uint32_t>(RuntimeSamplerAuthority::ControlPlane), encoded.authority);
    EXPECT_EQ(sizeof(RuntimeSamplerConfigurationV1), encoded.committedConfiguration.structureSize);
    EXPECT_EQ(1000u, encoded.committedConfiguration.cpuSamplingIntervalMilliseconds);
    EXPECT_EQ(20u, encoded.committedConfiguration.selectiveThreadSamplingIntervalMilliseconds);
    EXPECT_EQ(200u, encoded.committedConfiguration.maxAllocationSamplesPerMinute);
}

TEST(RuntimeSamplerConfigurationTest, V1StateEncoderRejectsInvalidOutputLayout)
{
    const RuntimeSamplerState state{};
    RuntimeSamplerStateV1     encoded{};

    EXPECT_EQ(RuntimeSamplerStateQueryResult::InvalidArgument, EncodeRuntimeSamplerStateV1(state, nullptr));
    EXPECT_EQ(RuntimeSamplerStateQueryResult::UnsupportedLayout, EncodeRuntimeSamplerStateV1(state, &encoded));
}

TEST(RuntimeSamplerConfigurationTest, RuntimeConfigurationExportsHaveStableSignatures)
{
    using ApplyFunction = std::int32_t(STDAPICALLTYPE*)(const RuntimeSamplerConfigurationV1*, RuntimeSamplerAuthority,
                                                        RuntimeSamplerStateV1*);
    using CorProfilerApplyFunction = RuntimeSamplerApplyResult (
        trace::CorProfiler::*)(const RuntimeSamplerConfigurationV1*, RuntimeSamplerAuthority, RuntimeSamplerStateV1*);
    using GetFunction = std::int32_t(STDAPICALLTYPE*)(RuntimeSamplerStateV1*);

    static_assert(std::is_same_v<decltype(&ApplyContinuousProfilerConfigurationV1), ApplyFunction>);
    static_assert(std::is_same_v<decltype(&trace::CorProfiler::ApplyContinuousProfilerConfigurationV1),
                                 CorProfilerApplyFunction>);
    static_assert(std::is_same_v<decltype(&GetContinuousProfilerStateV1), GetFunction>);
}
