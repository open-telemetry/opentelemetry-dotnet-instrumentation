// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Logging;
using OpenTelemetry.AutoInstrumentation.PluginApi.OpAmp;
using OpenTelemetry.OpAmp.Client.Listeners;
using OpenTelemetry.OpAmp.Client.Messages;

namespace OpenTelemetry.AutoInstrumentation.OpAmp;

internal sealed class EffectiveConfigReporter : IOpAmpListener<ServerCapabilitiesMessage>, IOpAmpListener<FlagsMessage>
{
    private static readonly IOtelLogger Logger = OtelLogging.GetLogger("OpAmp");
    private readonly IProvideEffectiveConfig _plugin;
    private readonly IOpAmpReportSender _reportSender;
    private readonly object _workLock = new();

    // Protected by _workLock: pending work, scheduling and server support.
    private Task _drainTask = Task.CompletedTask;
    private ReportingRequests _pendingRequests;
    private bool _drainScheduled;
    private bool _serverAcceptsEffectiveConfig;
    private int _effectiveConfigSupportRevision;

    // Forced shutdown also sets this flag without acquiring _workLock.
    private volatile bool _stopped;

    public EffectiveConfigReporter(IProvideEffectiveConfig plugin, IOpAmpReportSender reportSender)
    {
        _plugin = plugin;
        _reportSender = reportSender;
    }

    [Flags]
    private enum ReportingRequests
    {
        None = 0,
        EffectiveConfig = 1,
        FullState = 2
    }

    public void NotifyEffectiveConfigChanged()
    {
        if (_stopped)
        {
            return;
        }

        lock (_workLock)
        {
            if (_serverAcceptsEffectiveConfig)
            {
                RequestReport(ReportingRequests.EffectiveConfig);
            }
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

        lock (_workLock)
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
            // Invalidate reads that started before this support change.
            _effectiveConfigSupportRevision++;
            if (acceptsEffectiveConfig)
            {
                RequestReport(ReportingRequests.EffectiveConfig);
            }
        }
    }

    public void ReportFullState()
    {
        if (_stopped)
        {
            return;
        }

        lock (_workLock)
        {
            RequestReport(ReportingRequests.FullState);
        }
    }

    public void Stop()
    {
        if (_stopped)
        {
            return;
        }

        lock (_workLock)
        {
            // Shutdown discards pending work without waiting for a running plugin getter.
            _stopped = true;
            _pendingRequests = ReportingRequests.None;
        }
    }

    internal void ForceStop()
    {
        // Reach client disposal without waiting for a submission holding _workLock.
        _stopped = true;
    }

    internal Task WaitForIdleAsync()
    {
        lock (_workLock)
        {
            return _drainTask;
        }
    }

    // Called under _workLock; plugin getters run outside it.
    private void RequestReport(ReportingRequests requests)
    {
        if (_stopped)
        {
            return;
        }

        _pendingRequests |= requests;
        if (!_drainScheduled)
        {
            _drainScheduled = true;
            // Schedule configuration reads separately so OpAMP callbacks do not wait for the getter.
            _drainTask = Task.Run(Drain);
        }
    }

    private void Drain()
    {
        while (true)
        {
            ReportingRequests requests;
            bool acceptsEffectiveConfig;
            int effectiveConfigSupportRevision;
            lock (_workLock)
            {
                if (_stopped || _pendingRequests == ReportingRequests.None)
                {
                    _pendingRequests = ReportingRequests.None;
                    _drainScheduled = false;
                    return;
                }

                requests = _pendingRequests;
                _pendingRequests = ReportingRequests.None;
                acceptsEffectiveConfig = _serverAcceptsEffectiveConfig;
                effectiveConfigSupportRevision = _effectiveConfigSupportRevision;
            }

            Report(requests, acceptsEffectiveConfig, effectiveConfigSupportRevision);
        }
    }

    private void Report(ReportingRequests requests, bool acceptsEffectiveConfig, int effectiveConfigSupportRevision)
    {
        var fullState = (requests & ReportingRequests.FullState) != 0;
        if (!fullState && !acceptsEffectiveConfig)
        {
            return;
        }

        try
        {
            EffectiveConfigFile[]? files = null;
            if (acceptsEffectiveConfig)
            {
                // Copy the plugin's buffers so later mutations cannot change the queued report.
                files = _plugin.GetEffectiveConfig()
                    .Select(file => new EffectiveConfigFile(file.Content.ToArray(), file.ContentType, file.FileName))
                    .ToArray();
            }

            lock (_workLock)
            {
                if (_stopped)
                {
                    return;
                }

                if (effectiveConfigSupportRevision != _effectiveConfigSupportRevision)
                {
                    // Retry with current capabilities, retaining any full-state request.
                    RequestReport(requests);
                    return;
                }

                // Serialize validation and SDK queueing with capability changes and graceful shutdown.
                if (fullState)
                {
                    _reportSender.SendFullStateReport(new FullStateReport { EffectiveConfigFiles = files });
                }
                else
                {
                    _reportSender.SendEffectiveConfig(files!);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "An error occurred while reporting OpAmp effective configuration.");
        }
    }
}
