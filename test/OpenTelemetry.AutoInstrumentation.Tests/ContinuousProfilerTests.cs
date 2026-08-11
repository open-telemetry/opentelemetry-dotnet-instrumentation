// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.ContinuousProfiler;

namespace OpenTelemetry.AutoInstrumentation.Tests;

public class ContinuousProfilerTests
{
    private static readonly TimeSpan ExportTimeout = TimeSpan.FromSeconds(1);

    [Fact]
    public void SampleExporterDoesNotReadUntilStarted()
    {
        using var readAttempted = new ManualResetEventSlim();
        var bufferProcessor = new BufferProcessor(
            CreateHandlers(),
            (_, _) =>
            {
                readAttempted.Set();
                return (0, 0);
            });
        using var sampleExporter = new SampleExporter(
            bufferProcessor,
            TimeSpan.FromMilliseconds(10),
            ExportTimeout);

        Assert.False(readAttempted.Wait(TimeSpan.FromMilliseconds(100)));

        sampleExporter.Start();

        Assert.True(readAttempted.Wait(TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public void SampleExporterCannotStartAfterDisposal()
    {
        var bufferProcessor = new BufferProcessor(CreateHandlers(), (_, _) => (0, 0));
        var sampleExporter = new SampleExporter(
            bufferProcessor,
            TimeSpan.FromMilliseconds(10),
            ExportTimeout);

        sampleExporter.Dispose();

        Assert.Throws<ObjectDisposedException>(sampleExporter.Start);
    }

    [Fact]
    public void BufferProcessorContinuesAfterNativeReadThrows()
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void BestEffortNativeProfilerShutdownInvokesNativeShutdown(bool nativeResult)
    {
        var calls = 0;

        var exception = Record.Exception(
            () => Instrumentation.ShutdownNativeContinuousProfilerBestEffort(
                true,
                () =>
                {
                    calls++;
                    return nativeResult;
                }));

        Assert.Null(exception);
        Assert.Equal(1, calls);
    }

    [Fact]
    public void BestEffortNativeProfilerShutdownDoesNotInvokeNativeShutdownForNonOwner()
    {
        var calls = 0;

        var exception = Record.Exception(
            () => Instrumentation.ShutdownNativeContinuousProfilerBestEffort(
                false,
                () =>
                {
                    calls++;
                    throw new InvalidOperationException("The non-owner must not invoke native shutdown.");
                }));

        Assert.Null(exception);
        Assert.Equal(0, calls);
    }

    [Fact]
    public void BestEffortNativeProfilerShutdownDoesNotPropagateNativeException()
    {
        var exception = Record.Exception(
            () => Instrumentation.ShutdownNativeContinuousProfilerBestEffort(
                true,
                () => throw new DllNotFoundException("Test native shutdown failure.")));

        Assert.Null(exception);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
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

    private static Dictionary<SampleType, (Action<byte[], int, uint, CancellationToken> Handler, TimeSpan ExportTimeout)> CreateHandlers()
    {
        return new Dictionary<SampleType, (Action<byte[], int, uint, CancellationToken> Handler, TimeSpan ExportTimeout)>
        {
            [SampleType.Continuous] = ((_, _, _, _) => { }, ExportTimeout)
        };
    }
}
