// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Configurations;
using OpenTelemetry.AutoInstrumentation.Logging;
using OpenTelemetry.AutoInstrumentation.Plugins;
using OpenTelemetry.Resources;

namespace OpenTelemetry.AutoInstrumentation.OpAmp;

internal sealed class OpAmpLoader
{
    private static readonly IOtelLogger Logger = OtelLogging.GetLogger("OpAmp");
    private static readonly TimeSpan ShutdownTimeout = TimeSpan.FromSeconds(5);
    private readonly object _lifecycleLock = new();

    private OpAmpManager? _opAmpManager;
    private bool _stopped;

    public void PrepareOpAmpClient(Resource resources, OpAmpSettings opAmpSettings, PluginManager pluginManager)
    {
        // Instrumentation.Initialize calls preparation once per instrumentation lifetime.
        lock (_lifecycleLock)
        {
            if (_stopped)
            {
                return;
            }
        }

        // Plugin configuration must not prevent shutdown from acquiring the lifecycle lock.
#pragma warning disable CA2000 // Candidates are registered or disposed by the bounded shutdown worker.
        var configured = OpAmpManager.TryCreate(resources, opAmpSettings, pluginManager, out var manager);
#pragma warning restore CA2000
        lock (_lifecycleLock)
        {
            if (configured && !_stopped)
            {
                _opAmpManager = manager;
                return;
            }
        }

        if (manager != null)
        {
            StopManager(manager, ShutdownTimeout);
        }
    }

    public void StartOpAmpClient()
    {
        lock (_lifecycleLock)
        {
            _opAmpManager?.StartClient();
        }
    }

    public void StopOpAmpClientIfRunning()
    {
        StopOpAmpClientIfRunning(ShutdownTimeout);
    }

    // The timeout is configurable only for deterministic tests. Production always uses ShutdownTimeout.
    internal void StopOpAmpClientIfRunning(TimeSpan shutdownTimeout)
    {
#if NET
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(shutdownTimeout, TimeSpan.Zero);
#else
        if (shutdownTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(shutdownTimeout));
        }
#endif

        OpAmpManager? manager;
        lock (_lifecycleLock)
        {
            _stopped = true;
            manager = _opAmpManager;
            _opAmpManager = null;
        }

        if (manager == null)
        {
            return;
        }

        StopManager(manager, shutdownTimeout);
    }

    private static void StopManager(OpAmpManager manager, TimeSpan shutdownTimeout)
    {
        Task? stopTask = null;
        try
        {
            stopTask = manager.StopOpAmpClientAsync();

            // A timed wait does not require a thread-pool timer to enforce the deadline.
            if (!stopTask.Wait(shutdownTimeout))
            {
                Logger.Warning("OpAmp client did not stop within the shutdown timeout. Forced cleanup will continue in the background.");
                ContinueForcedCleanup(manager, stopTask);
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "An error occurred while stopping the OpAmp client.");
            ContinueForcedCleanup(manager, stopTask);
        }
    }

    private static void ContinueForcedCleanup(OpAmpManager manager, Task? stopTask)
    {
        if (stopTask != null)
        {
            _ = stopTask.ContinueWith(
                completedStopTask => Logger.Error(completedStopTask.Exception!, "An error occurred while stopping the OpAmp client."),
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }

        var forcedCleanupTask = manager.RequestForcedShutdown();
        _ = forcedCleanupTask.ContinueWith(
            completedForcedCleanupTask =>
            {
                if (completedForcedCleanupTask.Exception != null)
                {
                    Logger.Error(completedForcedCleanupTask.Exception, "An error occurred while forcefully disposing the OpAmp client.");
                }

                try
                {
                    manager.Dispose();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "An error occurred while disposing the OpAmp client.");
                }
            },
            CancellationToken.None,
            TaskContinuationOptions.None,
            TaskScheduler.Default);
    }
}
