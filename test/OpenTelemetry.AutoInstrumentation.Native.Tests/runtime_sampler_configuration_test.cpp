#include "pch.h"

#include "../../src/OpenTelemetry.AutoInstrumentation.Native/cor_profiler.h"
#include "../../src/OpenTelemetry.AutoInstrumentation.Native/runtime_sampler_configuration.h"

#include <cstddef>
#include <type_traits>

#ifdef _WIN32
HINSTANCE DllHandle = nullptr;
#endif

extern "C" std::int32_t STDAPICALLTYPE
ApplyContinuousProfilerConfiguration(const continuous_profiler::RuntimeSamplerConfiguration* request,
                                     continuous_profiler::RuntimeSamplerAuthority            authority,
                                     continuous_profiler::RuntimeSamplerState*               actualState);
extern "C" std::int32_t STDAPICALLTYPE
GetContinuousProfilerState(continuous_profiler::RuntimeSamplerState* actualState);

using namespace continuous_profiler;
using namespace std::chrono_literals;

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

namespace
{

RuntimeSamplerConfiguration Configuration(const uint32_t cpu, const uint32_t selective, const uint32_t allocation)
{
    return {sizeof(RuntimeSamplerConfiguration), cpu, selective, allocation};
}

class NetFrameworkCorProfiler final : public trace::CorProfiler
{
public:
    NetFrameworkCorProfiler()
    {
        info_   = nullptr;
        info12_ = nullptr;
        InitializeRuntimeSamplerService();
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

TEST(RuntimeSamplerConfigurationTest, ConfigurationRepresentsOneCompleteSnapshot)
{
    const auto configuration = Configuration(1000, 20, 200);

    ASSERT_TRUE(configuration.IsValid());
    EXPECT_EQ(1000ms, configuration.CpuSamplingInterval());
    EXPECT_EQ(20ms, configuration.SelectiveThreadSamplingInterval());
    EXPECT_EQ(200u, configuration.MaxAllocationSamplesPerMinute());
}

TEST(RuntimeSamplerConfigurationTest, ConfigurationRejectsTheWholeInvalidCandidate)
{
    EXPECT_FALSE(Configuration(1000, 30, 0).IsValid());
}

TEST(RuntimeSamplerConfigurationTest, UnsupportedAllocationIsNormalizedForSeedAndRejectedForControlPlane)
{
    NetFrameworkCorProfiler profiler;
    const auto              configuration = Configuration(0, 0, 200);
    RuntimeSamplerState     state{sizeof(RuntimeSamplerState)};

    EXPECT_EQ(RuntimeSamplerApplyResult::Applied,
              profiler.ApplyContinuousProfilerConfiguration(&configuration, RuntimeSamplerAuthority::Seed, &state));
    EXPECT_EQ(static_cast<uint32_t>(RuntimeSamplerAuthority::Seed), state.authority);
    EXPECT_EQ(0u, state.committedConfiguration.maxAllocationSamplesPerMinute);

    EXPECT_EQ(RuntimeSamplerApplyResult::RejectedUnsupportedRuntime,
              profiler.ApplyContinuousProfilerConfiguration(&configuration, RuntimeSamplerAuthority::ControlPlane,
                                                            &state));
    EXPECT_EQ(static_cast<uint32_t>(RuntimeSamplerAuthority::Seed), state.authority);
    EXPECT_EQ(0u, state.committedConfiguration.maxAllocationSamplesPerMinute);
}

TEST(RuntimeSamplerConfigurationTest, StateEncoderReturnsTheCompleteAuthoritativeState)
{
    const RuntimeSamplerControllerState state{RuntimeSamplerAuthority::ControlPlane, Configuration(1000, 20, 200)};
    RuntimeSamplerState                 encoded{sizeof(RuntimeSamplerState)};

    ASSERT_EQ(RuntimeSamplerStateQueryResult::Succeeded, EncodeRuntimeSamplerState(state, &encoded));
    EXPECT_EQ(sizeof(RuntimeSamplerState), encoded.structureSize);
    EXPECT_EQ(static_cast<uint32_t>(RuntimeSamplerAuthority::ControlPlane), encoded.authority);
    EXPECT_EQ(sizeof(RuntimeSamplerConfiguration), encoded.committedConfiguration.structureSize);
    EXPECT_EQ(1000u, encoded.committedConfiguration.cpuSamplingIntervalMilliseconds);
    EXPECT_EQ(20u, encoded.committedConfiguration.selectiveThreadSamplingIntervalMilliseconds);
    EXPECT_EQ(200u, encoded.committedConfiguration.maxAllocationSamplesPerMinute);
}

TEST(RuntimeSamplerConfigurationTest, StateEncoderRejectsInvalidOutputLayout)
{
    const RuntimeSamplerControllerState state{};
    RuntimeSamplerState                 encoded{};

    EXPECT_EQ(RuntimeSamplerStateQueryResult::InvalidArgument, EncodeRuntimeSamplerState(state, nullptr));
    EXPECT_EQ(RuntimeSamplerStateQueryResult::UnsupportedLayout, EncodeRuntimeSamplerState(state, &encoded));
}

TEST(RuntimeSamplerConfigurationTest, RuntimeConfigurationExportsHaveStableSignatures)
{
    using ApplyFunction = std::int32_t(STDAPICALLTYPE*)(const RuntimeSamplerConfiguration*, RuntimeSamplerAuthority,
                                                        RuntimeSamplerState*);
    using CorProfilerApplyFunction = RuntimeSamplerApplyResult (
        trace::CorProfiler::*)(const RuntimeSamplerConfiguration*, RuntimeSamplerAuthority, RuntimeSamplerState*);
    using GetFunction = std::int32_t(STDAPICALLTYPE*)(RuntimeSamplerState*);

    static_assert(std::is_same_v<decltype(&ApplyContinuousProfilerConfiguration), ApplyFunction>);
    static_assert(
        std::is_same_v<decltype(&trace::CorProfiler::ApplyContinuousProfilerConfiguration), CorProfilerApplyFunction>);
    static_assert(std::is_same_v<decltype(&GetContinuousProfilerState), GetFunction>);
}
