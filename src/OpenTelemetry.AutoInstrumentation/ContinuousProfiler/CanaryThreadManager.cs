// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NETFRAMEWORK
using System.Diagnostics;
using OpenTelemetry.AutoInstrumentation.Logging;

namespace OpenTelemetry.AutoInstrumentation.ContinuousProfiler;

/// <summary>
/// Manages a dedicated canary thread for .NET Framework continuous profiling.
/// The canary thread is required for seeded stack walking on .NET Framework.
/// </summary>
internal sealed class CanaryThreadManager : IDisposable
{
    private const string CanaryThreadName = "OpenTelemetry Profiler Canary Thread";
    private static readonly IOtelLogger Logger = OtelLogging.GetLogger();

    private readonly ManualResetEventSlim _shutdownTrigger = new(false);
    private readonly ManualResetEventSlim _threadStarted = new(false);
    private Thread? _canaryThread;
    private bool _disposed;

    /// <summary>
    /// Starts the canary thread and waits for it to be registered with the native profiler.
    /// </summary>
    /// <param name="timeout">Maximum time to wait for the canary thread to be registered.</param>
    /// <returns>True if the canary thread started successfully; otherwise, false.</returns>
    public bool Start(TimeSpan timeout)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(CanaryThreadManager));
        }

        if (_canaryThread != null)
        {
            Logger.Warning("Canary thread is already running.");
            return true;
        }

        Logger.Information("Starting canary thread for .NET Framework stack walking...");

        _canaryThread = new Thread(CanaryThreadProc)
        {
            Name = CanaryThreadName,
            IsBackground = true,
        };

        _canaryThread.Start();

        // Wait for the thread to be registered with the native profiler
        bool started = _threadStarted.Wait(timeout);

        if (started)
        {
            Logger.Information("Canary thread started successfully. ThreadId: {0}", _canaryThread.ManagedThreadId);
        }
        else
        {
            Logger.Error("Canary thread failed to start within {0}ms timeout.", timeout.TotalMilliseconds);
        }

        return started;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        Logger.Debug("Stopping canary thread...");

        _shutdownTrigger.Set();

        if (_canaryThread != null && _canaryThread.IsAlive)
        {
            // Give the thread a reasonable time to terminate
            if (!_canaryThread.Join(TimeSpan.FromSeconds(5)))
            {
                Logger.Warning("Canary thread did not terminate within timeout period.");
            }
            else
            {
                Logger.Information("Canary thread terminated successfully.");
            }
        }

        _shutdownTrigger.Dispose();
        _threadStarted.Dispose();
    }

    private void CanaryThreadProc()
    {
        try
        {
            Logger.Debug(
                "Canary thread started. ManagedThreadId: {0}",
                Thread.CurrentThread.ManagedThreadId);

            // Signal that the thread has started
            // The native profiler will detect this thread via ThreadCreated/ThreadAssignedToOSThread callbacks
            _threadStarted.Set();

            // Keep the thread alive until shutdown is requested
            // This thread must remain alive for the lifetime of the profiler
            _shutdownTrigger.Wait();

            Logger.Debug("Canary thread shutdown requested.");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Exception in canary thread.");
        }
        finally
        {
            Logger.Debug("Canary thread exiting.");
        }
    }
}
#endif
