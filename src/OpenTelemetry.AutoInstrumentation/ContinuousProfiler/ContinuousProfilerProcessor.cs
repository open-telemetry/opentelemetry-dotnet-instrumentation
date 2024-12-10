// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET

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
    private readonly TimeSpan _exportInterval;
    private readonly TimeSpan _exportTimeout;
    private readonly BufferProcessor _bufferProcessor;
    private readonly ManualResetEventSlim _shutdownTrigger = new(false);

    public ContinuousProfilerProcessor(BufferProcessor bufferProcessor, TimeSpan exportInterval, TimeSpan exportTimeout)
    {
        Logger.Debug("Initializing Continuous Profiler export thread.");

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
        var configuredGracePeriod = 2 * _exportTimeout;
        var finalGracePeriod = (int)Math.Min(configuredGracePeriod.TotalMilliseconds, 60000);
        _shutdownTrigger.Set();
        if (!_thread.Join(finalGracePeriod))
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
