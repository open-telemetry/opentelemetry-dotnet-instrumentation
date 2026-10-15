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
    private readonly TestEffectiveConfigPlugin _plugin = new();
    private readonly RecordingOpAmpReportSender _reportSender = new();
    private readonly OpAmpReporter _reporter;

    public OpAmpReporterTests()
    {
        _reporter = new OpAmpReporter(_plugin, _reportSender);
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

        var reportTask = Task.Run(() => _reporter.HandleServerCapabilities(ServerCapabilities));
        Task? stopTask = null;
        try
        {
            await AssertCompletes(reportEntered.Task);
            stopTask = Task.Run(() =>
            {
                _reporter.Stop();
                _reporter.NotifyEffectiveConfigChanged();
                _reporter.ReportFullState();
                _reporter.HandleServerCapabilities(ServerSentCapabilities.AcceptsStatus);
            });

            await AssertCompletes(stopTask);
            Assert.Empty(_reportSender.EffectiveConfigReports);
        }
        finally
        {
            releaseReport.Set();
            await AssertCompletes(Task.WhenAll(reportTask, stopTask ?? Task.CompletedTask));
        }

        Assert.Equal(1, _plugin.GetterCalls);
        if (blockSubmission)
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

    private sealed class RecordingOpAmpReportSender : IOpAmpReportSender
    {
        public List<IReadOnlyCollection<EffectiveConfigFile>> EffectiveConfigReports { get; } = [];

        public List<FullStateReport> FullStateReports { get; } = [];

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
    }

    private sealed class TestEffectiveConfigPlugin : IProvideEffectiveConfig
    {
        private int _getterCalls;

        public Func<IReadOnlyCollection<EffectiveConfigFile>> ReadConfig { get; set; } = () => CreateConfig("initial");

        public int GetterCalls => Volatile.Read(ref _getterCalls);

        public IReadOnlyCollection<EffectiveConfigFile> GetEffectiveConfig()
        {
            Interlocked.Increment(ref _getterCalls);
            return ReadConfig();
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
