// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Logging;
using OpenTelemetry.AutoInstrumentation.PluginApi.OpAmp;
using OpenTelemetry.OpAmp.Client.Listeners;
using OpenTelemetry.OpAmp.Client.Messages;

namespace OpenTelemetry.AutoInstrumentation.OpAmp;

internal sealed class OpAmpReporter : IOpAmpListener<ServerCapabilitiesMessage>, IOpAmpListener<FlagsMessage>
{
    private static readonly IOtelLogger Logger = OtelLogging.GetLogger("OpAmp");
    private readonly IProvideEffectiveConfig? _effectiveConfigProvider;
    private readonly IProvideRemoteConfigStatus? _remoteConfigStatusProvider;
    private readonly IOpAmpReportSender _reportSender;
    private readonly object _reportLock = new();

    // Protected by _reportLock, together with configuration reads and submissions.
    private ServerSentCapabilities _serverCapabilities;

    // Shutdown sets this flag without waiting for the reporting lock.
    private volatile bool _stopped;

    public OpAmpReporter(
        IProvideEffectiveConfig? effectiveConfigProvider,
        IProvideRemoteConfigStatus? remoteConfigStatusProvider,
        IOpAmpReportSender reportSender)
    {
        _effectiveConfigProvider = effectiveConfigProvider;
        _remoteConfigStatusProvider = remoteConfigStatusProvider;
        _reportSender = reportSender;
    }

    public bool EffectiveConfigReportingEnabled => _effectiveConfigProvider != null;

    public bool RemoteConfigStatusReportingEnabled => _remoteConfigStatusProvider != null;

    public void NotifyEffectiveConfigChanged()
    {
        if (_stopped || !EffectiveConfigReportingEnabled)
        {
            return;
        }

        lock (_reportLock)
        {
            ReportEffectiveConfig();
        }
    }

    public void NotifyRemoteConfigStatusChanged()
    {
        if (_stopped || !RemoteConfigStatusReportingEnabled)
        {
            return;
        }

        lock (_reportLock)
        {
            ReportRemoteConfigStatus();
        }
    }

    public void HandleMessage(ServerCapabilitiesMessage message)
    {
        HandleServerCapabilities(message.Capabilities);
    }

    public void HandleMessage(FlagsMessage message)
    {
        if ((message.Flags & ServerSentFlags.ReportFullState) != 0)
        {
            ReportFullState();
        }
    }

    public void HandleServerCapabilities(ServerSentCapabilities capabilities)
    {
        // OpAMP defines zero capabilities as unspecified; retain previously advertised support.
        if (_stopped || capabilities == ServerSentCapabilities.None)
        {
            return;
        }

        lock (_reportLock)
        {
            if (_stopped)
            {
                return;
            }

            var newlyEnabled = capabilities & ~_serverCapabilities;
            // Each nonzero advertisement replaces the previous set, including revoked capabilities.
            _serverCapabilities = capabilities;

            if ((newlyEnabled & ServerSentCapabilities.AcceptsEffectiveConfig) != 0)
            {
                ReportEffectiveConfig();
            }

            // Report any status already available; null is omitted.
            // The plugin notifies us as configuration application progresses.
            if ((newlyEnabled & ServerSentCapabilities.OffersRemoteConfig) != 0)
            {
                ReportRemoteConfigStatus();
            }
        }
    }

    public void ReportFullState()
    {
        if (_stopped)
        {
            return;
        }

        lock (_reportLock)
        {
            ReportFullStateCore();
        }
    }

    public void Stop()
    {
        // Signal stopping without waiting for an active getter or submission.
        _stopped = true;
    }

    // Called under _reportLock to order capability changes with local SDK submission.
    private void ReportEffectiveConfig()
    {
        var files = ReadEffectiveConfig();
        if (_stopped || files == null)
        {
            return;
        }

        try
        {
            // Shutdown may begin after this check; an admitted submission is allowed to finish.
            _reportSender.SendEffectiveConfig(files);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "An error occurred while reporting OpAmp state.");
        }
    }

    private void ReportFullStateCore()
    {
        var files = ReadEffectiveConfig();
        var remoteConfigStatus = ReadRemoteConfigStatus();
        if (_stopped)
        {
            return;
        }

        try
        {
            _reportSender.SendFullStateReport(new FullStateReport
            {
                EffectiveConfigFiles = files,
                RemoteConfigStatus = remoteConfigStatus
            });
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "An error occurred while reporting OpAmp state.");
        }
    }

    // Called under _reportLock
    private void ReportRemoteConfigStatus()
    {
        var report = ReadRemoteConfigStatus();
        if (_stopped || report == null)
        {
            return;
        }

        try
        {
            _reportSender.SendRemoteConfigStatus(report);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "An error occurred while reporting OpAmp remote configuration status.");
        }
    }

    private EffectiveConfigFile[]? ReadEffectiveConfig()
    {
        if (_stopped || _effectiveConfigProvider == null ||
            (_serverCapabilities & ServerSentCapabilities.AcceptsEffectiveConfig) == 0)
        {
            return null;
        }

        try
        {
            // Copy the plugin's buffers so later mutations cannot change the queued report.
            return _effectiveConfigProvider.GetEffectiveConfig()
                .Select(file => new EffectiveConfigFile(file.Content.ToArray(), file.ContentType, file.FileName))
                .ToArray();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "An error occurred while preparing effective configuration for OpAmp reporting.");
            return null;
        }
    }

    private RemoteConfigStatusReport? ReadRemoteConfigStatus()
    {
        // Status describes previously received configuration, regardless of current server offers.
        if (_stopped || _remoteConfigStatusProvider == null)
        {
            return null;
        }

        try
        {
            var report = _remoteConfigStatusProvider.GetRemoteConfigStatus();
            // Copy the hash so later mutations cannot change the queued report.
            return report == null ? null : new RemoteConfigStatusReport(report.LastRemoteConfigHash.Span, report.Status, report.ErrorMessage);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "An error occurred while preparing remote configuration status for OpAmp reporting.");
            return null;
        }
    }
}
