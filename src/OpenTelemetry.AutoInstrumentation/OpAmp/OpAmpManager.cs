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
    private readonly OpAmpClientProxy _clientProxy;
    private readonly IOpAmpPlugin? _plugin;
    private readonly OpAmpClientSettings _settings;

    private Task? _startupTask;
    private int _shutdownStarted;
    private int _disposed;
    private int _clientDisposed;
    private int _forceShutdownRequested;
    private bool _beforeStopCallbackInvoked;

    private OpAmpManager(IOpAmpPlugin? plugin, OpAmpClientProxy clientProxy, OpAmpClientSettings settings)
    {
        _plugin = plugin;
        _clientProxy = clientProxy;
        _settings = settings;
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
        try
        {
            var plugin = SelectPlugin(pluginManager);
            candidate = CreateManager(resources, opAmpSettings, plugin);
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
            _clientProxy.Dispose();
        }
    }

    internal Task RequestForcedShutdown()
    {
        if (Interlocked.Exchange(ref _forceShutdownRequested, 1) != 0)
        {
            return _forcedShutdownCompletion.Task;
        }

        // Forced disposal must not wait for lifecycle callbacks or startup cancellation because
        // either can execute plugin-controlled code after the loader's shutdown deadline.
        _ = Task.Factory.StartNew(
            () =>
            {
                try
                {
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
        _clientProxy.Subscribe(listener);
    }

    internal void Unsubscribe<T>(IOpAmpListener<T> listener)
        where T : OpAmpMessage
    {
        _clientProxy.Unsubscribe(listener);
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

    private static OpAmpManager CreateManager(Resource resources, OpAmpSettings opAmpSettings, IOpAmpPlugin? plugin)
    {
        OpAmpClientSettings? configuredSettings = null;
        var client = new OpAmpClient(settings =>
        {
            ConfigureClient(settings, opAmpSettings, resources, plugin);
            configuredSettings = settings;
        });

        return new OpAmpManager(plugin, new OpAmpClientProxy(client), configuredSettings!);
    }

    private static void ConfigureClient(
        OpAmpClientSettings settings,
        OpAmpSettings opAmpSettings,
        Resource resources,
        IOpAmpPlugin? plugin)
    {
        OpAmpClientSettingsConfigurator.ConfigureDefaults(settings, opAmpSettings, resources);

        plugin?.ConfigureOpAmpOptions(settings);
    }

    private async Task StartClientCoreAsync()
    {
        var startupCancellationToken = _startupCancellationSource.Token;

        try
        {
            // Do not advertise capabilities that automatic instrumentation cannot fulfill.
            _settings.EffectiveConfigurationReporting.EnableReporting = false;
            _settings.RemoteConfiguration.AcceptsRemoteConfig = false;
            _settings.RemoteConfiguration.ReportsRemoteConfigStatus = false;

            await _clientProxy.StartAsync(startupCancellationToken).ConfigureAwait(false);

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
            await _clientProxy.StopAsync().ConfigureAwait(false);
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
