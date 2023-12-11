// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET6_0_OR_GREATER

using OpenTelemetry.AutoInstrumentation.Logging;

namespace OpenTelemetry.AutoInstrumentation.ContinuousProfiler;

internal static class ContinuousProfilerProcessor
{
    public const string BackgroundThreadName = "OpenTelemetry Continuous Profiler Thread";

    private static readonly IOtelLogger Logger = OtelLogging.GetLogger();

    public static void Initialize(bool threadSamplingEnabled, bool allocationSamplingEnabled, TimeSpan exportInterval, object continuousProfilerExporter)
    {
        Logger.Debug("Initializing Continuous Profiler export thread.");

        var continuousProfilerExporterType = continuousProfilerExporter.GetType();
        var exportThreadSamplesMethod = continuousProfilerExporterType.GetMethod("ExportThreadSamples");

        if (exportThreadSamplesMethod == null)
        {
            Logger.Warning("Exporter does not have ExportThreadSamples method. Continuous Profiler initialization failed.");
            return;
        }

        var exportAllocationSamplesMethod = continuousProfilerExporterType.GetMethod("ExportAllocationSamples");
        if (exportAllocationSamplesMethod == null)
        {
            Logger.Warning("Exporter does not have ExportAllocationSamples method. Continuous Profiler initialization failed.");
            return;
        }

        var thread = new Thread(() =>
        {
            SampleReadingThread(new BufferProcessor(threadSamplingEnabled, allocationSamplingEnabled, continuousProfilerExporter, exportThreadSamplesMethod, exportAllocationSamplesMethod), exportInterval);
        })
        {
            Name = BackgroundThreadName,
            IsBackground = true
        };
        thread.Start();

        Logger.Information("Continuous Profiler export thread initialized.");
    }

    private static void SampleReadingThread(BufferProcessor sampleExporter, TimeSpan exportInterval)
    {
        while (true)
        {
            Thread.Sleep(exportInterval);
            sampleExporter.Process();
        }
    }
}
#endif
