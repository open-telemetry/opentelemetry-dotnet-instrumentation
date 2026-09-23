// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Logging;

namespace OpenTelemetry.AutoInstrumentation.OpAmp;

[Flags]
internal enum OpAmpReportingRequests
{
    None = 0,
    CustomCapabilities = 1,
    EffectiveConfig = 2,
    RemoteConfigStatus = 4,
    FullState = 8
}

internal sealed class OpAmpReportingWorker
{
    private static readonly IOtelLogger Logger = OtelLogging.GetLogger("OpAmp");
    private readonly object _lock = new();
    private readonly IOpAmpReportingProcessor _processor;

    // A single drain allows at most one executing batch and one coalesced successor.
    private ReportingBatch? _activeBatch;
    private ReportingBatch? _pendingBatch;
    private WorkerState _state = WorkerState.Accepting;
    private bool _drainScheduled;

    public OpAmpReportingWorker(IOpAmpReportingProcessor processor)
    {
        _processor = processor;
    }

    private enum WorkerState
    {
        Accepting,
        Stopping,
        Aborted
    }

    public void Request(OpAmpReportingRequests requests)
    {
        if (requests == OpAmpReportingRequests.None)
        {
            return;
        }

        bool scheduleDrain;
        lock (_lock)
        {
            if (_state != WorkerState.Accepting)
            {
                return;
            }

            scheduleDrain = AcceptWork(requests);
        }

        if (scheduleDrain)
        {
            ScheduleDrain();
        }
    }

    public Task WaitForAcceptedWorkAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Task completion;
        lock (_lock)
        {
            completion = GetLatestCompletion();
        }

        return WaitForCompletionAsync(completion, cancellationToken);
    }

    public Task StopAcceptingAsync()
    {
        Task completion;
        lock (_lock)
        {
            if (_state == WorkerState.Accepting)
            {
                _state = WorkerState.Stopping;
            }

            completion = GetLatestCompletion();
        }

        return WaitForCompletionAsync(completion, CancellationToken.None);
    }

    public void Abort()
    {
        ReportingBatch? activeBatch;
        ReportingBatch? pendingBatch;
        lock (_lock)
        {
            if (_state == WorkerState.Aborted)
            {
                return;
            }

            _state = WorkerState.Aborted;
            activeBatch = _activeBatch;
            pendingBatch = _pendingBatch;
            _activeBatch = null;
            _pendingBatch = null;
        }

        // Wake waiters so they can observe the aborted state and fault their own tasks.
        activeBatch?.Complete();
        pendingBatch?.Complete();
    }

    private static async Task AwaitWithCancellationAsync(Task task, CancellationToken cancellationToken)
    {
        if (task.IsCompleted || !cancellationToken.CanBeCanceled)
        {
            await task.ConfigureAwait(false);
            return;
        }

        var cancellationCompletion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using (cancellationToken.Register(() => cancellationCompletion.TrySetResult(true)))
        {
            if (await Task.WhenAny(task, cancellationCompletion.Task).ConfigureAwait(false) != task)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }
        }

        await task.ConfigureAwait(false);
    }

    private bool AcceptWork(OpAmpReportingRequests requests)
    {
        _pendingBatch ??= new ReportingBatch();
        _pendingBatch.Requests |= requests;
        if (_drainScheduled)
        {
            return false;
        }

        _drainScheduled = true;
        return true;
    }

    private Task GetLatestCompletion()
    {
        return (_pendingBatch ?? _activeBatch)?.Completion ?? Task.CompletedTask;
    }

    private async Task WaitForCompletionAsync(Task completion, CancellationToken cancellationToken)
    {
        await AwaitWithCancellationAsync(completion, cancellationToken).ConfigureAwait(false);

        lock (_lock)
        {
#if NET
            ObjectDisposedException.ThrowIf(_state == WorkerState.Aborted, this);
#else
            if (_state == WorkerState.Aborted)
            {
                throw new ObjectDisposedException(nameof(OpAmpReportingWorker));
            }
#endif
        }
    }

    private void ScheduleDrain()
    {
        // Callers await reporting-batch completion rather than the lifetime of this drain task.
        // ProcessBatch isolates processor failures so subsequent batches can continue.
        _ = Task.Run(Drain);
    }

    private void Drain()
    {
        // Continue with work accepted while the previous batch was executing.
        while (true)
        {
            ReportingBatch batch;
            lock (_lock)
            {
                if (_state == WorkerState.Aborted)
                {
                    _drainScheduled = false;
                    return;
                }

                if (_pendingBatch == null)
                {
                    _drainScheduled = false;
                    return;
                }

                batch = CaptureBatch();
            }

            ProcessBatch(batch);
            CompleteBatch(batch);
        }
    }

    private ReportingBatch CaptureBatch()
    {
        var batch = _pendingBatch!;
        _pendingBatch = null;
        _activeBatch = batch;
        return batch;
    }

    private void ProcessBatch(ReportingBatch batch)
    {
        try
        {
            _processor.Process(batch.Requests);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "An error occurred while processing OpAmp reporting state.");
        }
    }

    private void CompleteBatch(ReportingBatch batch)
    {
        lock (_lock)
        {
            if (ReferenceEquals(_activeBatch, batch))
            {
                _activeBatch = null;
            }
        }

        batch.Complete();
    }

    private sealed class ReportingBatch
    {
        private readonly TaskCompletionSource<bool> _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public OpAmpReportingRequests Requests { get; set; }

        public Task Completion => _completion.Task;

        public void Complete()
        {
            _completion.TrySetResult(true);
        }
    }
}
