using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Datadog.Collections;
using Datadog.Util;
using OpenTelemetry.AutoInstrumentation.ActivityExporter;
using OpenTelemetry.DynamicActivityBinding;

#pragma warning disable SA1124 // Do not use regions
#pragma warning disable SA1201 // Elements must appear in the correct order
#pragma warning disable SA1214 // Readonly fields must appear before non-readonly fields

namespace OpenTelemetry.AutoInstrumentation.ActivityCollector
{
    internal class CollectAndExportBackgroundLoop : IDisposable
    {
        private static class State
        {
            public const int NotStarted = 1;
            public const int Running = 2;
            public const int ShutdownRequested = 3;
            public const int ShutDown = 4;
            public const int Disposed = 4;
        }

        private const int CompletedItemsCollectionSegmentSize = 64;

        private const int MoveTowardsDefaultBatchSizeAfterAge = 100;
        private const double MoveTowardsDefaultBatchSizeStep = 0.1;

        private static readonly TimeSpan WaitIntervalMin = TimeSpan.FromMilliseconds(1);
        private static readonly TimeSpan WaitIntervalMax = TimeSpan.FromSeconds(10);

        private readonly TraceCache _activeTraces;
        private GrowingCollection<LocalTrace> _completedTraces;
        private GrowingCollection<ActivityStub> _completedActivities;

        private readonly TimeSpan _exportInterval;
        private readonly int _exportBatchSizeCapDefault;
        private int _exportBatchSizeCapCurrent;
        private int _exportBatchSizeCapHintAge = 0;

        private readonly bool _aggregateActivitiesIntoTraces;
        private readonly IActivityExporter _activityExporter;

        private int _loopState = State.NotStarted;
        private AutoResetEvent _loopSignal = null;
        private Thread _loopThread = null;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="exportInterval"></param>
        /// <param name="exportBatchSizeCap">An exporter can provide hints to modify the specified <c>exportBatchSizeCap</c>.
        /// There may be many reasons for that, for example when the destination cannot accept data fast enough.
        /// Such hints will be accepted, but over time we will slowly retun to the originally specified <c>exportBatchSizeCap</c>.
        /// We will log when a hint occurs. If such log messages are seen frequently, one should permanently change <c>exportBatchSizeCap</c>.
        /// Therefore, it should ultimately be user-configurable.
        /// The return to the original <c>exportBatchSizeCap</c> value after an exporter hint is controlled by the
        /// constants <seealso cref="MoveTowardsDefaultBatchSizeAfterAge"/>, <seealso cref="MoveTowardsDefaultBatchSizeStep"/>.
        /// The actual cap is stored in <seealso cref="_exportBatchSizeCapDefault"/> and <seealso cref="_exportBatchSizeCapCurrent"/>.
        /// See code for the detailed logic.
        /// <br />
        /// Also note that <c>exportBatchSizeCap</c> is a SOFT CAP. When it is reached, we signal the backgroud loop to start export
        /// as soon as possible. However, additional item may be added to the completed items buffers before the export actually starts.
        /// </param>
        /// <param name="aggregateActivitiesIntoTraces"></param>
        /// <param name="activityExporter"></param>
        public CollectAndExportBackgroundLoop(TimeSpan exportInterval, int exportBatchSizeCap, bool aggregateActivitiesIntoTraces, IActivityExporter activityExporter)
        {
            Validate.NotNull(activityExporter, nameof(activityExporter));

            if (exportInterval < WaitIntervalMin)
            {
                _exportInterval = WaitIntervalMin;
            }
            else if (exportInterval > WaitIntervalMax)
            {
                _exportInterval = WaitIntervalMax;
            }
            else
            {
                _exportInterval = exportInterval;
            }

            if (exportBatchSizeCap < 1)
            {
                _exportBatchSizeCapDefault = _exportBatchSizeCapCurrent = 1;
            }
            else
            {
                _exportBatchSizeCapDefault = _exportBatchSizeCapCurrent = exportBatchSizeCap;
            }

            if (aggregateActivitiesIntoTraces)
            {
                if (!activityExporter.IsExportTracesSupported)
                {
                    throw new ArgumentException($"{nameof(aggregateActivitiesIntoTraces)} is True, but the specified {nameof(activityExporter)}"
                                              + $" of type {activityExporter.GetType().FullName} reports that" 
                                              + $" {nameof(IActivityExporter.IsExportTracesSupported)} is False.");
                }

                _aggregateActivitiesIntoTraces = true;
                _activeTraces = new TraceCache();
                _completedTraces = new GrowingCollection<LocalTrace>(CompletedItemsCollectionSegmentSize);
                _completedActivities = null;
            }
            else
            {
                if (!activityExporter.IsExportActivitiesSupported)
                {
                    throw new ArgumentException($"{nameof(aggregateActivitiesIntoTraces)} is False, but the specified {nameof(activityExporter)}"
                                              + $" of type {activityExporter.GetType().FullName} reports that"
                                              + $" {nameof(IActivityExporter.IsExportActivitiesSupported)} is False.");
                }

                _aggregateActivitiesIntoTraces = false;
                _activeTraces = null;
                _completedTraces = null;
                _completedActivities = new GrowingCollection<ActivityStub>(CompletedItemsCollectionSegmentSize);
            }

            _activityExporter = activityExporter;
        }

        public bool Start()
        {
            // We create a new dedicated thread rather than using the thread pool.
            // The collect-and-export background loop uses a dedicated tread in order
            // to prevent activity processing and activity exporting from being affected
            // by potential thread pool starvation.
            // So, MainLoop() is a very long running operation that occupies a thread forever.
            // It uses synchronous waits / sleeps when it is idle and always keeps its thread afinity. 
            // Notably, the thread must be initially created explicitly, instead of obtaining it from the thread pool.
            // If we were to schedule MainLoop() on the thread pool, it would be possible that the thread chosen by the
            // pool had run user code before. Such user code may be doing an asynchronous wait scheduled to
            // continue on the same thread (e.g. this can occur when using a custom synchronization context or a
            // custom task scheduler). If such case the waiting user code will never continue (deadlock).
            // By creating our own thread, we guarantee no interactions with potentially incorrectly written async user code.

            int prevState = Interlocked.CompareExchange(ref _loopState, State.Running, State.NotStarted);
            if (prevState != State.NotStarted)
            {
                return false;
            }

            _loopSignal = new AutoResetEvent(false);

            _loopThread = new Thread(this.MainLoop);
            _loopThread.Name = this.GetType().Name + "." + nameof(MainLoop) + "-" + _activityExporter.GetType().Name;
            _loopThread.IsBackground = false;

            Log.Info(typeof(CollectAndExportBackgroundLoop).FullName,
                     "Starting loop thread",
                     nameof(_activityExporter), _activityExporter.GetType().FullName,
                     nameof(_exportInterval), _exportInterval,
                     nameof(_exportBatchSizeCapDefault), _exportBatchSizeCapDefault,
                     nameof(_aggregateActivitiesIntoTraces), _aggregateActivitiesIntoTraces,
                     $"{nameof(_loopThread)}.{nameof(Thread.Name)}", _loopThread.Name,
                     $"{nameof(_loopThread)}.{nameof(Thread.ManagedThreadId)}", _loopThread.ManagedThreadId,
                     $"{nameof(_loopThread)}.{nameof(Thread.Name)}", _loopThread.GetHashCode());

            _loopThread.Start();
            return true;
        }

        private IReadOnlyCollection<LocalTrace> GetResetCompletedTraces()
        {
            var newCompletedTracesBuffer = new GrowingCollection<LocalTrace>(CompletedItemsCollectionSegmentSize);

            GrowingCollection<LocalTrace> prevCompletedTracesBuffer = Interlocked.Exchange(ref _completedTraces, newCompletedTracesBuffer);
            return prevCompletedTracesBuffer;
        }

        private IReadOnlyCollection<ActivityStub> GetResetCompletedActivities()
        {
            var newCompletedActivitiesBuffer = new GrowingCollection<ActivityStub>(CompletedItemsCollectionSegmentSize);

            GrowingCollection<ActivityStub> prevCompletedActivitiesBuffer = Interlocked.Exchange(ref _completedActivities, newCompletedActivitiesBuffer);
            return prevCompletedActivitiesBuffer;
        }

        private void MainLoop()
        {
            Log.Info(typeof(CollectAndExportBackgroundLoop).FullName,
                     "Entering main loop",
                     nameof(_activityExporter), _activityExporter.GetType().FullName,
                     $"{nameof(_loopThread)}.{nameof(Thread.Name)}", _loopThread?.Name,
                     $"{nameof(_loopThread)}.{nameof(Thread.ManagedThreadId)}", _loopThread?.ManagedThreadId,
                     $"{nameof(_loopThread)}.{nameof(Thread.Name)}", _loopThread?.GetHashCode(),
                     $"{nameof(_loopThread)}.{nameof(Thread.IsThreadPoolThread)}", _loopThread?.IsThreadPoolThread,
                     $"{nameof(_loopThread)}.{nameof(Thread.IsBackground)}", _loopThread?.IsBackground,
                     $"{nameof(_loopThread)}.{nameof(Thread.Priority)}", _loopThread?.Priority,
                     $"{nameof(_loopThread)}.{nameof(Thread.ThreadState)}", _loopThread?.ThreadState);

            DateTimeOffset nextSendTargetTime = DateTimeOffset.Now + _exportInterval;

            while (Volatile.Read(ref _loopState) == State.Running)
            {
                try
                {
                    // Wait for the next internval:
                    WaitForNextExportCycle(nextSendTargetTime);

                    // The subsequent interval actually starts now.
                    // The exporting of items from the previous interval will occur during it.
                    nextSendTargetTime = DateTimeOffset.Now + _exportInterval;

                    // Grab the items and initiate the export:
                    ExportResult exportResult;
                    if (_aggregateActivitiesIntoTraces)
                    {
                        IReadOnlyCollection<LocalTrace> completedTraces = GetResetCompletedTraces();
                        exportResult = _activityExporter.ExportTraces(completedTraces);
                    }
                    else
                    {
                        IReadOnlyCollection<ActivityStub> completedActivities = GetResetCompletedActivities();
                        exportResult = _activityExporter.ExportActivities(completedActivities);
                    }

                    // Export finished. Log results:
                    LogExportResult(exportResult);

                    if (exportResult != null && exportResult.NextBatchSizeHint > 0)
                    {
                        // If the exporter gave us a batch size hint, use it:

                        Log.Info(typeof(CollectAndExportBackgroundLoop).FullName,
                                 $"{nameof(IActivityExporter)} provided a Batch-Size-Hint.",
                                 nameof(_activityExporter), _activityExporter.GetType().FullName,
                                 $"{nameof(_loopThread)}.{nameof(Thread.ManagedThreadId)}", _loopThread?.ManagedThreadId,
                                 nameof(_exportBatchSizeCapDefault), _exportBatchSizeCapDefault,
                                 nameof(_exportBatchSizeCapCurrent), _exportBatchSizeCapCurrent,
                                 nameof(ExportResult.NextBatchSizeHint), exportResult.NextBatchSizeHint);

                        _exportBatchSizeCapHintAge = 0;
                        _exportBatchSizeCapCurrent = exportResult.NextBatchSizeHint;
                    }
                    else if (++_exportBatchSizeCapHintAge > MoveTowardsDefaultBatchSizeAfterAge)
                    {
                        // If the last batch size hint was long time ago, move towards the default batch size:

                        _exportBatchSizeCapHintAge = MoveTowardsDefaultBatchSizeAfterAge; // prevent overflow

                        int delta = _exportBatchSizeCapDefault - _exportBatchSizeCapCurrent;
                        if (delta != 0)
                        {
                            int change = (int) Math.Round(delta * MoveTowardsDefaultBatchSizeStep);
                            if (change == 0)
                            {
                                change = delta > 0 ? 1 : -1;
                            } 
                            else if (Math.Abs(change) > Math.Abs(delta))
                            {
                                change = delta;
                            }

                            Log.Debug(typeof(CollectAndExportBackgroundLoop).FullName,
                                      $"Adjusting {nameof(_exportBatchSizeCapCurrent)} because last Batch-Size-Hint"
                                     + " is older than {MoveTowardsDefaultBatchSizeAfterAge} cycles.",
                                      nameof(_activityExporter), _activityExporter.GetType().FullName,
                                      $"{nameof(_loopThread)}.{nameof(Thread.ManagedThreadId)}", _loopThread?.ManagedThreadId,
                                      nameof(_exportBatchSizeCapDefault), _exportBatchSizeCapDefault,
                                      nameof(_exportBatchSizeCapCurrent), _exportBatchSizeCapCurrent,
                                      nameof(change), change);

                            _exportBatchSizeCapCurrent += change;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(typeof(CollectAndExportBackgroundLoop).FullName, ex,
                              nameof(_activityExporter), _activityExporter.GetType().FullName,
                              $"{nameof(_loopThread)}.{nameof(Thread.ManagedThreadId)}", _loopThread?.ManagedThreadId);
                }
            }  // while (Volatile.Read(ref _loopState) == State.Running)

            Interlocked.CompareExchange(ref _loopState, State.ShutDown, State.ShutdownRequested);
        }

        private void LogExportResult(ExportResult exportResult)
        {
            if (exportResult == null)
            {
                Log.Error(typeof(CollectAndExportBackgroundLoop).FullName,
                          "Items export failed",
                          nameof(_activityExporter), _activityExporter.GetType().FullName,
                          $"{nameof(_loopThread)}.{nameof(Thread.ManagedThreadId)}", _loopThread?.ManagedThreadId,
                          nameof(exportResult), "null");
            }
            else if (exportResult.IsSuccess)
            {
                Log.Debug(typeof(CollectAndExportBackgroundLoop).FullName,
                          "Items export completed",
                          nameof(_activityExporter), _activityExporter.GetType().FullName,
                          $"{nameof(_loopThread)}.{nameof(Thread.ManagedThreadId)}", _loopThread?.ManagedThreadId,
                          nameof(exportResult.IsTraceExport), exportResult.IsTraceExport,
                          nameof(exportResult.IsActivityExport), exportResult.IsActivityExport,
                          nameof(exportResult.RequestedTraceCount), exportResult.RequestedTraceCount,
                          nameof(exportResult.RequestedActivityCount), exportResult.RequestedActivityCount,
                          nameof(exportResult.NextBatchSizeHint), exportResult.NextBatchSizeHint);
            }
            else if (exportResult.Error != null)
            {
                Log.Error(typeof(CollectAndExportBackgroundLoop).FullName,
                          "Items export failed",
                          nameof(_activityExporter), _activityExporter.GetType().FullName,
                          $"{nameof(_loopThread)}.{nameof(Thread.ManagedThreadId)}", _loopThread?.ManagedThreadId,
                          nameof(exportResult.IsTraceExport), exportResult.IsTraceExport,
                          nameof(exportResult.IsActivityExport), exportResult.IsActivityExport,
                          nameof(exportResult.RequestedTraceCount), exportResult.RequestedTraceCount,
                          nameof(exportResult.RequestedActivityCount), exportResult.RequestedActivityCount,
                          nameof(exportResult.StatusCode), exportResult.StatusCode,
                          nameof(exportResult.NextBatchSizeHint), exportResult.NextBatchSizeHint,
                          nameof(exportResult.ErrorMessage), exportResult.ErrorMessage,
                          nameof(exportResult.Error), exportResult.Error);
            }
            else
            {
                Log.Error(typeof(CollectAndExportBackgroundLoop).FullName,
                         "Items export failed",
                          nameof(_activityExporter), _activityExporter.GetType().FullName,
                          $"{nameof(_loopThread)}.{nameof(Thread.ManagedThreadId)}", _loopThread?.ManagedThreadId,
                          nameof(exportResult.IsTraceExport), exportResult.IsTraceExport,
                          nameof(exportResult.IsActivityExport), exportResult.IsActivityExport,
                          nameof(exportResult.RequestedTraceCount), exportResult.RequestedTraceCount,
                          nameof(exportResult.RequestedActivityCount), exportResult.RequestedActivityCount,
                          nameof(exportResult.StatusCode), exportResult.StatusCode,
                          nameof(exportResult.NextBatchSizeHint), exportResult.NextBatchSizeHint,
                          nameof(exportResult.ErrorMessage), exportResult.ErrorMessage,
                          nameof(exportResult.Error), exportResult.Error);
            }
        }

        private void WaitForNextExportCycle(DateTimeOffset nextSendTargetTime)
        {
            while (true)
            {
                TimeSpan remainingWaitInterval = nextSendTargetTime - DateTimeOffset.Now;

                if (_completedTraces.Count >= _exportBatchSizeCapCurrent
                        || remainingWaitInterval <= TimeSpan.Zero
                        || Volatile.Read(ref _loopState) != State.Running)
                {
                    // If taget condition is met before we even start waiting, we do not wait.
                    // However, we still yioeld the tread to allow other work to make progress.

                    Thread.Yield();
                    return;
                }

                if (remainingWaitInterval < WaitIntervalMin)
                {
                    remainingWaitInterval = WaitIntervalMin;
                }

                try
                {
                    _loopSignal.WaitOne(remainingWaitInterval);
                }
                catch (Exception ex)
                {
                    Log.Error(typeof(CollectAndExportBackgroundLoop).FullName, ex,
                              nameof(_activityExporter), _activityExporter.GetType().FullName,
                              $"{nameof(_loopThread)}.{nameof(Thread.ManagedThreadId)}", _loopThread?.ManagedThreadId);
                }

                if (_completedTraces.Count >= _exportBatchSizeCapCurrent
                        || DateTimeOffset.Now >= nextSendTargetTime
                        || Volatile.Read(ref _loopState) != State.Running)
                {
                    return;
                }
            }
        }

        public void Shutdown()
        {
            // If we already shut down, all is good:
            int loopState = Volatile.Read(ref _loopState);
            if (loopState == State.ShutDown || loopState == State.Disposed)
            {
                return;
            }

            Log.Info(typeof(CollectAndExportBackgroundLoop).FullName,
                     "Requesting to shut down main loop",
                     nameof(_activityExporter), _activityExporter.GetType().FullName,
                     $"{nameof(_loopThread)}.{nameof(Thread.ManagedThreadId)}", _loopThread?.ManagedThreadId);

            // If we were not started, we can transition directly to shut down:
            int prevState = Interlocked.CompareExchange(ref _loopState, State.ShutDown, State.NotStarted);
            if (prevState == State.NotStarted)
            {
                Log.Info(typeof(CollectAndExportBackgroundLoop).FullName,
                         "Main loop shut down before it started",
                         nameof(_activityExporter), _activityExporter.GetType().FullName,
                         $"{nameof(_loopThread)}.{nameof(Thread.ManagedThreadId)}", _loopThread?.ManagedThreadId);

                return;
            }

            // Request shutdown:
            Interlocked.Exchange(ref _loopState, State.ShutdownRequested);

            // Signal main loop to wake up:
            _loopSignal.Set();

            // Yield thread and see if we have shut down:
            Thread.Yield();
            
            loopState = Volatile.Read(ref _loopState);
            if (loopState != State.ShutDown && loopState != State.Disposed)
            {
                // We have not shut down. We will now wait and periodically check until we shut down:
                int[] waitMillis = new int[] { 1, 2, 5, 10, 20, 50, 500 };
                int w = 0;

                loopState = Volatile.Read(ref _loopState);
                while (loopState != State.ShutDown && loopState != State.Disposed)
                {
                    Thread.Sleep(waitMillis[w]);

                    if (++w >= waitMillis.Length)
                    {
                        w = 0;
                    }

                    loopState = Volatile.Read(ref _loopState);
                }
            }

            Log.Info(typeof(CollectAndExportBackgroundLoop).FullName,
                     "Main loop shut down; requesting toi shut down th exporter",
                     nameof(_activityExporter), _activityExporter.GetType().FullName,
                     $"{nameof(_loopThread)}.{nameof(Thread.ManagedThreadId)}", _loopThread?.ManagedThreadId);

            try
            {
                _activityExporter.Shutdown();
            }
            catch (Exception ex)
            {
                Log.Error(typeof(CollectAndExportBackgroundLoop).FullName, ex,
                          nameof(_activityExporter), _activityExporter.GetType().FullName,
                          $"{nameof(_loopThread)}.{nameof(Thread.ManagedThreadId)}", _loopThread?.ManagedThreadId);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                int loopState = Volatile.Read(ref _loopState);
                if (loopState == State.Disposed)
                {
                    return;
                }

                // We need to signal the send loop to exit and then wait for it, before we can dispose.
                Shutdown();

                // Dispose managed state:

                AutoResetEvent loopSignal = _loopSignal;
                if (loopSignal != null)
                {
                    try
                    {
                        loopSignal.Set();
                    }
                    catch (Exception ex)
                    {
                        Log.Error(typeof(CollectAndExportBackgroundLoop).FullName, ex,
                                  nameof(_activityExporter), _activityExporter.GetType().FullName,
                                  $"{nameof(_loopThread)}.{nameof(Thread.ManagedThreadId)}", _loopThread?.ManagedThreadId);
                    }

                    try
                    {
                        loopSignal.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Log.Error(typeof(CollectAndExportBackgroundLoop).FullName, ex,
                                  nameof(_activityExporter), _activityExporter.GetType().FullName,
                                  $"{nameof(_loopThread)}.{nameof(Thread.ManagedThreadId)}", _loopThread?.ManagedThreadId);
                    }

                    _loopSignal = null;
                }

                try
                {
                    _activityExporter.Dispose();
                }
                catch (Exception ex)
                {
                    Log.Error(typeof(CollectAndExportBackgroundLoop).FullName, ex,
                              nameof(_activityExporter), _activityExporter.GetType().FullName,
                              $"{nameof(_loopThread)}.{nameof(Thread.ManagedThreadId)}", _loopThread?.ManagedThreadId);
                }

                _loopThread = null;

                Interlocked.Exchange(ref _loopState, State.Disposed);
            }

            // Free unmanaged resources and override finalizer
            // Set large fields to null
        }

        // Uncomment/Override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~ActivityCollector()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// The normal Shutdown method can take a long time, becasue it waits for the send loop and the exporter to shut down.
        /// Call ShutdownAsync, in order to perform the wait on the threadpool instead of the current thread.
        /// </summary>
        /// <returns>A task representing the completion of the Dispose.</returns>
        public Task ShutdownAsync()
        {
            return Task.Run(() =>
            {
                try
                {
                    Shutdown();
                }
                catch (Exception ex)
                {
                    Log.Error(typeof(CollectAndExportBackgroundLoop).FullName, ex,
                              nameof(_activityExporter), _activityExporter.GetType().FullName,
                              $"{nameof(_loopThread)}.{nameof(Thread.ManagedThreadId)}", _loopThread?.ManagedThreadId);
                }
            });
        }

        public void OnActivityStarted(ActivityStub activity)
        {
            if (activity.IsNoOpStub)
            {
                return;
            }

            if (!_aggregateActivitiesIntoTraces)
            {
                return;
            }

            activity.GetLocalTraceInfo(out bool isLocalRootActivity, out ulong rootActivitySpanIdHash);

            if (isLocalRootActivity)
            {
                var trace = new LocalTrace(activity, rootActivitySpanIdHash);
                bool isNewTrace = _activeTraces.TryAddNew(rootActivitySpanIdHash, trace);

                if (!isNewTrace)
                {
                    Log.Error(typeof(CollectAndExportBackgroundLoop).FullName,
                              $"{nameof(OnActivityStarted)}; activity is a local root, but a trace for this activity is already in-flight",
                              nameof(_activityExporter), _activityExporter.GetType().FullName,
                              $"{nameof(_loopThread)}.{nameof(Thread.ManagedThreadId)}", _loopThread?.ManagedThreadId,
                              "activity.Id", activity.Id,
                              nameof(rootActivitySpanIdHash), rootActivitySpanIdHash);
                }
            }
            else
            {
                bool isTraceExists = _activeTraces.TryGet(rootActivitySpanIdHash, out LocalTrace trace);

                if (!isTraceExists)
                {
                    Log.Error(typeof(CollectAndExportBackgroundLoop).FullName,
                              $"{nameof(OnActivityStarted)}; activity is NOT a local root; a trace for its parent chain should be in-flight, but it cannot be found",
                              nameof(_activityExporter), _activityExporter.GetType().FullName,
                              $"{nameof(_loopThread)}.{nameof(Thread.ManagedThreadId)}", _loopThread?.ManagedThreadId,
                              "activity.Id", activity.Id,
                              nameof(rootActivitySpanIdHash), rootActivitySpanIdHash);
                }
                else
                {
                    trace.Add(activity);
                }
            }
        }

        public void OnActivityStopped(ActivityStub activity)
        {
            if (activity.IsNoOpStub)
            {
                return;
            }

            if (!_aggregateActivitiesIntoTraces)
            {
                _completedActivities.Add(activity);

                if (_completedActivities.Count >= _exportBatchSizeCapCurrent)
                {
                    _loopSignal.Set();
                }
            }
            else
            {
                activity.GetLocalTraceInfo(out bool isLocalRootActivity, out ulong rootActivitySpanIdHash);

                if (isLocalRootActivity)
                {
                    bool isTraceExists = _activeTraces.TryRemove(rootActivitySpanIdHash, out LocalTrace trace);

                    if (!isTraceExists)
                    {
                        Log.Error(typeof(CollectAndExportBackgroundLoop).FullName,
                              $"{nameof(OnActivityStopped)}; activity is a local root; a trace for its parent chain should be in-flight, but it cannot be found",
                              nameof(_activityExporter), _activityExporter.GetType().FullName,
                              $"{nameof(_loopThread)}.{nameof(Thread.ManagedThreadId)}", _loopThread?.ManagedThreadId,
                              "activity.Id", activity.Id,
                              nameof(rootActivitySpanIdHash), rootActivitySpanIdHash);

                        // We should log this properly but in the prototype, we just throw.
                        throw new Exception($"Activity '{activity.Id}' stopped. It is a local root; a trace for its parent chain should be in-flight, but it cannot be found.");
                    }
                    else
                    {
                        _completedTraces.Add(trace);

                        if (_completedTraces.Count >= _exportBatchSizeCapCurrent)
                        {
                            _loopSignal.Set();
                        }
                    }
                }
            }
        }
    }
}

#pragma warning restore SA1214 // Readonly fields must appear before non-readonly fields
#pragma warning restore SA1201 // Elements must appear in the correct order
#pragma warning restore SA1124 // Do not use regions

