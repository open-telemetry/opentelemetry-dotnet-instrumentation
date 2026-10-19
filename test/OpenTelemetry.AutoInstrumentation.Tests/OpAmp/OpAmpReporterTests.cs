// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Concurrent;
using System.Text;
using OpenTelemetry.AutoInstrumentation.OpAmp;
using OpenTelemetry.AutoInstrumentation.PluginApi.OpAmp;
using OpenTelemetry.OpAmp.Client.Messages;
using OpenTelemetry.OpAmp.Client.Settings;

namespace OpenTelemetry.AutoInstrumentation.Tests.OpAmp;

public class OpAmpReporterTests
{
    private const ServerSentCapabilities ServerCapabilities = ServerSentCapabilities.AcceptsStatus | ServerSentCapabilities.AcceptsEffectiveConfig;
    private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(5);
    private readonly TestReportingPlugin _plugin = new();
    private readonly RecordingOpAmpReportSender _reportSender = new();
    private readonly OpAmpReporter _reporter;

    public OpAmpReporterTests()
    {
        _reporter = new OpAmpReporter(_plugin, _plugin, _reportSender);
    }

    [Fact]
    public void ReportsInitialAndNotifiedConfigurationWithCopiedBuffers()
    {
        var initialContent = Encoding.UTF8.GetBytes("initial");
        EffectiveConfigFile[] initialFiles = [new(initialContent, "text/yaml", "config.yaml")];
        _plugin.ReadConfig = () => initialFiles;
        _reportSender.BeforeEnqueue = () => initialContent[0] = (byte)'I';

        _reporter.HandleServerCapabilities(ServerCapabilities);
        _reportSender.BeforeEnqueue = null;
        Assert.Equal((byte)'I', initialContent[0]);

        _plugin.ReadConfig = () => CreateConfig("updated");
        _reporter.NotifyEffectiveConfigChanged();

        _plugin.ReadConfig = () => Array.Empty<EffectiveConfigFile>();
        _reporter.NotifyEffectiveConfigChanged();

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
        Assert.Equal(1, _plugin.GetterCalls);

        _reporter.HandleServerCapabilities(ServerCapabilities);
        Assert.Equal(1, _plugin.GetterCalls);

        _reporter.HandleServerCapabilities(ServerSentCapabilities.None);
        _reporter.NotifyEffectiveConfigChanged();
        Assert.Equal(2, _plugin.GetterCalls);

        _reporter.HandleServerCapabilities(ServerSentCapabilities.AcceptsStatus);
        _reporter.NotifyEffectiveConfigChanged();
        Assert.Equal(2, _plugin.GetterCalls);

        _reporter.HandleServerCapabilities(ServerCapabilities);
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

        _reporter.HandleServerCapabilities(ServerSentCapabilities.AcceptsStatus);
        _reporter.ReportFullState();

        Assert.Collection(
            _reportSender.FullStateReports,
            report => Assert.Equal("initial", GetConfigContent(report.EffectiveConfigFiles)),
            report => Assert.Null(report.EffectiveConfigFiles));
    }

    [Fact]
    public void FullStateIsReportedWithoutConfigurationWhenProviderFails()
    {
        _reporter.HandleServerCapabilities(ServerCapabilities);
        _plugin.ReadConfig = () => throw new InvalidOperationException("Cannot read configuration.");

        _reporter.ReportFullState();
        Assert.Null(Assert.Single(_reportSender.FullStateReports).EffectiveConfigFiles);

        _plugin.ReadConfig = () => CreateConfig("updated");
        _reporter.NotifyEffectiveConfigChanged();

        Assert.Collection(
            _reportSender.EffectiveConfigReports,
            report => Assert.Equal("initial", GetConfigContent(report)),
            report => Assert.Equal("updated", GetConfigContent(report)));
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
        Assert.Empty(_reportSender.EffectiveConfigReports);

        failReading = false;
        _reportSender.FailEffectiveConfigSend = false;
        _reporter.NotifyEffectiveConfigChanged();
        Assert.Equal("updated", GetConfigContent(Assert.Single(_reportSender.EffectiveConfigReports)));
    }

    [Fact]
    public void ReportsInitialAndChangedRemoteStatus()
    {
        _reporter.HandleServerCapabilities(ServerSentCapabilities.OffersRemoteConfig);

        _plugin.ReadRemoteStatus = () => CreateRemoteStatus(RemoteConfigStatusCode.Failed, "cannot apply", "new-hash");
        _reporter.NotifyRemoteConfigStatusChanged();

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
        _reporter.HandleServerCapabilities(ServerSentCapabilities.OffersRemoteConfig);
        _reporter.NotifyRemoteConfigStatusChanged();

        Assert.Empty(_reportSender.RemoteConfigStatusReports);

        _plugin.ReadRemoteStatus = () => CreateRemoteStatus(RemoteConfigStatusCode.Applying);
        _reporter.NotifyRemoteConfigStatusChanged();
        _plugin.ReadRemoteStatus = () => CreateRemoteStatus(RemoteConfigStatusCode.Applied);
        _reporter.NotifyRemoteConfigStatusChanged();

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
        _reporter.ReportFullState();

        AssertRemoteStatus(_reportSender.RemoteConfigStatusReports.Last(), status, errorMessage);
        AssertRemoteStatus(Assert.Single(_reportSender.FullStateReports).RemoteConfigStatus, status, errorMessage);
    }

    [Fact]
    public void FullStateReportsCurrentRemoteStatus()
    {
        _reporter.HandleServerCapabilities(ServerSentCapabilities.AcceptsStatus);

        _plugin.ReadRemoteStatus = () => CreateRemoteStatus(RemoteConfigStatusCode.Failed, "cannot apply", "new-hash");
        _reporter.ReportFullState();

        AssertRemoteStatus(Assert.Single(_reportSender.FullStateReports).RemoteConfigStatus, RemoteConfigStatusCode.Failed, "cannot apply", "new-hash");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FullStateOmitsUnavailableRemoteStatus(bool hasProvider)
    {
        _plugin.ReadRemoteStatus = () => null;
        var reporter = new OpAmpReporter(null, hasProvider ? _plugin : null, _reportSender);
        reporter.HandleServerCapabilities(ServerSentCapabilities.OffersRemoteConfig);

        reporter.ReportFullState();

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

        _reporter.HandleServerCapabilities(ServerSentCapabilities.OffersRemoteConfig);
        Assert.Empty(_reportSender.RemoteConfigStatusReports);

        _plugin.ReadRemoteStatus = () => CreateRemoteStatus();
        _reportSender.BeforeEnqueue = null;
        _reporter.NotifyRemoteConfigStatusChanged();
        AssertRemoteStatus(Assert.Single(_reportSender.RemoteConfigStatusReports), RemoteConfigStatusCode.Applied);
    }

    [Fact]
    public void FullStateSubmissionFailureIsContainedAndNextNotificationReportsStatus()
    {
        _reporter.HandleServerCapabilities(ServerSentCapabilities.OffersRemoteConfig);
        _reportSender.BeforeEnqueue = () => throw new InvalidOperationException("Cannot queue report.");

        _reporter.ReportFullState();
        Assert.Empty(_reportSender.FullStateReports);

        _reportSender.BeforeEnqueue = null;
        _reporter.NotifyRemoteConfigStatusChanged();

        Assert.Collection(
            _reportSender.RemoteConfigStatusReports,
            report => AssertRemoteStatus(report, RemoteConfigStatusCode.Applied),
            report => AssertRemoteStatus(report, RemoteConfigStatusCode.Applied));
    }

    [Fact]
    public async Task CapabilityRevocationIsOrderedWithReporting()
    {
        using var releaseEnqueue = new ManualResetEventSlim();
        var enqueueEntered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var capabilitiesStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _reportSender.BeforeEnqueue = () =>
        {
            enqueueEntered.TrySetResult(true);
            releaseEnqueue.Wait();
        };

        var reportTask = Task.Run(() => _reporter.HandleServerCapabilities(ServerCapabilities));
        Task? capabilitiesTask = null;
        try
        {
            await AssertCompletes(enqueueEntered.Task);
            capabilitiesTask = Task.Run(() =>
            {
                capabilitiesStarted.TrySetResult(true);
                _reporter.HandleServerCapabilities(ServerSentCapabilities.AcceptsStatus);
                _reporter.ReportFullState();
            });

            await AssertCompletes(capabilitiesStarted.Task);
        }
        finally
        {
            releaseEnqueue.Set();
            await AssertCompletes(Task.WhenAll(reportTask, capabilitiesTask ?? Task.CompletedTask));
        }

        Assert.Equal(1, _plugin.GetterCalls);
        Assert.Equal("initial", GetConfigContent(Assert.Single(_reportSender.EffectiveConfigReports)));
        Assert.Null(Assert.Single(_reportSender.FullStateReports).EffectiveConfigFiles);
        Assert.Equal(
            [nameof(IOpAmpReportSender.SendEffectiveConfig), nameof(IOpAmpReportSender.SendFullStateReport)],
            _reportSender.ReportOrder);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task ShutdownReturnsWhileReportingIsBlocked(bool blockSubmission, bool remoteStatus)
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
        else if (remoteStatus)
        {
            _plugin.ReadRemoteStatus = () =>
            {
                BlockReport();
                return CreateRemoteStatus();
            };
        }
        else
        {
            _plugin.ReadConfig = () =>
            {
                BlockReport();
                return CreateConfig("initial");
            };
        }

        var capabilities = remoteStatus ? ServerSentCapabilities.OffersRemoteConfig : ServerCapabilities;
        var reportTask = Task.Run(() => _reporter.HandleServerCapabilities(capabilities));
        Task? stopTask = null;
        try
        {
            await AssertCompletes(reportEntered.Task);
            stopTask = Task.Run(() =>
            {
                _reporter.Stop();
                _reporter.NotifyEffectiveConfigChanged();
                _reporter.NotifyRemoteConfigStatusChanged();
                _reporter.ReportFullState();
                _reporter.HandleServerCapabilities(ServerSentCapabilities.AcceptsStatus);
            });

            await AssertCompletes(stopTask);
            Assert.Empty(_reportSender.EffectiveConfigReports);
            Assert.Empty(_reportSender.RemoteConfigStatusReports);
        }
        finally
        {
            releaseReport.Set();
            await AssertCompletes(Task.WhenAll(reportTask, stopTask ?? Task.CompletedTask));
        }

        Assert.Equal(1, remoteStatus ? _plugin.RemoteStatusGetterCalls : _plugin.GetterCalls);
        if (remoteStatus)
        {
            Assert.Equal(blockSubmission ? 1 : 0, _reportSender.RemoteConfigStatusReports.Count);
        }
        else if (blockSubmission)
        {
            Assert.Equal("initial", GetConfigContent(Assert.Single(_reportSender.EffectiveConfigReports)));
        }
        else
        {
            Assert.Empty(_reportSender.EffectiveConfigReports);
        }

        Assert.Empty(_reportSender.FullStateReports);
    }

    private static async Task AssertCompletes(Task task)
    {
        Assert.Same(task, await Task.WhenAny(task, Task.Delay(TestTimeout)).ConfigureAwait(false));
        await task.ConfigureAwait(false);
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

    private sealed class RecordingOpAmpReportSender : IOpAmpReportSender
    {
        public List<IReadOnlyCollection<EffectiveConfigFile>> EffectiveConfigReports { get; } = [];

        public List<FullStateReport> FullStateReports { get; } = [];

        public List<RemoteConfigStatusReport> RemoteConfigStatusReports { get; } = [];

        public ConcurrentQueue<string> ReportOrder { get; } = new();

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
            ReportOrder.Enqueue(nameof(SendEffectiveConfig));
        }

        public void SendFullStateReport(FullStateReport report)
        {
            BeforeEnqueue?.Invoke();
            FullStateReports.Add(report);
            ReportOrder.Enqueue(nameof(SendFullStateReport));
        }

        public void SendRemoteConfigStatus(RemoteConfigStatusReport statusReport)
        {
            BeforeEnqueue?.Invoke();
            RemoteConfigStatusReports.Add(statusReport);
            ReportOrder.Enqueue(nameof(SendRemoteConfigStatus));
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
