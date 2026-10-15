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
    private readonly IOpAmpReportSender _reportSender;
    private readonly object _reportLock = new();

    // Protected by _reportLock, together with configuration reads and submissions.
    private bool _serverAcceptsEffectiveConfig;

    // Shutdown sets this flag without waiting for the reporting lock.
    private volatile bool _stopped;

    public OpAmpReporter(IProvideEffectiveConfig? effectiveConfigProvider, IOpAmpReportSender reportSender)
    {
        _effectiveConfigProvider = effectiveConfigProvider;
        _reportSender = reportSender;
    }

    public bool EffectiveConfigReportingEnabled => _effectiveConfigProvider != null;

    public void NotifyEffectiveConfigChanged()
    {
        if (_stopped || !EffectiveConfigReportingEnabled)
        {
            return;
        }

        lock (_reportLock)
        {
            Report(fullState: false);
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

            var acceptsEffectiveConfig = (capabilities & ServerSentCapabilities.AcceptsEffectiveConfig) != 0;
            if (acceptsEffectiveConfig == _serverAcceptsEffectiveConfig)
            {
                return;
            }

            _serverAcceptsEffectiveConfig = acceptsEffectiveConfig;
            if (_serverAcceptsEffectiveConfig)
            {
                Report(fullState: false);
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
            Report(fullState: true);
        }
    }

    public void Stop()
    {
        // Signal stopping without waiting for an active getter or submission.
        _stopped = true;
    }

    // Called under _reportLock to order capability changes with local SDK submission.
    private void Report(bool fullState)
    {
        if (_stopped || (!fullState && (!EffectiveConfigReportingEnabled || !_serverAcceptsEffectiveConfig)))
        {
            return;
        }

        try
        {
            EffectiveConfigFile[]? files = null;
            if (_effectiveConfigProvider != null && _serverAcceptsEffectiveConfig)
            {
                try
                {
                    // Copy the plugin's buffers so later mutations cannot change the queued report.
                    files = _effectiveConfigProvider.GetEffectiveConfig()
                        .Select(file => new EffectiveConfigFile(file.Content.ToArray(), file.ContentType, file.FileName))
                        .ToArray();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "An error occurred while preparing effective configuration for OpAmp reporting.");
                    if (!fullState)
                    {
                        return;
                    }

                    // Omit unavailable configuration so independent state can still be reported.
                }
            }

            if (_stopped)
            {
                return;
            }

            // Shutdown may begin after this check; an admitted submission is allowed to finish.
            if (fullState)
            {
                _reportSender.SendFullStateReport(new FullStateReport { EffectiveConfigFiles = files });
            }
            else
            {
                _reportSender.SendEffectiveConfig(files!);
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "An error occurred while reporting OpAmp state.");
        }
    }
}
