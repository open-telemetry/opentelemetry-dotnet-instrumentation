// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET

using System.Diagnostics;
using System.Globalization;
using OpenTelemetry.AutoInstrumentation.Logging;

namespace OpenTelemetry.AutoInstrumentation.ContinuousProfiler;

internal class SampleExporter : IDisposable
{
    private const string BackgroundThreadName = "OpenTelemetry Continuous Profiler Thread";

    private static readonly IOtelLogger Logger = OtelLogging.GetLogger();

    private readonly TimeSpan _exportInterval;
    private readonly TimeSpan _exportTimeout;
    private readonly BufferProcessor _bufferProcessor;
    private readonly ManualResetEventSlim _shutdownTrigger = new(false);
    // Additional async local required to get full set of notifications,
    // see https://github.com/dotnet/runtime/issues/67276#issuecomment-1089877762
    private readonly AsyncLocal<Activity?>? _supportingActivityAsyncLocal;
    private readonly Thread? _thread;

    public SampleExporter(BufferProcessor bufferProcessor, TimeSpan exportInterval, TimeSpan exportTimeout)
    {
        if (exportInterval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(exportInterval));
        }

        _exportInterval = exportInterval;
        if (exportTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(exportTimeout));
        }

        _exportTimeout = exportTimeout;
        _bufferProcessor = bufferProcessor;

        _supportingActivityAsyncLocal = new AsyncLocal<Activity?>(ActivityChanged);
        Activity.CurrentChanged += Activity_CurrentChanged;

        Logger.Debug("Initializing Continuous Profiler export thread.");

        _thread = new Thread(SampleReadingThread)
        {
            Name = BackgroundThreadName,
            IsBackground = true
        };
        _thread.Start();
    }

    public void Dispose()
    {
        Activity.CurrentChanged -= Activity_CurrentChanged;

        var configuredGracePeriod = 2 * _exportTimeout;
        var finalGracePeriod = (int)Math.Min(configuredGracePeriod.TotalMilliseconds, 60000);
        _shutdownTrigger.Set();
        if (_thread != null && !_thread.Join(finalGracePeriod))
        {
            Logger.Warning("Continuous profiler's exporter thread failed to terminate in required time.");
        }

        _shutdownTrigger.Dispose();
    }

    private static void ActivityChanged(AsyncLocalValueChangedArgs<Activity?> sender)
    {
        var currentActivity = sender.CurrentValue;

        // Identify activity stoppage
        // Stop() stops the activity and sets Activity.Current to parent
        if (sender is { ThreadContextChanged: false, PreviousValue.IsStopped: true } && sender.CurrentValue == sender.PreviousValue?.Parent)
        {
            if (TryParseSpanContext(sender.PreviousValue!, out var traceIdHigh, out var traceIdLow, out var spanId))
            {
                NativeMethods.ContinuousProfilerNotifySpanStopped(traceIdHigh, traceIdLow, spanId);
            }
        }

        if (currentActivity != null)
        {
            if (TryParseSpanContext(currentActivity, out var traceIdHigh, out var traceIdLow, out var spanId))
            {
                NativeMethods.ContinuousProfilerSetNativeContext(traceIdHigh, traceIdLow, spanId);
                return;
            }
        }

        NativeMethods.ContinuousProfilerSetNativeContext(0, 0, 0);
    }

    private static bool TryParseSpanContext(Activity currentActivity, out ulong traceIdHigh, out ulong traceIdLow, out ulong spanId)
    {
        traceIdLow = 0;
        traceIdHigh = 0;
        spanId = 0;
        var hexTraceId = currentActivity.TraceId.ToHexString();

        return ulong.TryParse(hexTraceId.AsSpan(0, 16), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out traceIdHigh) &&
               ulong.TryParse(hexTraceId.AsSpan(16), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out traceIdLow) &&
               ulong.TryParse(currentActivity.SpanId.ToHexString(), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out spanId);
    }

    private void Activity_CurrentChanged(object? sender, ActivityChangedEventArgs e)
    {
        if (_supportingActivityAsyncLocal != null)
        {
            _supportingActivityAsyncLocal.Value = e.Current;
        }
    }

    private void SampleReadingThread()
    {
        Logger.Information("Continuous Profiler export thread initialized.");

        var sw = new Stopwatch();
        var exportIntervalMilliseconds = _exportInterval.TotalMilliseconds;

        while (true)
        {
            var elapsed = sw.ElapsedMilliseconds;
            var remainingWaitTime = elapsed >= exportIntervalMilliseconds ? 0 : exportIntervalMilliseconds - elapsed;
            if (_shutdownTrigger.Wait((int)remainingWaitTime))
            {
                Logger.Debug("Shutdown requested, exiting continuous profiler's exporter thread.");
                return;
            }

            sw.Restart();
            _bufferProcessor.Process();
        }
    }
}
#endif
