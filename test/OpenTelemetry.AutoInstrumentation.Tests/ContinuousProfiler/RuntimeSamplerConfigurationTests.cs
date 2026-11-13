// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.InteropServices;
using OpenTelemetry.AutoInstrumentation.ContinuousProfiler;

namespace OpenTelemetry.AutoInstrumentation.Tests.ContinuousProfiler;

public class RuntimeSamplerConfigurationTests
{
    [Fact]
    public void InteropLayoutMatchesNativeContract()
    {
        Assert.Equal(16, Marshal.SizeOf<RuntimeSamplerConfiguration>());
        Assert.Equal(RuntimeSamplerConfiguration.Size, (uint)Marshal.SizeOf<RuntimeSamplerConfiguration>());
        Assert.Equal(0, Marshal.OffsetOf<RuntimeSamplerConfiguration>(nameof(RuntimeSamplerConfiguration.StructureSize)).ToInt32());
        Assert.Equal(4, Marshal.OffsetOf<RuntimeSamplerConfiguration>(nameof(RuntimeSamplerConfiguration.CpuSamplingIntervalMilliseconds)).ToInt32());
        Assert.Equal(8, Marshal.OffsetOf<RuntimeSamplerConfiguration>(nameof(RuntimeSamplerConfiguration.SelectiveThreadSamplingIntervalMilliseconds)).ToInt32());
        Assert.Equal(12, Marshal.OffsetOf<RuntimeSamplerConfiguration>(nameof(RuntimeSamplerConfiguration.MaxAllocationSamplesPerMinute)).ToInt32());

        Assert.Equal(24, Marshal.SizeOf<RuntimeSamplerState>());
        Assert.Equal(RuntimeSamplerState.Size, (uint)Marshal.SizeOf<RuntimeSamplerState>());
        Assert.Equal(0, Marshal.OffsetOf<RuntimeSamplerState>(nameof(RuntimeSamplerState.StructureSize)).ToInt32());
        Assert.Equal(4, Marshal.OffsetOf<RuntimeSamplerState>(nameof(RuntimeSamplerState.Authority)).ToInt32());
        Assert.Equal(8, Marshal.OffsetOf<RuntimeSamplerState>(nameof(RuntimeSamplerState.CommittedConfiguration)).ToInt32());
    }

    [Theory]
    [InlineData(0u, 0u, true)]
    [InlineData(1000u, 100u, true)]
    [InlineData(1000u, 300u, false)]
    public void ValidatesCpuAndSelectiveIntervals(uint cpuInterval, uint selectiveInterval, bool expected)
    {
        var configuration = new RuntimeSamplerConfiguration(cpuInterval, selectiveInterval, 0);

        Assert.Equal(RuntimeSamplerConfiguration.Size, configuration.StructureSize);
        Assert.Equal(expected, configuration.IsValid);
    }
}
