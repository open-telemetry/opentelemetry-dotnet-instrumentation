// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Concurrent;
using OpenTelemetry.AutoInstrumentation.OpAmp;

namespace OpenTelemetry.AutoInstrumentation.Tests.OpAmp;

public class OpAmpReportingWorkerTests
{
    private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(5);

    [Fact]
    public async Task ProviderRequestsRunAsynchronouslyAndCoalescePendingState()
    {
        using var firstRefreshEntered = new ManualResetEventSlim();
        using var releaseFirstRefresh = new ManualResetEventSlim();
        var processedRequests = new ConcurrentQueue<OpAmpReportingRequests>();
        var firstRefreshReleased = false;
        var worker = CreateWorker(requests =>
        {
            processedRequests.Enqueue(requests);
            if (processedRequests.Count == 1)
            {
                firstRefreshEntered.Set();
                firstRefreshReleased = releaseFirstRefresh.Wait(TestTimeout);
            }
        });

        try
        {
            worker.Request(OpAmpReportingRequests.EffectiveConfig);
            Assert.True(firstRefreshEntered.Wait(TestTimeout));

            worker.Request(OpAmpReportingRequests.EffectiveConfig | OpAmpReportingRequests.RemoteConfigStatus);
            worker.Request(OpAmpReportingRequests.RemoteConfigStatus);
            var completion = worker.WaitForAcceptedWorkAsync(CancellationToken.None);

            Assert.False(completion.IsCompleted);
            releaseFirstRefresh.Set();
            await completion.ConfigureAwait(true);
        }
        finally
        {
            releaseFirstRefresh.Set();
            worker.Abort();
        }

        Assert.True(firstRefreshReleased);
        Assert.Equal(
            [
                OpAmpReportingRequests.EffectiveConfig,
                OpAmpReportingRequests.EffectiveConfig | OpAmpReportingRequests.RemoteConfigStatus
            ],
            processedRequests);
    }

    [Fact]
    public async Task FullStateIsProcessedOnceAndLaterRequestsRemainPending()
    {
        using var firstRefreshEntered = new ManualResetEventSlim();
        using var releaseFirstRefresh = new ManualResetEventSlim();
        using var fullStateEntered = new ManualResetEventSlim();
        using var releaseFullState = new ManualResetEventSlim();
        var processedRequests = new ConcurrentQueue<OpAmpReportingRequests>();
        var firstRefreshReleased = false;
        var fullStateReleased = false;
        var worker = CreateWorker(requests =>
        {
            processedRequests.Enqueue(requests);
            if (requests == OpAmpReportingRequests.EffectiveConfig)
            {
                firstRefreshEntered.Set();
                firstRefreshReleased = releaseFirstRefresh.Wait(TestTimeout);
            }
            else if ((requests & OpAmpReportingRequests.FullState) != 0)
            {
                fullStateEntered.Set();
                fullStateReleased = releaseFullState.Wait(TestTimeout);
            }
        });

        try
        {
            worker.Request(OpAmpReportingRequests.EffectiveConfig);
            Assert.True(firstRefreshEntered.Wait(TestTimeout));

            worker.Request(OpAmpReportingRequests.CustomCapabilities);
            worker.Request(OpAmpReportingRequests.RemoteConfigStatus);
            worker.Request(OpAmpReportingRequests.FullState);
            releaseFirstRefresh.Set();
            Assert.True(fullStateEntered.Wait(TestTimeout));

            worker.Request(OpAmpReportingRequests.CustomCapabilities);
            releaseFullState.Set();
            await worker.WaitForAcceptedWorkAsync(CancellationToken.None).ConfigureAwait(true);
        }
        finally
        {
            releaseFirstRefresh.Set();
            releaseFullState.Set();
            worker.Abort();
        }

        Assert.True(firstRefreshReleased);
        Assert.True(fullStateReleased);
        var batches = processedRequests.ToArray();
        Assert.Equal(3, batches.Length);
        Assert.Equal(OpAmpReportingRequests.EffectiveConfig, batches[0]);
        Assert.True((batches[1] & OpAmpReportingRequests.FullState) != 0);
        Assert.Equal(OpAmpReportingRequests.CustomCapabilities, batches[2]);
    }

    [Fact]
    public async Task CapturedBatchWaitDoesNotIncludeLaterWork()
    {
        using var firstRefreshEntered = new ManualResetEventSlim();
        using var releaseFirstRefresh = new ManualResetEventSlim();
        using var laterRefreshEntered = new ManualResetEventSlim();
        using var releaseLaterRefresh = new ManualResetEventSlim();
        var worker = CreateWorker(requests =>
        {
            if ((requests & OpAmpReportingRequests.EffectiveConfig) != 0)
            {
                firstRefreshEntered.Set();
                releaseFirstRefresh.Wait(TestTimeout);
            }

            if ((requests & OpAmpReportingRequests.RemoteConfigStatus) != 0)
            {
                laterRefreshEntered.Set();
                releaseLaterRefresh.Wait(TestTimeout);
            }
        });

        try
        {
            worker.Request(OpAmpReportingRequests.EffectiveConfig);
            Assert.True(firstRefreshEntered.Wait(TestTimeout));
            var firstGeneration = worker.WaitForAcceptedWorkAsync(CancellationToken.None);

            worker.Request(OpAmpReportingRequests.RemoteConfigStatus);
            releaseFirstRefresh.Set();
            Assert.True(laterRefreshEntered.Wait(TestTimeout));

            var completed = await Task.WhenAny(firstGeneration, Task.Delay(TestTimeout)).ConfigureAwait(true);
            Assert.Same(firstGeneration, completed);
            Assert.False(worker.WaitForAcceptedWorkAsync(CancellationToken.None).IsCompleted);

            releaseLaterRefresh.Set();
            await worker.WaitForAcceptedWorkAsync(CancellationToken.None).ConfigureAwait(true);
        }
        finally
        {
            releaseFirstRefresh.Set();
            releaseLaterRefresh.Set();
            worker.Abort();
        }
    }

    [Fact]
    public async Task CanceledBatchWaitDoesNotPreventLaterProgress()
    {
        using var refreshEntered = new ManualResetEventSlim();
        using var releaseRefresh = new ManualResetEventSlim();
        using var cancellationSource = new CancellationTokenSource();
        var worker = CreateWorker(_ =>
        {
            refreshEntered.Set();
            releaseRefresh.Wait(TestTimeout);
        });

        try
        {
            worker.Request(OpAmpReportingRequests.EffectiveConfig);
            Assert.True(refreshEntered.Wait(TestTimeout));
            var canceledWait = worker.WaitForAcceptedWorkAsync(cancellationSource.Token);
            var survivingWait = worker.WaitForAcceptedWorkAsync(CancellationToken.None);

#if NET
            await cancellationSource.CancelAsync().ConfigureAwait(true);
#else
            cancellationSource.Cancel();
#endif
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => canceledWait).ConfigureAwait(true);
            Assert.False(survivingWait.IsCompleted);

            releaseRefresh.Set();
            await survivingWait.ConfigureAwait(true);
        }
        finally
        {
            releaseRefresh.Set();
            worker.Abort();
        }
    }

    [Fact]
    public async Task ProcessorFailureIsIsolatedAndLaterBatchContinues()
    {
        using var firstBatchEntered = new ManualResetEventSlim();
        using var releaseFirstBatch = new ManualResetEventSlim();
        var processedBatches = 0;
        var firstBatchReleased = false;
        var worker = CreateWorker(_ =>
        {
            if (Interlocked.Increment(ref processedBatches) == 1)
            {
                firstBatchEntered.Set();
                firstBatchReleased = releaseFirstBatch.Wait(TestTimeout);
                throw new InvalidOperationException("Test failure.");
            }
        });

        try
        {
            worker.Request(OpAmpReportingRequests.CustomCapabilities);
            Assert.True(firstBatchEntered.Wait(TestTimeout));
            worker.Request(OpAmpReportingRequests.EffectiveConfig);
            releaseFirstBatch.Set();
            await worker.WaitForAcceptedWorkAsync(CancellationToken.None).ConfigureAwait(true);
        }
        finally
        {
            releaseFirstBatch.Set();
            worker.Abort();
        }

        Assert.True(firstBatchReleased);
        Assert.Equal(2, processedBatches);
    }

    [Fact]
    public async Task StopDrainsAcceptedWorkAndRejectsNewRequests()
    {
        using var refreshEntered = new ManualResetEventSlim();
        using var releaseRefresh = new ManualResetEventSlim();
        var refreshCount = 0;
        var worker = CreateWorker(_ =>
        {
            Interlocked.Increment(ref refreshCount);
            refreshEntered.Set();
            releaseRefresh.Wait(TestTimeout);
        });

        try
        {
            worker.Request(OpAmpReportingRequests.EffectiveConfig);
            Assert.True(refreshEntered.Wait(TestTimeout));
            var stop = worker.StopAcceptingAsync();
            worker.Request(OpAmpReportingRequests.RemoteConfigStatus);

            Assert.False(stop.IsCompleted);
            releaseRefresh.Set();
            await stop.ConfigureAwait(true);
            Assert.Equal(1, refreshCount);
        }
        finally
        {
            releaseRefresh.Set();
            worker.Abort();
        }
    }

    [Fact]
    public async Task AbortFaultsActiveAndPendingWaiters()
    {
        using var blockedRefreshEntered = new ManualResetEventSlim();
        using var releaseBlockedRefresh = new ManualResetEventSlim();
        var blockedWorker = CreateWorker(_ =>
        {
            blockedRefreshEntered.Set();
            releaseBlockedRefresh.Wait(TestTimeout);
        });

        try
        {
            blockedWorker.Request(OpAmpReportingRequests.EffectiveConfig);
            Assert.True(blockedRefreshEntered.Wait(TestTimeout));
            var active = blockedWorker.WaitForAcceptedWorkAsync(CancellationToken.None);
            blockedWorker.Request(OpAmpReportingRequests.RemoteConfigStatus);
            var pending = blockedWorker.WaitForAcceptedWorkAsync(CancellationToken.None);
            blockedWorker.Abort();

            await Assert.ThrowsAsync<ObjectDisposedException>(() => active).ConfigureAwait(true);
            await Assert.ThrowsAsync<ObjectDisposedException>(() => pending).ConfigureAwait(true);
        }
        finally
        {
            blockedWorker.Abort();
            releaseBlockedRefresh.Set();
        }
    }

    private static OpAmpReportingWorker CreateWorker(Action<OpAmpReportingRequests> process)
    {
        return new OpAmpReportingWorker(new DelegateReportingProcessor(process));
    }

    private sealed class DelegateReportingProcessor(Action<OpAmpReportingRequests> process) : IOpAmpReportingProcessor
    {
        public void Process(OpAmpReportingRequests requests)
        {
            process(requests);
        }
    }
}
