// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Logging;
using OpenTelemetry.Resources;

namespace OpenTelemetry.Metrics;

internal sealed class PeriodicExportingMetricReaderAsync : MetricReaderAsync
{
    private readonly Thread _ExporterThread;
    private readonly AutoResetEvent _ExportTrigger = new(false);
    private readonly ManualResetEvent _DataExportedTrigger = new(false);
    private readonly ManualResetEvent _ShutdownTrigger = new(false);
    private readonly ManualResetEvent _ExportAsyncTaskCompleteTrigger = new(false);
    private readonly int _ExportIntervalMilliseconds;
    private TaskCompletionSource? _ShutdownTcs;
    private bool _Disposed;

    public PeriodicExportingMetricReaderAsync(
        ILogger<PeriodicExportingMetricReaderAsync> logger,
        Resource resource,
        IMetricExporterAsync exporter,
        IEnumerable<IMetricProducerFactory>? metricProducerFactories,
        PeriodicExportingMetricReaderOptions options)
        : base(logger, resource, exporter, metricProducerFactories, options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _ExportIntervalMilliseconds = options.ExportIntervalMilliseconds;

        _ExporterThread = new Thread(ExporterProc)
        {
            IsBackground = true,
            Name = $"OpenTelemetry-{GetType()}-{exporter.GetType()}",
        };
        _ExporterThread.Start();
    }

    public override Task CollectAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    public override Task FlushAsync(CancellationToken cancellationToken)
    {
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
                if (!timedOut)
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
            }

            _Disposed = true;
        }

        base.Dispose(disposing);
    }

    private void ExporterProc(object? state)
    {
        var triggers = new WaitHandle[] { _ExportTrigger, _ShutdownTrigger };

        while (true)
        {
            int waitHandleIndex;
            try
            {
                waitHandleIndex = WaitHandle.WaitAny(triggers, _ExportIntervalMilliseconds);
            }
            catch (ObjectDisposedException)
            {
                // the exporter is somehow disposed before the worker thread could finish its job
                return;
            }

            _ExportAsyncTaskCompleteTrigger.Reset();

            OnCollectAsync(cancellationToken: default).ContinueWith(
                static (t, o) =>
                {
                    ((EventWaitHandle)o!).Set();
                },
                _ExportAsyncTaskCompleteTrigger,
                CancellationToken.None,
                TaskContinuationOptions.RunContinuationsAsynchronously,
                TaskScheduler.Default);

            _ExportAsyncTaskCompleteTrigger.WaitOne();

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

            if (waitHandleIndex == 1)
            {
                _ShutdownTcs?.TrySetResult();
                return;
            }
        }
    }
}
