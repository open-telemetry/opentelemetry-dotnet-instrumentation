// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.ContinuousProfiler;

namespace OpenTelemetry.AutoInstrumentation.Tests.ContinuousProfiler;

public class BufferProcessorTests
{
    private static readonly TimeSpan ExportTimeout = TimeSpan.FromSeconds(1);

    [Fact]
    public void ForwardsSamplingIntervalFromNativeBatch()
    {
        uint? observedSamplingInterval = null;
        var handlers = new Dictionary<SampleType, (Action<byte[], int, uint, CancellationToken> Handler, TimeSpan ExportTimeout)>
        {
            [SampleType.Continuous] = ((_, _, samplingInterval, _) => observedSamplingInterval = samplingInterval, ExportTimeout)
        };
        var processor = new BufferProcessor(handlers, (_, _) => (1, 123u));

        processor.Process();

        Assert.Equal(123u, observedSamplingInterval);
    }

    [Fact]
    public void IsolatesReadFailureAndProcessesOtherSampleTypes()
    {
        var selectedThreadsProcessed = false;
        var handlers = new Dictionary<SampleType, (Action<byte[], int, uint, CancellationToken> Handler, TimeSpan ExportTimeout)>
        {
            [SampleType.Continuous] = ((_, _, _, _) => { }, ExportTimeout),
            [SampleType.SelectedThreads] = ((_, _, _, _) => selectedThreadsProcessed = true, ExportTimeout)
        };
        var processor = new BufferProcessor(
            handlers,
            (sampleType, _) => sampleType == SampleType.Continuous
                ? throw new DllNotFoundException("Test native read failure.")
                : (1, 0u));

        var exception = Record.Exception(processor.Process);

        Assert.Null(exception);
        Assert.True(selectedThreadsProcessed);
    }
}
