// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET6_0_OR_GREATER

using System.Diagnostics;
using System.Globalization;
using OpenTelemetry.AutoInstrumentation.Logging;

namespace OpenTelemetry.AutoInstrumentation.ContinuousProfiler;

internal static class ContinuousProfilerProcessor
{
    public const string BackgroundThreadName = "OpenTelemetry Continuous Profiler Thread";

    private static readonly IOtelLogger Logger = OtelLogging.GetLogger();
    // Additional async local required to get full set of notifications,
    // see https://github.com/dotnet/runtime/issues/67276#issuecomment-1089877762
    private static AsyncLocal<Activity?>? _supportingActivityAsyncLocal;

    public static void Activity_CurrentChanged(object? sender, ActivityChangedEventArgs e)
    {
        if (_supportingActivityAsyncLocal != null)
        {
            _supportingActivityAsyncLocal.Value = e.Current;
        }
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

        _supportingActivityAsyncLocal = new AsyncLocal<Activity?>(ActivityChanged);
        // TODO Graceful shutdown and Task.Delay https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/issues/3216
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

    private static void ActivityChanged(AsyncLocalValueChangedArgs<Activity?> sender)
    {
        var currentActivity = sender.CurrentValue;
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
