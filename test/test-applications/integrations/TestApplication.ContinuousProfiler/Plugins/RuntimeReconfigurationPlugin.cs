// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.PluginApi.ContinuousProfiling;
using OpenTelemetry.AutoInstrumentation.PluginApi.SelectiveSampling;
using TestApplication.ContinuousProfiler.Plugins;

namespace TestApplication.ContinuousProfiler;

#pragma warning disable CA1515 // Consider making public types internal. Needed for AutoInstrumentation plugin loading.
public class RuntimeReconfigurationPlugin : BasePlugin, IContinuousProfilerPlugin, ISelectiveSamplerPlugin
#pragma warning restore CA1515 // Consider making public types internal. Needed for AutoInstrumentation plugin loading.
{
    private const uint InitialThreadSamplingInterval = 1000u;
    private const uint InitialMaxMemorySamplesPerMinute = 60000u;
    private const uint InitialSnapshotSamplingInterval = 100u;
    private static readonly TimeSpan ExportInterval = TimeSpan.FromMilliseconds(100);
    private static readonly TimeSpan ExportTimeout = TimeSpan.FromSeconds(5);
    private static readonly NoopExporter Exporter = new();

    public ContinuousProfilerConfiguration GetFirstContinuousProfilerConfiguration()
    {
        return new ContinuousProfilerConfiguration
        {
            ThreadSamplingEnabled = false,
            ThreadSamplingInterval = InitialThreadSamplingInterval,
            AllocationSamplingEnabled = false,
            MaxMemorySamplesPerMinute = InitialMaxMemorySamplesPerMinute,
            ExportInterval = ExportInterval,
            ExportTimeout = ExportTimeout,
            Exporter = Exporter
        };
    }

    public SelectiveSamplerConfiguration GetFirstSelectiveSamplingConfiguration()
    {
        return new SelectiveSamplerConfiguration
        {
            Enabled = false,
            SamplingInterval = InitialSnapshotSamplingInterval,
            ExportInterval = ExportInterval,
            ExportTimeout = ExportTimeout,
            Exporter = Exporter
        };
    }

    private sealed class NoopExporter : IContinuousProfilerExporter, ISelectiveSamplerExporter
    {
        public void ExportThreadSamples(byte[] buffer, int read, uint samplingInterval, CancellationToken cancellationToken)
        {
        }

        public void ExportAllocationSamples(byte[] buffer, int read, CancellationToken cancellationToken)
        {
        }

        public void ExportSelectedThreadSamples(byte[] buffer, int read, CancellationToken cancellationToken)
        {
        }
    }
}
