// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Logging;
using OpenTelemetry.AutoInstrumentation.PluginApi.OpAmp;
using OpenTelemetry.OpAmp.Client.Listeners;
using OpenTelemetry.OpAmp.Client.Messages;

namespace OpenTelemetry.AutoInstrumentation.OpAmp;

internal sealed class OpAmpReporter : IOpAmpListener<ServerCapabilitiesMessage>, IOpAmpListener<FlagsMessage>, IDisposable
{
    private static readonly IOtelLogger Logger = OtelLogging.GetLogger("OpAmp");

    // Coalesce server-requested full-state reports and enforce a minimum interval between sends.
    // This is a local safeguard, not an OpAMP-defined timeout.
    private static readonly TimeSpan FullStateReportInterval = TimeSpan.FromSeconds(5);
    private readonly IProvideEffectiveConfig? _effectiveConfigProvider;
    private readonly IProvideRemoteConfigStatus? _remoteConfigStatusProvider;
    private readonly IOpAmpReportSender _reportSender;
    private readonly object _reportLock = new();
    private readonly object _stateLock = new();
    private readonly IOpAmpReportTimer _fullStateTimer;
    private readonly IOpAmpReportTimer _pluginReportTimer;

    // Capability reads are independent of scheduling state and must remain current while a
    // report waits for _reportLock. The remaining fields are protected by _stateLock.
    private volatile bool _serverAcceptsEffectiveConfig;
    private PluginReportKind _pendingPluginReports;
    private bool _fullStatePending;
    private FullStatePhase _fullStatePhase;
    private bool _pluginReportDrainActive;

    // Shutdown sets this flag without waiting for active reporting.
    private volatile bool _stopped;

    public OpAmpReporter(
        IProvideEffectiveConfig? effectiveConfigProvider,
        IProvideRemoteConfigStatus? remoteConfigStatusProvider,
        IOpAmpReportSender reportSender,
        Func<Action, IOpAmpReportTimer>? createTimer = null)
    {
        _effectiveConfigProvider = effectiveConfigProvider;
        _remoteConfigStatusProvider = remoteConfigStatusProvider;
        _reportSender = reportSender;
        createTimer ??= callback => new ReportTimer(callback);
        _fullStateTimer = createTimer(HandleScheduledFullState);
        _pluginReportTimer = createTimer(HandleScheduledPluginReports);
    }

    [Flags]
    private enum PluginReportKind
    {
        None = 0,
        EffectiveConfig = 1,
        RemoteConfigStatus = 2
    }

    private enum FullStatePhase
    {
        Idle,
        Waiting,
        Reporting
    }

    public bool EffectiveConfigReportingEnabled => _effectiveConfigProvider != null;

    public bool RemoteConfigStatusReportingEnabled => _remoteConfigStatusProvider != null;

    public void NotifyEffectiveConfigChanged()
    {
        if (EffectiveConfigReportingEnabled)
        {
            NotifyPluginReport(PluginReportKind.EffectiveConfig);
        }
    }

    public void NotifyRemoteConfigStatusChanged()
    {
        if (RemoteConfigStatusReportingEnabled)
        {
            NotifyPluginReport(PluginReportKind.RemoteConfigStatus);
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
        if (capabilities == ServerSentCapabilities.None)
        {
            return;
        }

        lock (_stateLock)
        {
            if (_stopped)
            {
                return;
            }

            // Each nonzero advertisement replaces the previous support value, including revocation.
            // A capability change is not a request for a report. It can only unblock a notification
            // that the plugin already made.
            _serverAcceptsEffectiveConfig = (capabilities & ServerSentCapabilities.AcceptsEffectiveConfig) != 0;
            TrySchedulePendingPluginReports();
        }
    }

    public void ReportFullState()
    {
        lock (_stateLock)
        {
            if (_stopped)
            {
                return;
            }

            _fullStatePending = true;
            if (_fullStatePhase == FullStatePhase.Idle)
            {
                _fullStateTimer.Schedule(TimeSpan.Zero);
                _fullStatePhase = FullStatePhase.Waiting;
            }
        }
    }

    public void Stop()
    {
        // Scheduling never waits for plugin code or SDK submissions.
        lock (_stateLock)
        {
            if (_stopped)
            {
                return;
            }

            _stopped = true;
            _pendingPluginReports = PluginReportKind.None;
            _fullStatePending = false;
            _fullStatePhase = FullStatePhase.Idle;
            _pluginReportDrainActive = false;
            // Already queued callbacks may still run, but disposal does not wait for them.
            _fullStateTimer.Dispose();
            _pluginReportTimer.Dispose();
        }
    }

    public void Dispose()
    {
        Stop();
    }

    private void NotifyPluginReport(PluginReportKind report)
    {
        lock (_stateLock)
        {
            if (_stopped)
            {
                return;
            }

            _pendingPluginReports |= report;
            TrySchedulePendingPluginReports();
        }
    }

    // Called under _stateLock.
    private PluginReportKind GetEligiblePluginReports(PluginReportKind reports)
    {
        if ((reports & PluginReportKind.EffectiveConfig) != 0 &&
            !_serverAcceptsEffectiveConfig)
        {
            reports &= ~PluginReportKind.EffectiveConfig;
        }

        return reports;
    }

    // Called under _stateLock. Notifications create pending work; capability changes can only
    // make that work eligible. The timer must never invoke the callback inline.
    private void TrySchedulePendingPluginReports()
    {
        if (_pluginReportDrainActive || GetEligiblePluginReports(_pendingPluginReports) == PluginReportKind.None)
        {
            return;
        }

        _pluginReportTimer.Schedule(TimeSpan.Zero);
        _pluginReportDrainActive = true;
    }

    private void HandleScheduledPluginReports()
    {
        var drainClaimed = false;
        try
        {
            PluginReportKind reports;
            lock (_stateLock)
            {
                if (_stopped || !_pluginReportDrainActive)
                {
                    return;
                }

                drainClaimed = true;
                reports = GetEligiblePluginReports(_pendingPluginReports);
                _pendingPluginReports &= ~reports;
            }

            if (reports == PluginReportKind.None)
            {
                return;
            }

            lock (_reportLock)
            {
                ReportPluginState(reports);
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "An unexpected error occurred while reporting OpAmp plugin state.");
        }
        finally
        {
            if (drainClaimed)
            {
                lock (_stateLock)
                {
                    _pluginReportDrainActive = false;
                    if (!_stopped)
                    {
                        TrySchedulePendingPluginReports();
                    }
                }
            }
        }
    }

    private void ReportPluginState(PluginReportKind reports)
    {
        if ((reports & PluginReportKind.EffectiveConfig) != 0)
        {
            ReportEffectiveConfig();
        }

        if ((reports & PluginReportKind.RemoteConfigStatus) != 0)
        {
            ReportRemoteConfigStatus();
        }
    }

    private void HandleScheduledFullState()
    {
        try
        {
            lock (_stateLock)
            {
                if (_stopped || _fullStatePhase != FullStatePhase.Waiting)
                {
                    return;
                }

                if (!_fullStatePending)
                {
                    _fullStatePhase = FullStatePhase.Idle;
                    return;
                }

                _fullStatePending = false;
                _fullStatePhase = FullStatePhase.Reporting;
            }

            ReportScheduledFullState();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "An error occurred while processing scheduled OpAmp full-state reporting.");
        }
    }

    private void ReportScheduledFullState()
    {
        try
        {
            lock (_reportLock)
            {
                ReportFullStateCore();
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "An unexpected error occurred while reporting OpAmp full state.");
        }
        finally
        {
            lock (_stateLock)
            {
                if (_stopped)
                {
                    _fullStatePhase = FullStatePhase.Idle;
                }
                else
                {
                    // Failed or empty attempts consume the cooldown too; they create no retries.
                    _fullStateTimer.Schedule(FullStateReportInterval);
                    _fullStatePhase = FullStatePhase.Waiting;
                }
            }
        }
    }

    private void ReportEffectiveConfig()
    {
        if (!TryUseEffectiveConfigCapability())
        {
            return;
        }

        var files = ReadEffectiveConfig();
        if (files == null || !TryUseEffectiveConfigCapability())
        {
            return;
        }

        try
        {
            // The last capability read is the submission admission point. A later shutdown or
            // capability revocation does not cancel a submission that has already been admitted.
            _reportSender.SendEffectiveConfig(files);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "An error occurred while reporting OpAmp state.");
        }
    }

    private void ReportFullStateCore()
    {
        var files = _serverAcceptsEffectiveConfig ? ReadEffectiveConfig() : null;
        var remoteConfigStatus = ReadRemoteConfigStatus();
        if (_stopped)
        {
            return;
        }

        // Recheck after provider reads so a processed revocation is reflected in the
        // message even when it arrived while a provider was running.
        if (!_serverAcceptsEffectiveConfig)
        {
            files = null;
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
        if (_stopped || _effectiveConfigProvider == null)
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

    private bool TryUseEffectiveConfigCapability()
    {
        if (_serverAcceptsEffectiveConfig && !_stopped)
        {
            return true;
        }

        lock (_stateLock)
        {
            if (_stopped)
            {
                return false;
            }

            if (_serverAcceptsEffectiveConfig)
            {
                return true;
            }

            _pendingPluginReports |= PluginReportKind.EffectiveConfig;
            return false;
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

    private sealed class ReportTimer : IOpAmpReportTimer
    {
        private readonly Timer _timer;

        public ReportTimer(Action callback)
        {
            _timer = new Timer(_ => callback(), null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        }

        public void Schedule(TimeSpan delay)
        {
            _timer.Change(delay, Timeout.InfiniteTimeSpan);
        }

        public void Dispose()
        {
            _timer.Dispose();
        }
    }
}
