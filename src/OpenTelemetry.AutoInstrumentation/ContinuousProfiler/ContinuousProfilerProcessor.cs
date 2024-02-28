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

    public ContinuousProfilerProcessor(bool threadSamplingEnabled, bool allocationSamplingEnabled, Action<byte[], int> threadSamplesMethod, Action<byte[], int> allocationSamplesMethod, TimeSpan exportInterval)
    {
        Logger.Debug("Initializing Continuous Profiler export thread.");

        _exportInterval = exportInterval;
        _supportingActivityAsyncLocal = new AsyncLocal<Activity?>(ActivityChanged);

        _thread = new Thread(() =>
        {
            SampleReadingThread(new BufferProcessor(threadSamplingEnabled, allocationSamplingEnabled, threadSamplesMethod, allocationSamplesMethod));
        })
        {
            Name = BackgroundThreadName,
            IsBackground = true
        };
        _thread.Start();

        Logger.Information("Continuous Profiler export thread initialized.");
    }

    public void Activity_CurrentChanged(object? sender, ActivityChangedEventArgs e)
    {
        _supportingActivityAsyncLocal.Value = e.Current;
    }

    public void Dispose()
    {
        _shutdownTrigger.Set();
        if (!_thread.Join(_exportInterval))
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

    private void SampleReadingThread(BufferProcessor sampleExporter)
    {
        while (true)
        {
            // TODO Task.Delay https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/issues/3216
            if (_shutdownTrigger.Wait(_exportInterval))
            {
                Logger.Debug("Shutdown requested, exiting continuous profiler's exporter thread.");
                return;
            }

            sampleExporter.Process();
        }
    }
}
#endif
