// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics.CodeAnalysis;
using OpenTelemetry.AutoInstrumentation.Configurations;
using OpenTelemetry.AutoInstrumentation.Logging;
using OpenTelemetry.AutoInstrumentation.OpAmp.Listeners;
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
    private readonly object _pluginLifecycleLock = new();
    private readonly OpAmpClientTransport _clientTransport = new();
    private readonly CustomCapabilitiesCoordinator _customCapabilities;
    private readonly EffectiveConfigReportingState _effectiveConfigReportingState;
    private readonly RemoteConfigStatusReportingState _remoteConfigStatusReportingState;
    private readonly OpAmpReportingWorker _reportingWorker;
    private readonly ServerSentCapabilitiesState _serverCapabilitiesState = new();
    private readonly IOpAmpPlugin? _plugin;
    private readonly CapabilitiesListener _capabilitiesListener;
    private readonly FlagsMessageListener _flagsListener;

    private Task? _startupTask;
    // Startup is serialized by OpAmpLoader's lifecycle lock.
    private bool _startupStarted;
    private int _shutdownStarted;
    private int _disposed;
    private int _clientDisposed;
    private int _clientStarted;

    private OpAmpManager(PluginManager pluginManager)
    {
        var opAmpPlugins = pluginManager.GetOpAmpPlugins();
        if (opAmpPlugins.Count > 0)
        {
            _plugin = opAmpPlugins[0].Instance;

            if (opAmpPlugins.Count > 1)
            {
                var ignoredPluginNames = string.Join(", ", opAmpPlugins.Skip(1).Select(plugin => plugin.Type.FullName ?? plugin.Type.Name));
                Logger.Warning(
                    "Multiple OpAMP plugins are configured. Using '{0}' and ignoring the following plugins for OpAMP: {1}.",
                    opAmpPlugins[0].Type.FullName ?? opAmpPlugins[0].Type.Name,
                    ignoredPluginNames);
            }
        }

        _effectiveConfigReportingState = new EffectiveConfigReportingState(_plugin as IProvideEffectiveConfig);
        _remoteConfigStatusReportingState = new RemoteConfigStatusReportingState(_plugin as IProvideRemoteConfigStatus);
        _customCapabilities = new CustomCapabilitiesCoordinator(_clientTransport);
        var reportProcessor = new OpAmpReportProcessor(
            _effectiveConfigReportingState,
            _remoteConfigStatusReportingState,
            _serverCapabilitiesState,
            _customCapabilities,
            _clientTransport);
        _reportingWorker = new OpAmpReportingWorker(reportProcessor);
        _capabilitiesListener = new CapabilitiesListener(this);
        _flagsListener = new FlagsMessageListener(this);
    }

    public void StartClient()
    {
        if (Volatile.Read(ref _shutdownStarted) != 0 ||
            _startupStarted)
        {
            return;
        }

        _startupStarted = true;
        _startupTask = StartClientCoreAsync();
    }

    public Task StopOpAmpClientAsync()
    {
        if (Interlocked.Exchange(ref _shutdownStarted, 1) != 0)
        {
            return Task.CompletedTask;
        }

        // Cancellation propagation and plugin callbacks may block synchronously, so keep them off the loader thread.
        return Task.Run(StopOpAmpClientCoreAsync);
    }

    public void Dispose()
    {
        // Suppress post-start activation before disposal can race with startup completion.
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
            candidate = new OpAmpManager(pluginManager);
            candidate.PrepareClient(resources, opAmpSettings);
            manager = candidate;
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "An error occurred while initializing the OpAmp client.");

            try
            {
                candidate?.Dispose();
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
        var disposeClient = Interlocked.Exchange(ref _clientDisposed, 1) == 0;
        _reportingWorker.Abort();
        if (disposeClient)
        {
            _clientTransport.Dispose();
        }
    }

    internal void HandleServerCapabilities(ServerSentCapabilities capabilities)
    {
        var newlyEnabled = _serverCapabilitiesState.UpdateAndGetNewlyEnabled(capabilities);
        var requests = OpAmpReportingRequests.None;

        if (_effectiveConfigReportingState.Enabled &&
            (newlyEnabled & ServerSentCapabilities.AcceptsEffectiveConfig) != 0)
        {
            requests |= OpAmpReportingRequests.EffectiveConfig;
        }

        if (_remoteConfigStatusReportingState.Enabled &&
            (newlyEnabled & ServerSentCapabilities.OffersRemoteConfig) != 0)
        {
            requests |= OpAmpReportingRequests.RemoteConfigStatus;
        }

        _reportingWorker.Request(requests);
    }

    internal void HandleServerCustomCapabilities(ICollection<string> capabilities)
    {
        _customCapabilities.UpdateServerCapabilities(capabilities);
    }

    internal void ReportCustomCapabilities(IReadOnlyCollection<string> capabilities)
    {
        if (_customCapabilities.UpdateClientCapabilities(capabilities) == CustomCapabilitiesReportRequirement.Required)
        {
            RequestCustomCapabilitiesReport();
        }
    }

    internal void NotifyEffectiveConfigChanged()
    {
        if (!_effectiveConfigReportingState.Enabled)
        {
            return;
        }

        _reportingWorker.Request(OpAmpReportingRequests.EffectiveConfig);
    }

    internal void NotifyRemoteConfigStatusChanged()
    {
        if (!_remoteConfigStatusReportingState.Enabled)
        {
            return;
        }

        _reportingWorker.Request(OpAmpReportingRequests.RemoteConfigStatus);
    }

    internal void SendCustomMessage(string capability, string type, ReadOnlyMemory<byte> data)
    {
        var eligibility = _customCapabilities.SendCustomMessageIfEligible(capability, type, data);

        if (eligibility == CustomMessageEligibility.Allowed)
        {
            return;
        }

        if (eligibility == CustomMessageEligibility.CapabilityPublicationPending)
        {
            Logger.Error("Cannot send custom message. Capability '{0}' has not been submitted to the upstream OpAmp client.", capability);
            return;
        }

        if (eligibility == CustomMessageEligibility.CapabilityNotReported)
        {
            Logger.Error("Cannot send custom message. Capability '{0}' is not supported.", capability);
            return;
        }

        Logger.Error("Cannot send custom message. Server does not support capability: {0}.", capability);
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

    internal void RequestFullStateReport()
    {
        _reportingWorker.Request(OpAmpReportingRequests.FullState);
    }

    internal async Task FlushAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (Volatile.Read(ref _clientStarted) == 0 ||
            Volatile.Read(ref _shutdownStarted) != 0)
        {
            throw new InvalidOperationException("The OpAmp client is not running.");
        }

        await _reportingWorker.WaitForAcceptedWorkAsync(cancellationToken).ConfigureAwait(false);

        await _clientTransport.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private void PrepareClient(Resource resources, OpAmpSettings opAmpSettings)
    {
        var client = new OpAmpClient(settings => ConfigureClient(settings, opAmpSettings, resources));
        _clientTransport.Initialize(client);

        _clientTransport.Subscribe<ServerCapabilitiesMessage>(_capabilitiesListener);
        _clientTransport.Subscribe<CustomCapabilitiesMessage>(_capabilitiesListener);
        _clientTransport.Subscribe(_flagsListener);

        _plugin?.ConfigureOpAmpClient(new PluginOpAmpClient(this));
    }

    private async Task StartClientCoreAsync()
    {
        var startupCancellationToken = _startupCancellationSource.Token;

        try
        {
            var startupTask = _clientTransport.StartAsync(startupCancellationToken);

            await startupTask.ConfigureAwait(false);

            lock (_pluginLifecycleLock)
            {
                // This read linearizes successful activation with a concurrent shutdown request.
                if (Volatile.Read(ref _shutdownStarted) == 0)
                {
                    if (_customCapabilities.StartClientReporting() == CustomCapabilitiesReportRequirement.Required)
                    {
                        RequestCustomCapabilitiesReport();
                    }

                    Volatile.Write(ref _clientStarted, 1);

                    // Notify plugins that the prepared OpAmp client has started successfully.
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

    private void RequestCustomCapabilitiesReport()
    {
        try
        {
            _reportingWorker.Request(OpAmpReportingRequests.CustomCapabilities);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "An error occurred while reporting OpAmp custom capabilities.");
        }
    }

    private async Task StopOpAmpClientCoreAsync()
    {
        CancelStartup();

        Task pendingReportingWork;
        lock (_pluginLifecycleLock)
        {
            // If startup claimed activation, wait until cached capability reporting and the
            // post-start callback have completed before closing the reporting worker.
            pendingReportingWork = _reportingWorker.StopAcceptingAsync();
        }

        try
        {
            await pendingReportingWork.ConfigureAwait(false);
        }
        catch (ObjectDisposedException) when (Volatile.Read(ref _clientDisposed) != 0 || Volatile.Read(ref _disposed) != 0)
        {
            // Forceful disposal abandons reporting work that could not finish before the deadline.
        }

        lock (_pluginLifecycleLock)
        {
            try
            {
                _plugin?.BeforeOpAmpClientStopped();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "An error occurred in an OpAmp plugin before stopping the client.");
            }
        }

        try
        {
            if (_startupTask != null)
            {
                await _startupTask.ConfigureAwait(false);
            }

            _clientTransport.Unsubscribe<ServerCapabilitiesMessage>(_capabilitiesListener);
            _clientTransport.Unsubscribe<CustomCapabilitiesMessage>(_capabilitiesListener);
            _clientTransport.Unsubscribe(_flagsListener);

            await _clientTransport.StopAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "An error occurred while stopping the OpAmp client.");
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
    }

    private void ConfigureClient(OpAmpClientSettings settings, OpAmpSettings opAmpSettings, Resource resources)
    {
        OpAmpClientSettingsConfigurator.ConfigureDefaults(settings, opAmpSettings, resources);

        settings.EffectiveConfigurationReporting.EnableReporting = false;
        settings.RemoteConfiguration.ReportsRemoteConfigStatus = false;

        // Provider-backed reporting requires an explicit opt-in from the selected plugin.
        _plugin?.ConfigureOpAmpOptions(settings);

        // Providers define which requested capabilities the manager can fulfill.
        _effectiveConfigReportingState.SetEnabled(settings.EffectiveConfigurationReporting.EnableReporting);
        _remoteConfigStatusReportingState.SetEnabled(settings.RemoteConfiguration.ReportsRemoteConfigStatus);
        settings.EffectiveConfigurationReporting.EnableReporting = _effectiveConfigReportingState.Enabled;
        settings.RemoteConfiguration.ReportsRemoteConfigStatus = _remoteConfigStatusReportingState.Enabled;
    }
}
