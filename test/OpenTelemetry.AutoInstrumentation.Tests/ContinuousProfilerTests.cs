// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.ContinuousProfiler;
using OpenTelemetry.AutoInstrumentation.PluginApi.SelectiveSampling;

namespace OpenTelemetry.AutoInstrumentation.Tests;

public class ContinuousProfilerTests
{
    private static readonly TimeSpan ExportTimeout = TimeSpan.FromSeconds(1);

    [Fact]
    public void BufferProcessorIsolatesNativeReadFailuresAndForwardsSamplingInterval()
    {
        uint? observedSamplingInterval = null;
        var handlers = CreateHandlers();
        handlers.Add(
            SampleType.SelectedThreads,
            ((_, _, samplingInterval, _) => observedSamplingInterval = samplingInterval, ExportTimeout));
        var bufferProcessor = new BufferProcessor(
            handlers,
            (sampleType, _) =>
            {
                if (sampleType == SampleType.Continuous)
                {
                    throw new DllNotFoundException("Test native read failure.");
                }

                return (1, 123u);
            });

        var exception = Record.Exception(bufferProcessor.Process);

        Assert.Null(exception);
        Assert.Equal(123u, observedSamplingInterval);
    }

    [Fact]
    public void BestEffortNativeProfilerShutdownHonorsOwnershipAndSwallowsFailures()
    {
        var calls = 0;

        Instrumentation.ShutdownNativeContinuousProfilerBestEffort(
            false,
            () =>
            {
                calls++;
                return true;
            });
        Assert.Equal(0, calls);

        Instrumentation.ShutdownNativeContinuousProfilerBestEffort(
            true,
            () =>
            {
                calls++;
                return false;
            });
        Assert.Equal(1, calls);

        var exception = Record.Exception(
            () => Instrumentation.ShutdownNativeContinuousProfilerBestEffort(
                true,
                () => throw new DllNotFoundException("Test native shutdown failure.")));

        Assert.Null(exception);
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void AllocationSamplingPreparationIsPlatformAware(
        bool allocationSamplingEnabled,
        bool runtimeAllocationSamplingConfigured)
    {
        var configuration = Instrumentation.GetEffectiveAllocationSamplingConfiguration(
            allocationSamplingEnabled,
            runtimeAllocationSamplingConfigured);

#if NET
        Assert.Equal(allocationSamplingEnabled, configuration.Enabled);
        Assert.Equal(
            allocationSamplingEnabled || runtimeAllocationSamplingConfigured,
            configuration.Prepared);
#else
        Assert.False(configuration.Enabled);
        Assert.False(configuration.Prepared);
#endif
    }

    [Fact]
    public void SelectiveSamplingIsPreparedWithoutBeingInitiallyEnabled()
    {
        var configuration = Instrumentation.GetEffectiveSamplingConfiguration(
            false,
            100,
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(1),
            true);

        Assert.False(configuration.Enabled);
        Assert.True(configuration.Prepared);
    }

    [Theory]
    [InlineData(0, 1, 1, true)]
    [InlineData(1, 0, 1, true)]
    [InlineData(1, 1, 0, true)]
    [InlineData(1, 1, 1, false)]
    public void InvalidSamplingConfigurationIsNotEnabledOrPrepared(
        uint samplingInterval,
        int exportIntervalMilliseconds,
        int exportTimeoutMilliseconds,
        bool exporterConfigured)
    {
        var configuration = Instrumentation.GetEffectiveSamplingConfiguration(
            true,
            samplingInterval,
            TimeSpan.FromMilliseconds(exportIntervalMilliseconds),
            TimeSpan.FromMilliseconds(exportTimeoutMilliseconds),
            exporterConfigured);

        Assert.False(configuration.Enabled);
        Assert.False(configuration.Prepared);
    }

    [Fact]
    public void SelectiveSamplingConfigurationIsEnabledByDefault()
    {
        Assert.True(new SelectiveSamplerConfiguration().Enabled);
    }

    [Theory]
    [InlineData(true, true, true)]
    [InlineData(false, true, false)]
    [InlineData(true, false, false)]
    [InlineData(false, false, false)]
    public void OnlyInitializationOwnerStartsSampleExporter(
        bool nativeConfigurationApplied,
        bool isInitializationOwner,
        bool expected)
    {
        Assert.Equal(
            expected,
            Instrumentation.ShouldStartSampleExporter(nativeConfigurationApplied, isInitializationOwner));
    }

    private static Dictionary<SampleType, (Action<byte[], int, uint, CancellationToken> Handler, TimeSpan ExportTimeout)> CreateHandlers()
    {
        return new Dictionary<SampleType, (Action<byte[], int, uint, CancellationToken> Handler, TimeSpan ExportTimeout)>
        {
            [SampleType.Continuous] = ((_, _, _, _) => { }, ExportTimeout)
        };
    }
}
