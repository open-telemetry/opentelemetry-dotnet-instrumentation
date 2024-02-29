// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET6_0_OR_GREATER

using System.Diagnostics;
using System.Globalization;
using OpenTelemetry.AutoInstrumentation.Logging;

namespace OpenTelemetry.AutoInstrumentation.ContinuousProfiler;

internal class ContinuousProfilerProcessor : IDisposable
{
    private const string BackgroundThreadName = "OpenTelemetry Continuous Profiler Thread";

    private static readonly IOtelLogger Logger = OtelLogging.GetLogger();

    // Additional async local required to get full set of notifications,
    // see https://github.com/dotnet/runtime/issues/67276#issuecomment-1089877762
    private readonly AsyncLocal<Activity?> _supportingActivityAsyncLocal;
    private readonly Thread _thread;
    private readonly ManualResetEventSlim _shutdownTrigger = new(false);
    private readonly TimeSpan _exportInterval;
    private readonly BufferProcessor _bufferProcessor;

    public ContinuousProfilerProcessor(BufferProcessor bufferProcessor, TimeSpan exportInterval)
    {
        Logger.Debug("Initializing Continuous Profiler export thread.");

        _exportInterval = exportInterval;
        _bufferProcessor = bufferProcessor;
        _supportingActivityAsyncLocal = new AsyncLocal<Activity?>(ActivityChanged);

        _thread = new Thread(SampleReadingThread)
        {
            Name = BackgroundThreadName,
            IsBackground = true
        };
        _thread.Start();
    }

    public void Activity_CurrentChanged(object? sender, ActivityChangedEventArgs e)
    {
        _supportingActivityAsyncLocal.Value = e.Current;
    }

    public void Dispose()
    {
        _shutdownTrigger.Set();
        // Wait 5s for exporter thread to terminate, similarly to https://github.com/open-telemetry/opentelemetry-dotnet/blob/77ef12327f720ca3defd0c9590c0197cceb5952e/src/OpenTelemetry/Trace/TracerProviderSdk.cs#L383
        if (!_thread.Join(TimeSpan.FromSeconds(5)))
        {
            Logger.Warning("Continuous profiler's exporter thread failed to terminate in required time.");
        }

        _shutdownTrigger.Dispose();
    }

    private static void ActivityChanged(AsyncLocalValueChangedArgs<Activity?> sender)
    {
        var currentActivity = sender.CurrentValue;

        if (currentActivity != null)
        {
            var hexTraceId = currentActivity.TraceId.ToHexString();

            if (ulong.TryParse(hexTraceId.AsSpan(0, 16), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var traceIdHigh) &&
                ulong.TryParse(hexTraceId.AsSpan(16), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var traceIdLow) &&
                ulong.TryParse(currentActivity.SpanId.ToHexString(), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var spanId))
            {
                NativeMethods.ContinuousProfilerSetNativeContext(traceIdHigh, traceIdLow, spanId);
                return;
            }
        }

        NativeMethods.ContinuousProfilerSetNativeContext(0, 0, 0);
    }

    private void SampleReadingThread()
    {
        Logger.Information("Continuous Profiler export thread initialized.");

        while (true)
        {
            // TODO Task.Delay https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/issues/3216
            if (_shutdownTrigger.Wait(_exportInterval))
            {
                Logger.Debug("Shutdown requested, exiting continuous profiler's exporter thread.");
                return;
            }

            _bufferProcessor.Process();
        }
    }
}
#endif
