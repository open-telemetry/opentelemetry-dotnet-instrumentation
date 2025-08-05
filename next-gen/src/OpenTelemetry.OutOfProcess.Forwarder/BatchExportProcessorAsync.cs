// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Logging;
using OpenTelemetry.Resources;

namespace OpenTelemetry;

internal abstract class BatchExportProcessorAsync<TBufferedTelemetry, TBatchWriter, TBufferedBatch> : Processor
    where TBufferedTelemetry : class, IBufferedTelemetry<TBufferedTelemetry>
    where TBatchWriter : IBatchWriter
    where TBufferedBatch : IBatch<TBatchWriter>, allows ref struct
{
    private readonly ILogger _Logger;
    private readonly IExporterAsync<TBatchWriter> _Exporter;
    private readonly CircularBuffer<TBufferedTelemetry> _CircularBuffer;
    private readonly BufferedTelemetryBatch<TBufferedTelemetry> _BufferedBatch;
    private readonly Thread _ExporterThread;
    private readonly AutoResetEvent _ExportTrigger = new(false);
    private readonly ManualResetEvent _DataExportedTrigger = new(false);
    private readonly ManualResetEvent _ShutdownTrigger = new(false);
    private readonly ManualResetEvent _ExportAsyncTaskCompleteTrigger = new(false);
    private readonly int _MaxExportBatchSize;
    private readonly int _ExportIntervalMilliseconds;
    private readonly int _ExportTimeoutMilliseconds;
    private long _DroppedCount;
    private long _ShutdownDrainTarget = long.MaxValue;
    private TaskCompletionSource? _ShutdownTcs;
    private CancellationTokenSource? _ExportCts;
    private bool _Disposed;

    protected BatchExportProcessorAsync(
        ILogger logger,
        Resource resource,
        IExporterAsync<TBatchWriter> exporter,
        BatchExportProcessorOptions options)
    {
        // Validate all parameters first, before initializing anything
        ArgumentNullException.ThrowIfNull(options);
        _Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _Exporter = exporter ?? throw new ArgumentNullException(nameof(exporter));

        _CircularBuffer = new(options.MaxQueueSize);
        _BufferedBatch = new BufferedTelemetryBatch<TBufferedTelemetry>(resource);
        _MaxExportBatchSize = Math.Min(options.MaxExportBatchSize, options.MaxQueueSize);
        _ExportIntervalMilliseconds = options.ExportIntervalMilliseconds;
        _ExportTimeoutMilliseconds = options.ExportTimeoutMilliseconds;

        // Only start the thread after all validation and initialization is complete
        _ExporterThread = new Thread(ExporterProc)
        {
            IsBackground = true,
            Name = $"OpenTelemetry-{GetType()}-{exporter.GetType()}",
        };
        _ExporterThread.Start();
    }

    public override Task FlushAsync(CancellationToken cancellationToken)
    {
        long tail = _CircularBuffer.RemovedCount;
        long head = _CircularBuffer.AddedCount;

        if (head == tail)
        {
            return Task.CompletedTask;
        }

        try
        {
            _ExportTrigger.Set();
        }
        catch (ObjectDisposedException)
        {
            return Task.CompletedTask;
        }

        var tcs = new TaskCompletionSource();
        RegisteredWaitHandle rwh = ThreadPool.RegisterWaitForSingleObject(
            waitObject: _DataExportedTrigger,
            callBack: (state, timedOut) =>
            {
                if (!timedOut && _CircularBuffer.RemovedCount >= head)
                {
                    tcs.TrySetResult();
                }
                else if (cancellationToken.IsCancellationRequested)
                {
                    tcs.TrySetCanceled();
                }
            },
            state: null,
            millisecondsTimeOutInterval: 1000,
            executeOnlyOnce: false);

        return tcs.Task
            .ContinueWith((_) => rwh.Unregister(waitObject: null), TaskScheduler.Default);
    }

    public override Task ShutdownAsync(CancellationToken cancellationToken)
    {
        if (_ShutdownTcs != null)
        {
            return _ShutdownTcs.Task.WaitAsync(cancellationToken);
        }

        Volatile.Write(ref _ShutdownDrainTarget, _CircularBuffer.AddedCount);

        _ShutdownTcs = new TaskCompletionSource();

        _ShutdownTrigger.Set();

        return _ShutdownTcs.Task.WaitAsync(cancellationToken);
    }

    protected override void Dispose(bool disposing)
    {
        if (!_Disposed)
        {
            if (disposing)
            {
                _ExportTrigger.Dispose();
                _DataExportedTrigger.Dispose();
                _ShutdownTrigger.Dispose();
                _ExportAsyncTaskCompleteTrigger.Dispose();
                _ExportCts?.Dispose();
                _Exporter.Dispose();
            }

            _Disposed = true;
        }

        base.Dispose(disposing);
    }

    protected void AddItemToBatch(TBufferedTelemetry item)
    {
        Debug.Assert(item != null);

        if (_CircularBuffer.TryAdd(item, maxSpinCount: 50000))
        {
            if (_CircularBuffer.Count >= _MaxExportBatchSize)
            {
                try
                {
                    _ExportTrigger.Set();
                }
                catch (ObjectDisposedException)
                {
                }
            }
        }
        else
        {
            // either the queue is full or exceeded the spin limit, drop the item on the floor
            Interlocked.Increment(ref _DroppedCount);
        }
    }

    protected abstract void CreateBatch(
        BufferedTelemetryBatch<TBufferedTelemetry> bufferedBatch,
        out TBufferedBatch batch);

    private void ExporterProc(object? state)
    {
        var triggers = new WaitHandle[] { _ExportTrigger, _ShutdownTrigger };

        while (true)
        {
            // only wait when the queue doesn't have enough items, otherwise keep busy and send data continuously
            if (_CircularBuffer.Count < _MaxExportBatchSize)
            {
                try
                {
                    WaitHandle.WaitAny(triggers, _ExportIntervalMilliseconds);
                }
                catch (ObjectDisposedException)
                {
                    // the exporter is somehow disposed before the worker thread could finish its job
                    return;
                }
            }

            long targetCount = _CircularBuffer.RemovedCount + Math.Min(_MaxExportBatchSize, _CircularBuffer.Count);
            if (targetCount > 0)
            {
                while (_CircularBuffer.RemovedCount < targetCount)
                {
                    TBufferedTelemetry item = _CircularBuffer.Read();

                    _BufferedBatch.Add(item);
                }

                try
                {
                    _ExportAsyncTaskCompleteTrigger.Reset();

                    ExportAsync().ContinueWith(
                        static (t, o) =>
                        {
                            try
                            {
                                ((EventWaitHandle)o!).Set();
                            }
                            catch (ObjectDisposedException)
                            {
                                // EventWaitHandle was disposed during shutdown, nothing to signal
                            }
                        },
                        _ExportAsyncTaskCompleteTrigger,
                        CancellationToken.None,
                        TaskContinuationOptions.RunContinuationsAsynchronously,
                        TaskScheduler.Default);

                    _ExportAsyncTaskCompleteTrigger.WaitOne();
                }
                catch (ObjectDisposedException)
                {
                    // The processor is being disposed, exit the worker thread
                    return;
                }
                finally
                {
                    _BufferedBatch.Reset();
                }
            }

            try
            {
                _DataExportedTrigger.Set();
                _DataExportedTrigger.Reset();
            }
            catch (ObjectDisposedException)
            {
                // the exporter is somehow disposed before the worker thread could finish its job
                return;
            }

            if (_CircularBuffer.RemovedCount >= Volatile.Read(ref _ShutdownDrainTarget))
            {
                _ShutdownTcs?.TrySetResult();

                if (_DroppedCount > 0)
                {
                    _Logger.BatchExporterDroppedItems(_Exporter.GetType().FullName, _DroppedCount);
                }

                return;
            }
        }
    }

    private Task ExportAsync()
    {
        CancellationToken token;
        if (_ExportTimeoutMilliseconds < 0)
        {
            token = CancellationToken.None;
        }
        else
        {
            if (_ExportCts == null || !_ExportCts.TryReset())
            {
                CancellationTokenSource? oldCts = _ExportCts;
                _ExportCts = new CancellationTokenSource(_ExportTimeoutMilliseconds);
                oldCts?.Dispose();
            }

            token = _ExportCts.Token;
        }

        CreateBatch(_BufferedBatch, out TBufferedBatch? batch);

        Task<bool> exportAsyncTask;
        try
        {
            exportAsyncTask = _Exporter.ExportAsync(in batch, token);
        }
        catch (Exception ex)
        {
            _Logger.TelemetryExportException(ex, _Exporter.GetType().FullName);
            return Task.CompletedTask;
        }

        return AwaitExportAsyncTask(exportAsyncTask);
    }

    private async Task AwaitExportAsyncTask(
        Task<bool> exportAsyncTask)
    {
        Debug.Assert(exportAsyncTask != null);

        try
        {
            bool result = await exportAsyncTask.ConfigureAwait(false);

            _Logger.TelemetryExportCompleted(result, _Exporter.GetType().FullName);
        }
        catch (Exception ex)
        {
            _Logger.TelemetryExportException(ex, _Exporter.GetType().FullName);
        }
    }
}
