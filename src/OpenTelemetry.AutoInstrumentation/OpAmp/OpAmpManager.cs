// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics.CodeAnalysis;
using OpenTelemetry.AutoInstrumentation.Configurations;
using OpenTelemetry.AutoInstrumentation.Logging;
using OpenTelemetry.AutoInstrumentation.PluginApi.OpAmp;
using OpenTelemetry.AutoInstrumentation.Plugins;
using OpenTelemetry.OpAmp.Client;
using OpenTelemetry.OpAmp.Client.Listeners;
using OpenTelemetry.OpAmp.Client.Messages;
using OpenTelemetry.OpAmp.Client.Settings;
using OpenTelemetry.Resources;

namespace OpenTelemetry.AutoInstrumentation.OpAmp;

internal sealed class OpAmpManager : IDisposable
{
    private static readonly IOtelLogger Logger = OtelLogging.GetLogger("OpAmp");
    private readonly CancellationTokenSource _startupCancellationSource = new();
    private readonly TaskCompletionSource<bool> _forcedShutdownCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly object _pluginLifecycleLock = new();
    private readonly OpAmpClientTransport _clientTransport;
    private readonly IOpAmpPlugin? _plugin;

    private Task? _startupTask;
    private int _shutdownStarted;
    private int _disposed;
    private int _clientDisposed;
    private int _forceShutdownRequested;
    private bool _beforeStopCallbackInvoked;

    private OpAmpManager(IOpAmpPlugin? plugin, OpAmpClientTransport clientTransport)
    {
        _plugin = plugin;
        _clientTransport = clientTransport;
    }

    public void StartClient()
    {
        // Startup is serialized by OpAmpLoader's lifecycle lock.
        if (Volatile.Read(ref _shutdownStarted) != 0 || _startupTask != null)
        {
            return;
        }

        _startupTask = StartClientCoreAsync();
    }

    public Task StopOpAmpClientAsync()
    {
        if (Interlocked.Exchange(ref _shutdownStarted, 1) != 0)
        {
            return Task.CompletedTask;
        }

        // Cancellation propagation and plugin callbacks may block synchronously.
        return Task.Run(StopOpAmpClientCoreAsync);
    }

    public void Dispose()
    {
        Interlocked.Exchange(ref _shutdownStarted, 1);

        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        CancelStartup();

        try
        {
            ForceDisposeClient();
        }
        finally
        {
            _startupCancellationSource.Dispose();
        }
    }

    internal static bool TryCreate(
        Resource resources,
        OpAmpSettings opAmpSettings,
        PluginManager pluginManager,
        [NotNullWhen(true)] out OpAmpManager? manager)
    {
        OpAmpManager? candidate = null;
        OpAmpClientTransport? clientTransport = null;
        try
        {
            var plugin = SelectPlugin(pluginManager);
            clientTransport = CreateClientTransport(resources, opAmpSettings, plugin);
            candidate = new OpAmpManager(plugin, clientTransport);
            plugin?.ConfigureOpAmpClient(new PluginOpAmpClient(candidate));
            manager = candidate;
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "An error occurred while initializing the OpAmp client.");

            try
            {
                if (candidate != null)
                {
                    candidate.Dispose();
                }
                else
                {
                    clientTransport?.Dispose();
                }
            }
            catch (Exception disposeException)
            {
                Logger.Error(disposeException, "An error occurred while disposing an OpAmp client after initialization failed.");
            }

            manager = null;
            return false;
        }
    }

    internal void ForceDisposeClient()
    {
        if (Interlocked.Exchange(ref _clientDisposed, 1) == 0)
        {
            _clientTransport.Dispose();
        }
    }

    internal Task RequestForcedShutdown()
    {
        if (Interlocked.Exchange(ref _forceShutdownRequested, 1) != 0)
        {
            return _forcedShutdownCompletion.Task;
        }

        // The loader's deadline also bounds a blocked lifecycle callback or client disposal.
        _ = Task.Factory.StartNew(
            () =>
            {
                try
                {
                    InvokeBeforeStopCallback();
                    ForceDisposeClient();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "An error occurred while forcefully disposing the OpAmp client.");
                }
                finally
                {
                    _forcedShutdownCompletion.TrySetResult(true);
                }
            },
            CancellationToken.None,
            TaskCreationOptions.DenyChildAttach | TaskCreationOptions.LongRunning,
            TaskScheduler.Default);

        return _forcedShutdownCompletion.Task;
    }

    internal void Subscribe<T>(IOpAmpListener<T> listener)
        where T : OpAmpMessage
    {
        _clientTransport.Subscribe(listener);
    }

    internal void Unsubscribe<T>(IOpAmpListener<T> listener)
        where T : OpAmpMessage
    {
        _clientTransport.Unsubscribe(listener);
    }

    private static IOpAmpPlugin? SelectPlugin(PluginManager pluginManager)
    {
        var opAmpPlugins = pluginManager.GetOpAmpPlugins();
        if (opAmpPlugins.Count == 0)
        {
            return null;
        }

        if (opAmpPlugins.Count > 1)
        {
            var ignoredPluginNames = string.Join(", ", opAmpPlugins.Skip(1).Select(plugin => plugin.Type.FullName ?? plugin.Type.Name));
            Logger.Warning(
                "Multiple OpAMP plugins are configured. Using '{0}' and ignoring the following plugins for OpAMP: {1}.",
                opAmpPlugins[0].Type.FullName ?? opAmpPlugins[0].Type.Name,
                ignoredPluginNames);
        }

        return opAmpPlugins[0].Instance;
    }

    private static OpAmpClientTransport CreateClientTransport(Resource resources, OpAmpSettings opAmpSettings, IOpAmpPlugin? plugin)
    {
        var client = new OpAmpClient(settings => ConfigureClient(settings, opAmpSettings, resources, plugin));
        return new OpAmpClientTransport(client);
    }

    private static void ConfigureClient(
        OpAmpClientSettings settings,
        OpAmpSettings opAmpSettings,
        Resource resources,
        IOpAmpPlugin? plugin)
    {
        OpAmpClientSettingsConfigurator.ConfigureDefaults(settings, opAmpSettings, resources);

        plugin?.ConfigureOpAmpOptions(settings);

        // Do not allow the client to advertise reporting capabilities
        // that this manager cannot currently fulfill.
        settings.EffectiveConfigurationReporting.EnableReporting = false;
        settings.RemoteConfiguration.ReportsRemoteConfigStatus = false;
    }

    private async Task StartClientCoreAsync()
    {
        var startupCancellationToken = _startupCancellationSource.Token;

        try
        {
            await _clientTransport.StartAsync(startupCancellationToken).ConfigureAwait(false);

            lock (_pluginLifecycleLock)
            {
                if (Volatile.Read(ref _shutdownStarted) != 0)
                {
                    return;
                }

                try
                {
                    _plugin?.AfterOpAmpClientStarted();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "An error occurred in an OpAmp plugin after starting the client.");
                }
            }
        }
        catch (OperationCanceledException) when (startupCancellationToken.IsCancellationRequested)
        {
            // Cancellation is expected when shutdown starts while the client is connecting.
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "OpAmp client stopped unexpectedly.");
        }
    }

    private async Task StopOpAmpClientCoreAsync()
    {
        var startupTask = _startupTask;

        // The lifecycle lock serializes this callback with the post-start callback. If startup
        // has not claimed activation, _shutdownStarted prevents it from doing so afterward.
        InvokeBeforeStopCallback();
        CancelStartup();

        if (startupTask != null)
        {
            try
            {
                await startupTask.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "An error occurred while completing OpAmp startup during shutdown.");
            }
        }

        if (startupTask == null || Volatile.Read(ref _forceShutdownRequested) != 0)
        {
            DisposeClientSafely();
            return;
        }

        await StopTransportSafelyAsync().ConfigureAwait(false);
    }

    private void DisposeClientSafely()
    {
        try
        {
            ForceDisposeClient();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "An error occurred while disposing the OpAmp client.");
        }
    }

    private async Task StopTransportSafelyAsync()
    {
        try
        {
            await _clientTransport.StopAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "An error occurred while stopping the OpAmp client.");
        }
    }

    private void InvokeBeforeStopCallback()
    {
        lock (_pluginLifecycleLock)
        {
            if (_beforeStopCallbackInvoked)
            {
                return;
            }

            _beforeStopCallbackInvoked = true;
            try
            {
                _plugin?.BeforeOpAmpClientStopped();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "An error occurred in an OpAmp plugin before stopping the client.");
            }
        }
    }

    private void CancelStartup()
    {
        try
        {
            _startupCancellationSource.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // Cancellation may race with forced or repeated disposal.
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "An error occurred while cancelling OpAmp client startup.");
        }
    }
}
