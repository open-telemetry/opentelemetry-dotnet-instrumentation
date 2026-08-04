// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.PluginApi.ContinuousProfiling;
using TestApplication.ContinuousProfiler.Plugins;

namespace TestApplication.ContinuousProfiler;

#pragma warning disable CA1515 // Consider making public types internal. Needed for AutoInstrumentation plugin loading.
public class RuntimeReconfigurationPlugin : BasePlugin, IContinuousProfilerPlugin
#pragma warning restore CA1515 // Consider making public types internal. Needed for AutoInstrumentation plugin loading.
{
    private const uint InitialThreadSamplingInterval = 1000u;
    private static int _threadExportCount;
    private static uint _lastThreadSamplingInterval;

    public ContinuousProfilerConfiguration GetFirstContinuousProfilerConfiguration()
    {
        return new ContinuousProfilerConfiguration
        {
            ThreadSamplingEnabled = false,
            ThreadSamplingInterval = InitialThreadSamplingInterval,
            AllocationSamplingEnabled = false,
            ExportInterval = TimeSpan.FromMilliseconds(100),
            ExportTimeout = TimeSpan.FromSeconds(5),
            Exporter = new CountingExporter()
        };
    }

    internal static int GetThreadExportCount() => Volatile.Read(ref _threadExportCount);

    internal static uint GetLastThreadSamplingInterval() => Volatile.Read(ref _lastThreadSamplingInterval);

    private sealed class CountingExporter : IContinuousProfilerExporter
    {
        public void ExportThreadSamples(byte[] buffer, int read, uint samplingInterval, CancellationToken cancellationToken)
        {
            Volatile.Write(ref _lastThreadSamplingInterval, samplingInterval);
            Interlocked.Increment(ref _threadExportCount);
        }

        public void ExportAllocationSamples(byte[] buffer, int read, CancellationToken cancellationToken)
        {
        }
    }
}
