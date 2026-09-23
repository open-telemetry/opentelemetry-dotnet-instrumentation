// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Logging;
using OpenTelemetry.OpAmp.Client.Messages;

namespace OpenTelemetry.AutoInstrumentation.OpAmp;

internal sealed class OpAmpReportProcessor : IOpAmpReportingProcessor
{
    private static readonly IOtelLogger Logger = OtelLogging.GetLogger("OpAmp");
    private readonly CustomCapabilitiesCoordinator _customCapabilities;
    private readonly EffectiveConfigReportingState _effectiveConfig;
    private readonly OpAmpClientTransport _clientTransport;
    private readonly RemoteConfigStatusReportingState _remoteConfigStatus;
    private readonly ServerSentCapabilitiesState _serverCapabilities;

    public OpAmpReportProcessor(
        EffectiveConfigReportingState effectiveConfig,
        RemoteConfigStatusReportingState remoteConfigStatus,
        ServerSentCapabilitiesState serverCapabilities,
        CustomCapabilitiesCoordinator customCapabilities,
        OpAmpClientTransport clientTransport)
    {
        _effectiveConfig = effectiveConfig;
        _remoteConfigStatus = remoteConfigStatus;
        _serverCapabilities = serverCapabilities;
        _customCapabilities = customCapabilities;
        _clientTransport = clientTransport;
    }

    public void Process(OpAmpReportingRequests requests)
    {
        if ((requests & OpAmpReportingRequests.FullState) != 0)
        {
            BuildAndSubmitFullStateReport();
            return;
        }

        if ((requests & OpAmpReportingRequests.CustomCapabilities) != 0)
        {
            SubmitCustomCapabilities();
        }

        if ((requests & OpAmpReportingRequests.EffectiveConfig) != 0)
        {
            ReportEffectiveConfig();
        }

        if ((requests & OpAmpReportingRequests.RemoteConfigStatus) != 0)
        {
            ReportRemoteConfigStatus();
        }
    }

    private void SubmitCustomCapabilities()
    {
        try
        {
            _customCapabilities.SubmitClientCapabilitiesIfChanged();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "An error occurred while reporting OpAmp custom capabilities.");
        }
    }

    private void BuildAndSubmitFullStateReport()
    {
        var report = new FullStateReport();
        var serverCapabilities = _serverCapabilities.GetSnapshot();
        if (_effectiveConfig.Enabled &&
            (serverCapabilities & ServerSentCapabilities.AcceptsEffectiveConfig) != 0)
        {
            _effectiveConfig.TryUpdateSnapshot();
            if (_effectiveConfig.TryGetSnapshot(out var effectiveConfig))
            {
                report.EffectiveConfigFiles = effectiveConfig;
            }
        }

        if (_remoteConfigStatus.Enabled &&
            (serverCapabilities & ServerSentCapabilities.OffersRemoteConfig) != 0)
        {
            _remoteConfigStatus.TryUpdateSnapshot();
            if (_remoteConfigStatus.TryGetSnapshot(out var remoteConfigStatus))
            {
                report.RemoteConfigStatus = remoteConfigStatus;
            }
        }

        try
        {
            _customCapabilities.SubmitFullStateWithCapabilities(report);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "An error occurred while sending an OpAmp full-state report.");
        }
    }

    private void ReportEffectiveConfig()
    {
        if (!_effectiveConfig.Enabled ||
            (_serverCapabilities.GetSnapshot() & ServerSentCapabilities.AcceptsEffectiveConfig) == 0)
        {
            return;
        }

        if (_effectiveConfig.TryUpdateSnapshot() &&
            _effectiveConfig.TryGetSnapshot(out var effectiveConfig))
        {
            try
            {
                _clientTransport.SendEffectiveConfig(effectiveConfig);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "An error occurred while sending OpAmp effective configuration.");
            }
        }
    }

    private void ReportRemoteConfigStatus()
    {
        if (!_remoteConfigStatus.Enabled ||
            (_serverCapabilities.GetSnapshot() & ServerSentCapabilities.OffersRemoteConfig) == 0)
        {
            return;
        }

        if (_remoteConfigStatus.TryUpdateSnapshot() &&
            _remoteConfigStatus.TryGetSnapshot(out var remoteConfigStatus))
        {
            try
            {
                _clientTransport.SendRemoteConfigStatus(remoteConfigStatus);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "An error occurred while sending OpAmp remote configuration status.");
            }
        }
    }
}
