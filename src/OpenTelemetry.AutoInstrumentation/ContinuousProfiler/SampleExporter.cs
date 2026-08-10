// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
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
    private readonly object _lifecycleLock = new();

    // Additional async local required to get full set of notifications,
    // see https://github.com/dotnet/runtime/issues/67276#issuecomment-1089877762
    private AsyncLocal<Activity?>? _supportingActivityAsyncLocal;
    private Thread? _thread;
    private bool _activityTrackingEnabled;
    private bool _disposed;
    private bool _started;

    public SampleExporter(BufferProcessor bufferProcessor, TimeSpan exportInterval, TimeSpan exportTimeout)
    {
#if NET
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(exportInterval, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(exportTimeout, TimeSpan.Zero);
#else
        if (exportInterval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(exportInterval));
        }

        if (exportTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(exportTimeout));
        }
#endif
        _exportInterval = exportInterval;
        _exportTimeout = exportTimeout;
        _bufferProcessor = bufferProcessor;
    }

    public void Start()
    {
        lock (_lifecycleLock)
        {
#if NET
            ObjectDisposedException.ThrowIf(_disposed, this);
#else
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(SampleExporter));
            }
#endif

            if (_started)
            {
                return;
            }

            Logger.Debug("Initializing Continuous Profiler export thread.");

            try
            {
                _supportingActivityAsyncLocal = new AsyncLocal<Activity?>(ActivityChanged);
                Activity.CurrentChanged += Activity_CurrentChanged;
                _activityTrackingEnabled = true;
                _supportingActivityAsyncLocal.Value = Activity.Current;

                _thread = new Thread(SampleReadingThread)
                {
                    Name = BackgroundThreadName,
                    IsBackground = true
                };
                _thread.Start();
                _started = true;
            }
            catch
            {
                if (_activityTrackingEnabled)
                {
                    Activity.CurrentChanged -= Activity_CurrentChanged;
                    _activityTrackingEnabled = false;
                }

                _supportingActivityAsyncLocal = null;
                _thread = null;
                throw;
            }
        }
    }

    public void Dispose()
    {
        Thread? thread;
        lock (_lifecycleLock)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            if (_activityTrackingEnabled)
            {
                Activity.CurrentChanged -= Activity_CurrentChanged;
                _activityTrackingEnabled = false;
            }

            _supportingActivityAsyncLocal = null;
            _shutdownTrigger.Set();
            thread = _thread;
        }

        var configuredGracePeriod = 2 * _exportTimeout.TotalMilliseconds;
        var finalGracePeriod = (int)Math.Min(configuredGracePeriod, 60000);
        if (thread != null && !thread.Join(finalGracePeriod))
        {
            Logger.Warning("Continuous profiler's exporter thread failed to terminate in required time.");
            return;
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
            NativeMethods.ContinuousProfilerNotifySpanStopped(sender.PreviousValue!);
        }

        if (currentActivity != null)
        {
            NativeMethods.ContinuousProfilerSetNativeContext(currentActivity);
            return;
        }

        NativeMethods.ContinuousProfilerResetNativeContext();
    }

    private void Activity_CurrentChanged(object? sender, ActivityChangedEventArgs e)
    {
        var supportingActivityAsyncLocal = _supportingActivityAsyncLocal;
        if (supportingActivityAsyncLocal != null)
        {
            supportingActivityAsyncLocal.Value = e.Current;
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
