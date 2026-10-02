// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Configurations;
using OpenTelemetry.AutoInstrumentation.Logging;
using OpenTelemetry.AutoInstrumentation.Plugins;
using OpenTelemetry.Resources;

namespace OpenTelemetry.AutoInstrumentation.OpAmp;

internal static class OpAmpLoader
{
    private static readonly IOtelLogger Logger = OtelLogging.GetLogger("OpAmp");
    private static readonly TimeSpan ShutdownTimeout = TimeSpan.FromSeconds(5);
    private static readonly object LifecycleLock = new();

    private static OpAmpManager? _opAmpManager;

    public static void PrepareOpAmpClient(Resource resources, OpAmpSettings opAmpSettings, PluginManager pluginManager)
    {
        lock (LifecycleLock)
        {
            if (_opAmpManager != null)
            {
                throw new InvalidOperationException("OpAMP is already enabled");
            }

            OpAmpManager? manager = null;
            try
            {
                if (!OpAmpManager.TryCreate(resources, opAmpSettings, pluginManager, out manager))
                {
                    return;
                }

                _opAmpManager = manager;
                manager = null;
            }
            finally
            {
                manager?.Dispose();
            }
        }
    }

    public static void StartOpAmpClient()
    {
        lock (LifecycleLock)
        {
            _opAmpManager?.StartClient();
        }
    }

    public static void StopOpAmpClientIfRunning()
    {
        StopOpAmpClientIfRunning(ShutdownTimeout);
    }

    // The timeout is configurable only for deterministic tests. Production always uses ShutdownTimeout.
    internal static void StopOpAmpClientIfRunning(TimeSpan shutdownTimeout)
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
        lock (LifecycleLock)
        {
            manager = _opAmpManager;
            _opAmpManager = null;
        }

        if (manager == null)
        {
            return;
        }

        Task? stopTask = null;
        try
        {
            var timeoutTask = Task.Delay(shutdownTimeout);
            stopTask = manager.StopOpAmpClientAsync();
            var completedTaskIndex = Task.WaitAny(stopTask, timeoutTask);

            if (completedTaskIndex == 0)
            {
                try
                {
                    stopTask.GetAwaiter().GetResult();
                }
                finally
                {
                    manager.Dispose();
                }
            }
            else
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
