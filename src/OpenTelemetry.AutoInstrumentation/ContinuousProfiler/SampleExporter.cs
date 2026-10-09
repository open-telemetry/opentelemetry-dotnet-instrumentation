// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using OpenTelemetry.AutoInstrumentation.Logging;

namespace OpenTelemetry.AutoInstrumentation.ContinuousProfiler;

internal class SampleExporter : IDisposable
{
    private const string BackgroundThreadName = "OpenTelemetry Continuous Profiler Thread";

    private static readonly IOtelLogger Logger = OtelLogging.GetLogger();

    private readonly TimeSpan _exportTimeout;
    private readonly (TimeSpan Interval, Action Process)[] _exportSchedules;
    private readonly ManualResetEventSlim _shutdownTrigger = new(false);
    private readonly ManualResetEventSlim _activationTrigger = new(false);
    private readonly ManualResetEventSlim _readerThreadStarted = new(false);
    private readonly object _lifecycleLock = new();
    // An additional AsyncLocal is required to receive the full set of Activity notifications.
    // See https://github.com/dotnet/runtime/issues/67276#issuecomment-1089877762.
    private AsyncLocal<Activity?>? _supportingActivityAsyncLocal;
    private Func<bool>? _deferredInitializer;
    private TimeSpan _deferredInitializerRetryInterval;
    private Thread? _thread;
#if NETFRAMEWORK
    private Mutex? _readerMutex;
    private Func<bool>? _canRead;
#endif
    private bool _disposed;
    private bool _started;

    public SampleExporter(BufferProcessor bufferProcessor, TimeSpan exportInterval, TimeSpan exportTimeout)
        : this([(exportInterval, bufferProcessor.Process)], exportTimeout)
    {
    }

    internal SampleExporter(BufferProcessor bufferProcessor, IReadOnlyDictionary<SampleType, TimeSpan> exportIntervals, TimeSpan exportTimeout)
        : this(CreateExportSchedules(bufferProcessor, exportIntervals), exportTimeout)
    {
    }

    private SampleExporter((TimeSpan Interval, Action Process)[] exportSchedules, TimeSpan exportTimeout)
    {
#if NET
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(exportTimeout, TimeSpan.Zero);
#else
        if (exportTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(exportTimeout));
        }
#endif

        if (exportSchedules.Length == 0)
        {
            throw new ArgumentException("At least one export handler must be configured.", nameof(exportSchedules));
        }

        foreach (var schedule in exportSchedules)
        {
            if (schedule.Interval <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(exportSchedules), "Export intervals must be positive.");
            }
        }

        _exportTimeout = exportTimeout;
        _exportSchedules = exportSchedules;
    }

    public void Start()
    {
        StartCore(null, TimeSpan.Zero);
    }

    public void Activate()
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

            if (!_started)
            {
                throw new InvalidOperationException("The continuous profiler exporter must be started before it is activated.");
            }

            if (_activationTrigger.IsSet)
            {
                return;
            }

            _supportingActivityAsyncLocal = new AsyncLocal<Activity?>(ActivityChanged);
            Activity.CurrentChanged += Activity_CurrentChanged;
            _supportingActivityAsyncLocal.Value = Activity.Current;
            _activationTrigger.Set();
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
            Activity.CurrentChanged -= Activity_CurrentChanged;
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

        _activationTrigger.Dispose();
        _readerThreadStarted.Dispose();
        _shutdownTrigger.Dispose();
#if NETFRAMEWORK
        _readerMutex?.Dispose();
#endif
    }

    internal void Start(Func<bool> deferredInitializer, TimeSpan retryInterval)
    {
#if NET
        ArgumentNullException.ThrowIfNull(deferredInitializer);
#else
        if (deferredInitializer == null)
        {
            throw new ArgumentNullException(nameof(deferredInitializer));
        }
#endif
#if NET
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(retryInterval, TimeSpan.Zero);
#else
        if (retryInterval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(retryInterval));
        }
#endif

        StartCore(deferredInitializer, retryInterval);
    }

#if NETFRAMEWORK
    internal void Start(Func<bool> deferredInitializer, TimeSpan retryInterval, string readerMutexName, Func<bool>? canRead = null)
    {
        if (string.IsNullOrWhiteSpace(readerMutexName))
        {
            throw new ArgumentException("A reader mutex name is required.", nameof(readerMutexName));
        }

        if (deferredInitializer == null)
        {
            throw new ArgumentNullException(nameof(deferredInitializer));
        }

        if (retryInterval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(retryInterval));
        }

        StartCore(deferredInitializer, retryInterval, readerMutexName, canRead);
    }
#endif

    internal bool WaitForReaderThreadStart(TimeSpan timeout) => _readerThreadStarted.Wait(timeout);

    private static (TimeSpan Interval, Action Process)[] CreateExportSchedules(
        BufferProcessor bufferProcessor,
        IReadOnlyDictionary<SampleType, TimeSpan> exportIntervals)
    {
        var schedules = new (TimeSpan Interval, Action Process)[exportIntervals.Count];
        var index = 0;
        foreach (var entry in exportIntervals)
        {
            var sampleType = entry.Key;
            schedules[index++] = (entry.Value, () => bufferProcessor.Process(sampleType));
        }

        return schedules;
    }

    private static int GetRemainingWaitMilliseconds(double intervalMilliseconds, double elapsedMilliseconds)
    {
        var remainingMilliseconds = intervalMilliseconds - elapsedMilliseconds;
        return remainingMilliseconds <= 0
            ? 0
            : (int)Math.Min(Math.Ceiling(remainingMilliseconds), int.MaxValue);
    }

    private static bool TryRunDeferredInitializer(Func<bool> deferredInitializer)
    {
        try
        {
            return deferredInitializer();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Continuous profiler deferred initialization failed and will be retried.");
            return false;
        }
    }

    private static void ActivityChanged(AsyncLocalValueChangedArgs<Activity?> sender)
    {
        var currentActivity = sender.CurrentValue;
        if (sender is { ThreadContextChanged: false, PreviousValue.IsStopped: true } && sender.CurrentValue == sender.PreviousValue?.Parent)
        {
            NativeMethods.ContinuousProfilerNotifySpanStopped(sender.PreviousValue!);
        }

        if (currentActivity != null)
        {
            NativeMethods.ContinuousProfilerSetNativeContext(currentActivity);
        }
        else
        {
            NativeMethods.ContinuousProfilerResetNativeContext();
        }
    }

#if NETFRAMEWORK
    private bool TryCanRead()
    {
        try
        {
            return _canRead?.Invoke() ?? true;
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "Failed to check the continuous profiler reader's native configuration.");
            return false;
        }
    }
#endif

    private void Activity_CurrentChanged(object? sender, ActivityChangedEventArgs e)
    {
        if (_supportingActivityAsyncLocal != null)
        {
            _supportingActivityAsyncLocal.Value = e.Current;
        }
    }

#if NETFRAMEWORK
    private void StartCore(Func<bool>? deferredInitializer, TimeSpan retryInterval, string? readerMutexName = null, Func<bool>? canRead = null)
#else
    private void StartCore(Func<bool>? deferredInitializer, TimeSpan retryInterval)
#endif
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

            _deferredInitializer = deferredInitializer;
            _deferredInitializerRetryInterval = retryInterval;
            try
            {
#if NETFRAMEWORK
                _readerMutex = readerMutexName == null ? null : new Mutex(false, readerMutexName);
                _canRead = canRead;
#endif
                Logger.Debug("Initializing Continuous Profiler export thread.");
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
#if NETFRAMEWORK
                _readerMutex?.Dispose();
                _readerMutex = null;
                _canRead = null;
#endif
                _deferredInitializer = null;
                _deferredInitializerRetryInterval = TimeSpan.Zero;
                _thread = null;
                throw;
            }
        }
    }

    private void SampleReadingThread()
    {
        Logger.Information("Continuous Profiler export thread initialized.");
        _readerThreadStarted.Set();
        if (WaitHandle.WaitAny([_shutdownTrigger.WaitHandle, _activationTrigger.WaitHandle]) == 0)
        {
            return;
        }

#if NETFRAMEWORK
        var readerMutex = _readerMutex;
        if (readerMutex == null)
        {
            RunExportLoop();
            return;
        }

        var retryMilliseconds = GetRemainingWaitMilliseconds(_deferredInitializerRetryInterval.TotalMilliseconds, 0);
        while (!_shutdownTrigger.IsSet)
        {
            if (!TryCanRead())
            {
                _shutdownTrigger.Wait(retryMilliseconds);
                continue;
            }

            bool ownsReaderMutex;
            try
            {
                ownsReaderMutex = readerMutex.WaitOne(0);
            }
            catch (AbandonedMutexException)
            {
                // The previous AppDomain's reader exited without releasing its mutex.
                ownsReaderMutex = true;
            }

            if (!ownsReaderMutex)
            {
                _shutdownTrigger.Wait(retryMilliseconds);
                continue;
            }

            try
            {
                if (!_shutdownTrigger.IsSet && TryCanRead())
                {
                    RunExportLoop();
                }
            }
            finally
            {
                readerMutex.ReleaseMutex();
            }
        }
#else
        RunExportLoop();
#endif
    }

    private void RunExportLoop()
    {
        var exportStopwatches = new Stopwatch[_exportSchedules.Length];
        for (var i = 0; i < exportStopwatches.Length; i++)
        {
            exportStopwatches[i] = Stopwatch.StartNew();
        }

        var deferredInitializer = _deferredInitializer;
        Stopwatch? deferredInitializerStopwatch = null;
        if (deferredInitializer != null && !TryRunDeferredInitializer(deferredInitializer))
        {
            deferredInitializerStopwatch = Stopwatch.StartNew();
        }
        else
        {
            deferredInitializer = null;
        }

#if NETFRAMEWORK
        var readerEligibilityStopwatch = _readerMutex == null ? null : Stopwatch.StartNew();
#endif

        while (true)
        {
            var remainingWait = int.MaxValue;
            for (var i = 0; i < _exportSchedules.Length; i++)
            {
                remainingWait = Math.Min(
                    remainingWait,
                    GetRemainingWaitMilliseconds(_exportSchedules[i].Interval.TotalMilliseconds, exportStopwatches[i].Elapsed.TotalMilliseconds));
            }

            if (deferredInitializer != null)
            {
                remainingWait = Math.Min(
                    remainingWait,
                    GetRemainingWaitMilliseconds(_deferredInitializerRetryInterval.TotalMilliseconds, deferredInitializerStopwatch!.Elapsed.TotalMilliseconds));
            }

#if NETFRAMEWORK
            if (readerEligibilityStopwatch != null)
            {
                remainingWait = Math.Min(
                    remainingWait,
                    GetRemainingWaitMilliseconds(_deferredInitializerRetryInterval.TotalMilliseconds, readerEligibilityStopwatch.Elapsed.TotalMilliseconds));
            }
#endif

            if (_shutdownTrigger.Wait(remainingWait))
            {
                return;
            }

#if NETFRAMEWORK
            if (readerEligibilityStopwatch != null && readerEligibilityStopwatch.Elapsed >= _deferredInitializerRetryInterval)
            {
                readerEligibilityStopwatch.Restart();
                if (!TryCanRead())
                {
                    return;
                }
            }
#endif

            if (deferredInitializer != null && deferredInitializerStopwatch!.Elapsed >= _deferredInitializerRetryInterval)
            {
                if (TryRunDeferredInitializer(deferredInitializer))
                {
                    deferredInitializer = null;
                    deferredInitializerStopwatch = null;
                }
                else
                {
                    deferredInitializerStopwatch.Restart();
                }
            }

            for (var i = 0; i < _exportSchedules.Length; i++)
            {
                if (exportStopwatches[i].Elapsed >= _exportSchedules[i].Interval)
                {
                    exportStopwatches[i].Restart();
                    _exportSchedules[i].Process();
                }
            }
        }
    }
}
