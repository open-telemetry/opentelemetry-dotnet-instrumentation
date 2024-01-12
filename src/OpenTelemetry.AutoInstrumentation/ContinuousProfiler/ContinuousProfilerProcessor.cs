// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET6_0_OR_GREATER

using System.Globalization;
using System.Reflection;
using OpenTelemetry.AutoInstrumentation.Logging;

namespace OpenTelemetry.AutoInstrumentation.ContinuousProfiler;

internal static class ContinuousProfilerProcessor
{
    public const string BackgroundThreadName = "OpenTelemetry Continuous Profiler Thread";

    private static readonly IOtelLogger Logger = OtelLogging.GetLogger();

    public static void Activity_CurrentChanged(object? sender, System.Diagnostics.ActivityChangedEventArgs e)
    {
        var currentActivity = e.Current;
        var managedThreadId = Environment.CurrentManagedThreadId;

        if (currentActivity != null)
        {
            var hexTraceId = currentActivity.TraceId.ToHexString();

            if (ulong.TryParse(hexTraceId.AsSpan(0, 16), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var traceIdHigh) &&
                ulong.TryParse(hexTraceId.AsSpan(16), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var traceIdLow) &&
                ulong.TryParse(currentActivity.SpanId.ToHexString(), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var spanId))
            {
                NativeMethods.ContinuousProfilerSetNativeContext(traceIdHigh, traceIdLow, spanId, managedThreadId);
                return;
            }
        }

        NativeMethods.ContinuousProfilerSetNativeContext(0, 0, 0, managedThreadId);
    }

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

        var threadSamplesMethod = exportThreadSamplesMethod.CreateDelegate<Action<byte[], int>>(continuousProfilerExporter);
        var allocationSamplesMethod = exportAllocationSamplesMethod.CreateDelegate<Action<byte[], int>>(continuousProfilerExporter);

        // TODO Graceful shutdown and Task.Delay https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/issues/3216
        var thread = new Thread(() =>
        {
            SampleReadingThread(new BufferProcessor(threadSamplingEnabled, allocationSamplingEnabled, threadSamplesMethod, allocationSamplesMethod), exportInterval);
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
            // TODO Graceful shutdown and Task.Delay https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/issues/3216
            Thread.Sleep(exportInterval);
            sampleExporter.Process();
        }
    }
}
#endif
