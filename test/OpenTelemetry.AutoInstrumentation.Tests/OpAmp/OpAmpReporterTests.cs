// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Text;
using OpenTelemetry.AutoInstrumentation.OpAmp;
using OpenTelemetry.AutoInstrumentation.PluginApi.OpAmp;
using OpenTelemetry.AutoInstrumentation.Tests.Util;
using OpenTelemetry.OpAmp.Client.Messages;
using OpenTelemetry.OpAmp.Client.Settings;

namespace OpenTelemetry.AutoInstrumentation.Tests.OpAmp;

public sealed class OpAmpReporterTests : IDisposable
{
    private const ServerSentCapabilities ServerCapabilities = ServerSentCapabilities.AcceptsStatus | ServerSentCapabilities.AcceptsEffectiveConfig;
    private static readonly TimeSpan ReportInterval = TimeSpan.FromSeconds(5);
    private readonly TestReportingPlugin _plugin = new();
    private readonly RecordingOpAmpReportSender _reportSender = new();
    private readonly List<ManualReportTimer> _timers = [];
    private readonly OpAmpReporter _reporter;
    private long _elapsedTicks;

    public OpAmpReporterTests()
    {
        _reporter = new OpAmpReporter(_plugin, _plugin, _reportSender, CreateTimer);
    }

    [Fact]
    public void ReportsInitialAndNotifiedConfigurationWithCopiedBuffers()
    {
        var initialContent = Encoding.UTF8.GetBytes("initial");
        EffectiveConfigFile[] initialFiles = [new(initialContent, "text/yaml", "config.yaml")];
        _plugin.ReadConfig = () => initialFiles;
        _reportSender.BeforeEnqueue = () => initialContent[0] = (byte)'I';

        _reporter.HandleServerCapabilities(ServerCapabilities);
        Assert.Empty(_reportSender.EffectiveConfigReports);
        Assert.Equal((byte)'i', initialContent[0]);
        _reporter.NotifyEffectiveConfigChanged();
        Assert.Equal((byte)'i', initialContent[0]);
        RunDueTimers();
        _reportSender.BeforeEnqueue = null;
        Assert.Equal((byte)'I', initialContent[0]);

        _plugin.ReadConfig = () => CreateConfig("updated");
        _reporter.NotifyEffectiveConfigChanged();
        RunDueTimers();

        _plugin.ReadConfig = () => Array.Empty<EffectiveConfigFile>();
        _reporter.NotifyEffectiveConfigChanged();
        RunDueTimers();

        Assert.Collection(
            _reportSender.EffectiveConfigReports,
            report => Assert.Equal("initial", GetConfigContent(report)),
            report => Assert.Equal("updated", GetConfigContent(report)),
            report => Assert.Empty(report));
    }

    [Fact]
    public void ReportsOnlyWhileServerSupportsReportingAndBeforeShutdown()
    {
        _reporter.NotifyEffectiveConfigChanged();
        Assert.Equal(0, _plugin.GetterCalls);
        Assert.Empty(_reportSender.EffectiveConfigReports);

        _reporter.HandleServerCapabilities(ServerCapabilities);
        RunDueTimers();
        Assert.Equal(1, _plugin.GetterCalls);

        _reporter.HandleServerCapabilities(ServerCapabilities);
        RunDueTimers();
        Assert.Equal(1, _plugin.GetterCalls);

        _reporter.HandleServerCapabilities(ServerSentCapabilities.None);
        _reporter.NotifyEffectiveConfigChanged();
        RunDueTimers();
        Assert.Equal(2, _plugin.GetterCalls);

        _reporter.HandleServerCapabilities(ServerSentCapabilities.AcceptsStatus);
        _reporter.NotifyEffectiveConfigChanged();
        RunDueTimers();
        Assert.Equal(2, _plugin.GetterCalls);

        _reporter.HandleServerCapabilities(ServerCapabilities);
        Assert.Equal(2, _plugin.GetterCalls);
        RunDueTimers();
        Assert.Equal(3, _plugin.GetterCalls);

        _reporter.Stop();
        _reporter.NotifyEffectiveConfigChanged();
        _reporter.ReportFullState();
        Assert.Equal(3, _plugin.GetterCalls);
        Assert.Equal(3, _reportSender.EffectiveConfigReports.Count);
        Assert.Empty(_reportSender.FullStateReports);
    }

    [Fact]
    public void FullStateReportsConfigurationOnlyWhenSupported()
    {
        _reporter.HandleServerCapabilities(ServerCapabilities);
        _reporter.ReportFullState();
        RunDueTimers();

        _reporter.HandleServerCapabilities(ServerSentCapabilities.AcceptsStatus);
        AdvanceTime(ReportInterval);
        _reporter.ReportFullState();
        RunDueTimers();

        Assert.Collection(
            _reportSender.FullStateReports,
            report => Assert.Equal("initial", GetConfigContent(report.EffectiveConfigFiles)),
            report => Assert.Null(report.EffectiveConfigFiles));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReportingFailureIsContainedAndNextNotificationSucceeds(bool failSending)
    {
        var failReading = !failSending;
        _plugin.ReadConfig = () =>
            failReading ? throw new InvalidOperationException("Cannot read configuration.") : CreateConfig("updated");
        _reportSender.FailEffectiveConfigSend = failSending;

        _reporter.HandleServerCapabilities(ServerCapabilities);
        _reporter.NotifyEffectiveConfigChanged();
        RunDueTimers();
        Assert.Empty(_reportSender.EffectiveConfigReports);

        failReading = false;
        _reportSender.FailEffectiveConfigSend = false;
        _reporter.NotifyEffectiveConfigChanged();
        RunDueTimers();
        Assert.Equal("updated", GetConfigContent(Assert.Single(_reportSender.EffectiveConfigReports)));
    }

    [Fact]
    public void ReportsInitialAndChangedRemoteStatus()
    {
        _reporter.NotifyRemoteConfigStatusChanged();
        RunDueTimers();

        _plugin.ReadRemoteStatus = () => CreateRemoteStatus(RemoteConfigStatusCode.Failed, "cannot apply", "new-hash");
        _reporter.NotifyRemoteConfigStatusChanged();
        RunDueTimers();

        Assert.Collection(
            _reportSender.RemoteConfigStatusReports,
            report => AssertRemoteStatus(report, RemoteConfigStatusCode.Applied),
            report => AssertRemoteStatus(report, RemoteConfigStatusCode.Failed, "cannot apply", "new-hash"));
        Assert.Empty(_reportSender.EffectiveConfigReports);
    }

    [Fact]
    public void RemoteStatusIsOmittedUntilPluginPublishesApplicationProgress()
    {
        _plugin.ReadRemoteStatus = () => null;
        _reporter.NotifyRemoteConfigStatusChanged();
        RunDueTimers();

        Assert.Empty(_reportSender.RemoteConfigStatusReports);

        _plugin.ReadRemoteStatus = () => CreateRemoteStatus(RemoteConfigStatusCode.Applying);
        _reporter.NotifyRemoteConfigStatusChanged();
        RunDueTimers();
        _plugin.ReadRemoteStatus = () => CreateRemoteStatus(RemoteConfigStatusCode.Applied);
        _reporter.NotifyRemoteConfigStatusChanged();
        RunDueTimers();

        Assert.Collection(
            _reportSender.RemoteConfigStatusReports,
            report => AssertRemoteStatus(report, RemoteConfigStatusCode.Applying),
            report => AssertRemoteStatus(report, RemoteConfigStatusCode.Applied));
    }

    [Theory]
    [InlineData(RemoteConfigStatusCode.Applied, null)]
    [InlineData(RemoteConfigStatusCode.Failed, "cannot apply")]
    public void ReportsRemoteStatusAfterServerStopsOfferingConfiguration(RemoteConfigStatusCode status, string? errorMessage)
    {
        _plugin.ReadRemoteStatus = () => CreateRemoteStatus(RemoteConfigStatusCode.Applying);
        _reporter.HandleServerCapabilities(ServerSentCapabilities.AcceptsStatus | ServerSentCapabilities.OffersRemoteConfig);
        _reporter.HandleServerCapabilities(ServerSentCapabilities.AcceptsStatus);

        _plugin.ReadRemoteStatus = () => CreateRemoteStatus(status, errorMessage);
        _reporter.NotifyRemoteConfigStatusChanged();
        RunDueTimers();
        _reporter.ReportFullState();
        RunDueTimers();

        AssertRemoteStatus(_reportSender.RemoteConfigStatusReports.Last(), status, errorMessage);
        AssertRemoteStatus(Assert.Single(_reportSender.FullStateReports).RemoteConfigStatus, status, errorMessage);
    }

    [Fact]
    public void FullStateReportsCurrentRemoteStatus()
    {
        _reporter.HandleServerCapabilities(ServerSentCapabilities.AcceptsStatus);

        _plugin.ReadRemoteStatus = () => CreateRemoteStatus(RemoteConfigStatusCode.Failed, "cannot apply", "new-hash");
        _reporter.ReportFullState();
        RunDueTimers();

        AssertRemoteStatus(Assert.Single(_reportSender.FullStateReports).RemoteConfigStatus, RemoteConfigStatusCode.Failed, "cannot apply", "new-hash");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FullStateOmitsUnavailableRemoteStatus(bool hasProvider)
    {
        _plugin.ReadRemoteStatus = () => null;
        using var reporter = new OpAmpReporter(null, hasProvider ? _plugin : null, _reportSender, CreateTimer);
        reporter.ReportFullState();
        RunDueTimers();

        Assert.Null(Assert.Single(_reportSender.FullStateReports).RemoteConfigStatus);
        Assert.Empty(_reportSender.RemoteConfigStatusReports);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FullStateReportsAvailableStateWhenOneProviderFails(bool failEffectiveConfig)
    {
        _reporter.HandleServerCapabilities(ServerCapabilities | ServerSentCapabilities.OffersRemoteConfig);
        if (failEffectiveConfig)
        {
            _plugin.ReadConfig = () => throw new InvalidOperationException("Cannot read configuration.");
        }
        else
        {
            _plugin.ReadRemoteStatus = () => throw new InvalidOperationException("Cannot read status.");
        }

        _reporter.ReportFullState();
        RunDueTimers();

        var report = Assert.Single(_reportSender.FullStateReports);
        if (failEffectiveConfig)
        {
            Assert.Null(report.EffectiveConfigFiles);
            AssertRemoteStatus(report.RemoteConfigStatus, RemoteConfigStatusCode.Applied);
        }
        else
        {
            Assert.Equal("initial", GetConfigContent(report.EffectiveConfigFiles));
            Assert.Null(report.RemoteConfigStatus);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RemoteStatusFailureIsContainedAndNextNotificationRetries(bool failSending)
    {
        if (failSending)
        {
            _reportSender.BeforeEnqueue = () => throw new InvalidOperationException("Cannot queue status.");
        }
        else
        {
            _plugin.ReadRemoteStatus = () => throw new InvalidOperationException("Cannot read status.");
        }

        _reporter.NotifyRemoteConfigStatusChanged();
        RunDueTimers();
        Assert.Empty(_reportSender.RemoteConfigStatusReports);

        _plugin.ReadRemoteStatus = () => CreateRemoteStatus();
        _reportSender.BeforeEnqueue = null;
        _reporter.NotifyRemoteConfigStatusChanged();
        RunDueTimers();
        AssertRemoteStatus(Assert.Single(_reportSender.RemoteConfigStatusReports), RemoteConfigStatusCode.Applied);
    }

    [Fact]
    public void FullStateSubmissionFailureIsContainedAndNextNotificationReportsStatus()
    {
        _reportSender.BeforeEnqueue = () => throw new InvalidOperationException("Cannot queue report.");

        _reporter.ReportFullState();
        RunDueTimers();
        Assert.Empty(_reportSender.FullStateReports);

        _reportSender.BeforeEnqueue = null;
        _reporter.NotifyRemoteConfigStatusChanged();
        RunDueTimers();

        AssertRemoteStatus(Assert.Single(_reportSender.RemoteConfigStatusReports), RemoteConfigStatusCode.Applied);
    }

    [Fact]
    public async Task CapabilityChangesDoNotWaitForClaimedReporting()
    {
        using var releaseEnqueue = new ManualResetEventSlim();
        var enqueueEntered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _reportSender.BeforeEnqueue = () =>
        {
            enqueueEntered.TrySetResult(true);
            releaseEnqueue.Wait();
        };

        _reporter.HandleServerCapabilities(ServerCapabilities);
        _reporter.ReportFullState();
        var reportTask = Task.Run(RunDueTimers);
        Task? capabilitiesTask = null;
        try
        {
            await TaskAssertions.CompletesAsync(enqueueEntered.Task);
            capabilitiesTask = Task.Run(() => _reporter.HandleServerCapabilities(ServerSentCapabilities.AcceptsStatus));

            await TaskAssertions.CompletesAsync(capabilitiesTask);
        }
        finally
        {
            releaseEnqueue.Set();
            await TaskAssertions.CompletesAsync(Task.WhenAll(reportTask, capabilitiesTask ?? Task.CompletedTask));
        }

        Assert.Equal(1, _plugin.GetterCalls);
        Assert.Equal("initial", GetConfigContent(Assert.Single(_reportSender.FullStateReports).EffectiveConfigFiles));
    }

    [Fact]
    public async Task EffectiveConfigNotificationRemainsPendingWhenCapabilityIsRevokedDuringRead()
    {
        using var releaseRead = new ManualResetEventSlim();
        var readEntered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var blockFirstRead = 1;
        _plugin.ReadConfig = () =>
        {
            if (Interlocked.Exchange(ref blockFirstRead, 0) == 1)
            {
                readEntered.TrySetResult(true);
                releaseRead.Wait();
            }

            return CreateConfig("initial");
        };

        _reporter.HandleServerCapabilities(ServerCapabilities);
        _reporter.NotifyEffectiveConfigChanged();
        var reportTask = Task.Run(_timers[1].FireIfDue);
        try
        {
            await TaskAssertions.CompletesAsync(readEntered.Task);
            _reporter.HandleServerCapabilities(ServerSentCapabilities.AcceptsStatus);
        }
        finally
        {
            releaseRead.Set();
            await TaskAssertions.CompletesAsync(reportTask);
        }

        Assert.Empty(_reportSender.EffectiveConfigReports);
        Assert.Equal(1, _plugin.GetterCalls);

        _reporter.HandleServerCapabilities(ServerCapabilities);
        RunDueTimers();

        Assert.Equal(2, _plugin.GetterCalls);
        Assert.Equal("initial", GetConfigContent(Assert.Single(_reportSender.EffectiveConfigReports)));
    }

    [Fact]
    public async Task FullStateOmitsEffectiveConfigWhenCapabilityIsRevokedDuringRead()
    {
        using var releaseRead = new ManualResetEventSlim();
        var readEntered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _plugin.ReadConfig = () =>
        {
            readEntered.TrySetResult(true);
            releaseRead.Wait();
            return CreateConfig("initial");
        };

        _reporter.HandleServerCapabilities(ServerCapabilities);
        _reporter.ReportFullState();
        var reportTask = Task.Run(RunDueTimers);
        try
        {
            await TaskAssertions.CompletesAsync(readEntered.Task);
            _reporter.HandleServerCapabilities(ServerSentCapabilities.AcceptsStatus);
        }
        finally
        {
            releaseRead.Set();
            await TaskAssertions.CompletesAsync(reportTask);
        }

        Assert.Equal(1, _plugin.GetterCalls);
        Assert.Null(Assert.Single(_reportSender.FullStateReports).EffectiveConfigFiles);
    }

    [Fact]
    public async Task NotificationDuringActiveReportReturnsBeforeFollowUpDrain()
    {
        using var releaseEnqueue = new ManualResetEventSlim();
        var enqueueEntered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _reportSender.BeforeEnqueue = () =>
        {
            enqueueEntered.TrySetResult(true);
            releaseEnqueue.Wait();
        };

        _reporter.HandleServerCapabilities(ServerCapabilities);
        _reporter.ReportFullState();
        var reportTask = Task.Run(_timers[0].FireIfDue);
        Task? notificationTask = null;
        try
        {
            await TaskAssertions.CompletesAsync(enqueueEntered.Task);
            notificationTask = Task.Run(_reporter.NotifyEffectiveConfigChanged);
            await TaskAssertions.CompletesAsync(notificationTask);

            Assert.Empty(_reportSender.EffectiveConfigReports);
        }
        finally
        {
            releaseEnqueue.Set();
            await TaskAssertions.CompletesAsync(Task.WhenAll(
                reportTask,
                notificationTask ?? Task.CompletedTask));
        }

        RunDueTimers();
        Assert.Single(_reportSender.FullStateReports);
        Assert.Single(_reportSender.EffectiveConfigReports);
    }

    [Fact]
    public async Task BurstNotificationsDuringActivePluginReportAreCoalescedIntoOneFollowUp()
    {
        const int NotificationCount = 8;
        using var releaseReport = new ManualResetEventSlim();
        var reportEntered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var blockFirstRead = 1;
        _plugin.ReadRemoteStatus = () =>
        {
            if (Interlocked.Exchange(ref blockFirstRead, 0) == 1)
            {
                reportEntered.TrySetResult(true);
                releaseReport.Wait();
            }

            return CreateRemoteStatus();
        };

        _reporter.NotifyRemoteConfigStatusChanged();
        var reportTask = Task.Run(_timers[1].FireIfDue);
        try
        {
            await TaskAssertions.CompletesAsync(reportEntered.Task);
            for (var i = 0; i < NotificationCount; i++)
            {
                _reporter.NotifyRemoteConfigStatusChanged();
            }

            // A real zero-delay timer could run each newly scheduled callback and let the next
            // notification queue another. Keep one drain active until this report completes.
            Assert.Equal(1, _timers[1].ScheduleCount);
            Assert.Empty(_reportSender.RemoteConfigStatusReports);
        }
        finally
        {
            releaseReport.Set();
            await TaskAssertions.CompletesAsync(reportTask);
        }

        Assert.Single(_reportSender.RemoteConfigStatusReports);
        Assert.Equal(2, _timers[1].ScheduleCount);

        _timers[1].FireIfDue();
        Assert.Equal(2, _plugin.RemoteStatusGetterCalls);
        Assert.Equal(2, _reportSender.RemoteConfigStatusReports.Count);

        _timers[1].FireIfDue();
        Assert.Equal(2, _timers[1].ScheduleCount);
        Assert.Equal(2, _reportSender.RemoteConfigStatusReports.Count);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ShutdownReturnsWhileReportingIsBlocked(bool blockSubmission)
    {
        using var releaseReport = new ManualResetEventSlim();
        var reportEntered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        void BlockReport()
        {
            reportEntered.TrySetResult(true);
            releaseReport.Wait();
        }

        if (blockSubmission)
        {
            _reportSender.BeforeEnqueue = BlockReport;
        }
        else
        {
            _plugin.ReadConfig = () =>
            {
                BlockReport();
                return CreateConfig("initial");
            };
        }

        _reporter.HandleServerCapabilities(ServerCapabilities);
        _reporter.ReportFullState();
        var reportTask = Task.Run(RunDueTimers);
        Task? stopTask = null;
        try
        {
            await TaskAssertions.CompletesAsync(reportEntered.Task);
            stopTask = Task.Run(() =>
            {
                _reporter.Stop();
                _reporter.NotifyEffectiveConfigChanged();
                _reporter.NotifyRemoteConfigStatusChanged();
                _reporter.ReportFullState();
                _reporter.HandleServerCapabilities(ServerSentCapabilities.AcceptsStatus);
            });

            await TaskAssertions.CompletesAsync(stopTask);
            Assert.Empty(_reportSender.EffectiveConfigReports);
            Assert.Empty(_reportSender.RemoteConfigStatusReports);
        }
        finally
        {
            releaseReport.Set();
            await TaskAssertions.CompletesAsync(Task.WhenAll(reportTask, stopTask ?? Task.CompletedTask));
        }

        Assert.Equal(1, _plugin.GetterCalls);
        Assert.Empty(_reportSender.RemoteConfigStatusReports);
        Assert.Empty(_reportSender.EffectiveConfigReports);
        Assert.Equal(blockSubmission ? 1 : 0, _reportSender.FullStateReports.Count);
    }

    [Fact]
    public void RepeatedFullStateRequestsAreCoalescedWithoutExtendingCooldown()
    {
        _reporter.ReportFullState();
        Assert.Empty(_reportSender.FullStateReports);
        RunDueTimers();
        Assert.Single(_reportSender.FullStateReports);
        _reporter.ReportFullState();
        for (var i = 0; i < 4; i++)
        {
            AdvanceTime(TimeSpan.FromSeconds(1));
            _reporter.ReportFullState();
        }

        AdvanceTime(TimeSpan.FromSeconds(1) - TimeSpan.FromTicks(1));
        _reporter.ReportFullState();
        Assert.Single(_reportSender.FullStateReports);

        AdvanceTime(TimeSpan.FromTicks(1));
        Assert.Equal(2, _reportSender.FullStateReports.Count);
        Assert.Equal(2, _plugin.RemoteStatusGetterCalls);

        AdvanceTime(ReportInterval + ReportInterval);
        Assert.Equal(2, _reportSender.FullStateReports.Count);
        _reporter.ReportFullState();
        RunDueTimers();
        Assert.Equal(3, _reportSender.FullStateReports.Count);
    }

    [Fact]
    public void CapabilityChangesDoNotCreateReportsOrStartCooldown()
    {
        var effectiveConfigCapabilities = ServerSentCapabilities.AcceptsStatus | ServerSentCapabilities.AcceptsEffectiveConfig;
        var remoteConfigCapabilities = ServerSentCapabilities.AcceptsStatus | ServerSentCapabilities.OffersRemoteConfig;

        _reporter.HandleServerCapabilities(effectiveConfigCapabilities);
        _reporter.HandleServerCapabilities(remoteConfigCapabilities);
        _reporter.HandleServerCapabilities(effectiveConfigCapabilities | ServerSentCapabilities.OffersRemoteConfig);
        RunDueTimers();

        Assert.Equal(0, _plugin.GetterCalls);
        Assert.Equal(0, _plugin.RemoteStatusGetterCalls);
        Assert.Empty(_reportSender.EffectiveConfigReports);
        Assert.Empty(_reportSender.RemoteConfigStatusReports);

        _reporter.ReportFullState();
        RunDueTimers();
        Assert.Single(_reportSender.FullStateReports);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FailedOrIncompleteFullStateAttemptsConsumeCooldown(bool failSending)
    {
        _plugin.ReadConfig = () => failSending ? CreateConfig("initial") : throw new InvalidOperationException("Cannot read configuration.");
        if (failSending)
        {
            _reportSender.BeforeEnqueue = () => throw new InvalidOperationException("Cannot queue report.");
        }

        _reporter.HandleServerCapabilities(ServerCapabilities);
        _reporter.ReportFullState();
        RunDueTimers();
        Assert.Equal(failSending ? 0 : 1, _reportSender.FullStateReports.Count);

        _plugin.ReadConfig = () => CreateConfig("updated");
        _reportSender.BeforeEnqueue = null;
        _reporter.ReportFullState();
        Assert.Equal(1, _plugin.GetterCalls);
        Assert.Equal(failSending ? 0 : 1, _reportSender.FullStateReports.Count);

        AdvanceTime(ReportInterval);
        Assert.Equal("updated", GetConfigContent(_reportSender.FullStateReports.Last().EffectiveConfigFiles));
        AdvanceTime(ReportInterval + ReportInterval);
        Assert.Equal(2, _plugin.GetterCalls);
        Assert.Equal(failSending ? 1 : 2, _reportSender.FullStateReports.Count);
    }

    [Fact]
    public void PluginNotificationsDoNotParticipateInServerCooldown()
    {
        _reporter.HandleServerCapabilities(ServerCapabilities);

        _reporter.ReportFullState();
        RunDueTimers();
        Assert.Single(_reportSender.FullStateReports);

        _reporter.ReportFullState();

        AdvanceTime(TimeSpan.FromSeconds(4));
        _reporter.NotifyEffectiveConfigChanged();
        RunDueTimers();
        Assert.Single(_reportSender.EffectiveConfigReports);
        Assert.Single(_reportSender.FullStateReports);

        AdvanceTime(TimeSpan.FromSeconds(1));
        Assert.Equal(2, _reportSender.FullStateReports.Count);
    }

    [Fact]
    public void PendingFullStateReadsFreshStateWhenDispatched()
    {
        _reporter.HandleServerCapabilities(ServerCapabilities | ServerSentCapabilities.OffersRemoteConfig);
        _reporter.ReportFullState();
        Assert.Equal(0, _plugin.GetterCalls);
        Assert.Equal(0, _plugin.RemoteStatusGetterCalls);
        Assert.Empty(_reportSender.FullStateReports);

        _plugin.ReadConfig = () => CreateConfig("updated");
        _plugin.ReadRemoteStatus = () => CreateRemoteStatus(RemoteConfigStatusCode.Failed, "cannot apply", "new-hash");

        RunDueTimers();
        var report = Assert.Single(_reportSender.FullStateReports);
        Assert.Equal("updated", GetConfigContent(report.EffectiveConfigFiles));
        AssertRemoteStatus(report.RemoteConfigStatus, RemoteConfigStatusCode.Failed, "cannot apply", "new-hash");
    }

    [Fact]
    public void DeferredEffectiveConfigRechecksCapabilitiesBeforeClaimingWork()
    {
        using var reporter = new OpAmpReporter(_plugin, null, _reportSender, CreateTimer);
        reporter.NotifyEffectiveConfigChanged();
        reporter.HandleServerCapabilities(ServerCapabilities);
        reporter.HandleServerCapabilities(ServerSentCapabilities.AcceptsStatus);

        RunDueTimers();
        Assert.Equal(0, _plugin.GetterCalls);
        Assert.Empty(_reportSender.EffectiveConfigReports);

        reporter.HandleServerCapabilities(ServerCapabilities);
        Assert.Equal(0, _plugin.GetterCalls);
        RunDueTimers();
        Assert.Equal("initial", GetConfigContent(Assert.Single(_reportSender.EffectiveConfigReports)));
    }

    [Fact]
    public void StaleDeferredPluginCallbackDoesNotDuplicateNotification()
    {
        var timers = new List<ManualReportTimer>();
        IOpAmpReportTimer CreateLocalTimer(Action callback)
        {
            var timer = new ManualReportTimer(GetElapsed, callback);
            timers.Add(timer);
            return timer;
        }

        using var reporter = new OpAmpReporter(null, _plugin, _reportSender, CreateLocalTimer);
        reporter.NotifyRemoteConfigStatusChanged();
        Assert.Empty(_reportSender.RemoteConfigStatusReports);
        timers[1].FireIfDue();
        Assert.Single(_reportSender.RemoteConfigStatusReports);

        timers[1].InvokeCallback();
        reporter.Stop();
        timers[1].InvokeCallback();
        Assert.Single(_reportSender.RemoteConfigStatusReports);
    }

    [Fact]
    public void StaleCallbacksDoNotDuplicateReportsOrBypassShutdown()
    {
        _reporter.ReportFullState();
        RunDueTimers();
        Assert.Single(_reportSender.FullStateReports);

        AdvanceTime(ReportInterval);
        _timers[0].InvokeCallback();
        Assert.Single(_reportSender.FullStateReports);

        _reporter.ReportFullState();
        _reporter.Stop();
        _reporter.Stop();
        _timers[0].InvokeCallback();
        AdvanceTime(ReportInterval);
        Assert.Equal(1, _plugin.RemoteStatusGetterCalls);
        Assert.Single(_reportSender.FullStateReports);
    }

    [Fact]
    public async Task RequestsDuringActiveReportingRemainPending()
    {
        using var releaseReport = new ManualResetEventSlim();
        var entered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        void BlockReport()
        {
            entered.TrySetResult(true);
            releaseReport.Wait();
        }

        _plugin.ReadRemoteStatus = () =>
        {
            BlockReport();
            return CreateRemoteStatus();
        };

        _reporter.ReportFullState();
        var reportTask = Task.Run(RunDueTimers);
        Task? requestTask = null;
        try
        {
            await TaskAssertions.CompletesAsync(entered.Task);
            requestTask = Task.Run(_reporter.ReportFullState);
            await TaskAssertions.CompletesAsync(requestTask);
            AdvanceTime(ReportInterval + ReportInterval);
            Assert.Empty(_reportSender.FullStateReports);
        }
        finally
        {
            releaseReport.Set();
            await TaskAssertions.CompletesAsync(Task.WhenAll(reportTask, requestTask ?? Task.CompletedTask));
        }

        _plugin.ReadRemoteStatus = () => CreateRemoteStatus();
        Assert.Single(_reportSender.FullStateReports);
        AdvanceTime(ReportInterval - TimeSpan.FromTicks(1));
        Assert.Single(_reportSender.FullStateReports);
        AdvanceTime(TimeSpan.FromTicks(1));
        Assert.Equal(2, _reportSender.FullStateReports.Count);
        Assert.Equal(2, _plugin.RemoteStatusGetterCalls);
    }

    public void Dispose()
    {
        _reporter.Dispose();
        GC.SuppressFinalize(this);
    }

    private static string GetConfigContent(IEnumerable<EffectiveConfigFile>? files)
    {
        Assert.NotNull(files);
        return Encoding.UTF8.GetString(Assert.Single(files).Content.ToArray());
    }

    private static EffectiveConfigFile[] CreateConfig(string content)
    {
        return [new EffectiveConfigFile(Encoding.UTF8.GetBytes(content), "text/yaml", "config.yaml")];
    }

    private static RemoteConfigStatusReport CreateRemoteStatus(
        RemoteConfigStatusCode status = RemoteConfigStatusCode.Applied, string? errorMessage = null, string hash = "config-hash")
    {
        return new RemoteConfigStatusReport(Encoding.UTF8.GetBytes(hash), status, errorMessage);
    }

    private static void AssertRemoteStatus(
        RemoteConfigStatusReport? report, RemoteConfigStatusCode status, string? errorMessage = null, string hash = "config-hash")
    {
        Assert.NotNull(report);
        Assert.Equal(hash, Encoding.UTF8.GetString(report.LastRemoteConfigHash.ToArray()));
        Assert.Equal(status, report.Status);
        Assert.Equal(errorMessage, report.ErrorMessage);
    }

    private TimeSpan GetElapsed()
    {
        return TimeSpan.FromTicks(Interlocked.Read(ref _elapsedTicks));
    }

    private void RunDueTimers()
    {
        AdvanceTime(TimeSpan.Zero);
    }

    private void AdvanceTime(TimeSpan elapsed)
    {
        Interlocked.Add(ref _elapsedTicks, elapsed.Ticks);
        foreach (var timer in _timers)
        {
            timer.FireIfDue();
        }
    }

    private ManualReportTimer CreateTimer(Action callback)
    {
        var timer = new ManualReportTimer(GetElapsed, callback);
        _timers.Add(timer);
        return timer;
    }

    private sealed class ManualReportTimer(Func<TimeSpan> getElapsed, Action callback) : IOpAmpReportTimer
    {
        private readonly object _lock = new();
        private TimeSpan? _dueTime;
        private bool _disposed;
        private int _scheduleCount;

        public int ScheduleCount
        {
            get
            {
                lock (_lock)
                {
                    return _scheduleCount;
                }
            }
        }

        public void Schedule(TimeSpan delay)
        {
            lock (_lock)
            {
                Assert.False(_disposed);
                _dueTime = getElapsed() + delay;
                _scheduleCount++;
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                _disposed = true;
                _dueTime = null;
            }
        }

        public void FireIfDue()
        {
            lock (_lock)
            {
                if (!_dueTime.HasValue || _dueTime.Value > getElapsed())
                {
                    return;
                }

                _dueTime = null;
            }

            InvokeCallback();
        }

        // Also models callbacks queued before disposal.
        public void InvokeCallback()
        {
            callback();
        }
    }

    private sealed class RecordingOpAmpReportSender : IOpAmpReportSender
    {
        public List<IReadOnlyCollection<EffectiveConfigFile>> EffectiveConfigReports { get; } = [];

        public List<FullStateReport> FullStateReports { get; } = [];

        public List<RemoteConfigStatusReport> RemoteConfigStatusReports { get; } = [];

        public bool FailEffectiveConfigSend { get; set; }

        public Action? BeforeEnqueue { get; set; }

        public void SendEffectiveConfig(IReadOnlyCollection<EffectiveConfigFile> files)
        {
            if (FailEffectiveConfigSend)
            {
                throw new InvalidOperationException("Cannot queue configuration.");
            }

            BeforeEnqueue?.Invoke();
            EffectiveConfigReports.Add(files);
        }

        public void SendFullStateReport(FullStateReport report)
        {
            BeforeEnqueue?.Invoke();
            FullStateReports.Add(report);
        }

        public void SendRemoteConfigStatus(RemoteConfigStatusReport statusReport)
        {
            BeforeEnqueue?.Invoke();
            RemoteConfigStatusReports.Add(statusReport);
        }
    }

    private sealed class TestReportingPlugin : IProvideEffectiveConfig, IProvideRemoteConfigStatus
    {
        private int _getterCalls;
        private int _remoteStatusGetterCalls;

        public Func<IReadOnlyCollection<EffectiveConfigFile>> ReadConfig { get; set; } = () => CreateConfig("initial");

        public Func<RemoteConfigStatusReport?> ReadRemoteStatus { get; set; } = () => CreateRemoteStatus();

        public int GetterCalls => Volatile.Read(ref _getterCalls);

        public int RemoteStatusGetterCalls => Volatile.Read(ref _remoteStatusGetterCalls);

        public IReadOnlyCollection<EffectiveConfigFile> GetEffectiveConfig()
        {
            Interlocked.Increment(ref _getterCalls);
            return ReadConfig();
        }

        public RemoteConfigStatusReport? GetRemoteConfigStatus()
        {
            Interlocked.Increment(ref _remoteStatusGetterCalls);
            return ReadRemoteStatus();
        }

        public void ConfigureOpAmpOptions(OpAmpClientSettings settings)
        {
        }

        public void ConfigureOpAmpClient(IOpAmpClient client)
        {
        }

        public void AfterOpAmpClientStarted()
        {
        }

        public void BeforeOpAmpClientStopped()
        {
        }
    }
}
