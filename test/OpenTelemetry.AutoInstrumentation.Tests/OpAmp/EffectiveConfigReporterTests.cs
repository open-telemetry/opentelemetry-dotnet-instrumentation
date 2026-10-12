// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Text;
using OpenTelemetry.AutoInstrumentation.OpAmp;
using OpenTelemetry.AutoInstrumentation.PluginApi.OpAmp;
using OpenTelemetry.OpAmp.Client.Messages;
using OpenTelemetry.OpAmp.Client.Settings;

namespace OpenTelemetry.AutoInstrumentation.Tests.OpAmp;

public class EffectiveConfigReporterTests
{
    private const ServerSentCapabilities ServerCapabilities = ServerSentCapabilities.AcceptsStatus | ServerSentCapabilities.AcceptsEffectiveConfig;
    private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(5);
    private readonly TestEffectiveConfigPlugin _plugin = new();
    private readonly RecordingOpAmpReportSender _reportSender = new();
    private readonly EffectiveConfigReporter _reporter;

    public EffectiveConfigReporterTests()
    {
        _reporter = new EffectiveConfigReporter(_plugin, _reportSender);
    }

    [Fact]
    public async Task ReportsInitialAndNotifiedConfigurationWithCopiedBuffers()
    {
        var initialContent = Encoding.UTF8.GetBytes("initial");
        EffectiveConfigFile[] initialFiles = [new(initialContent, "text/yaml", "config.yaml")];
        _plugin.ReadConfig = _ => initialFiles;

        _reporter.HandleServerCapabilities(ServerCapabilities);
        await AssertCompletes(_reporter.WaitForIdleAsync());
        var initialReport = Assert.Single(_reportSender.EffectiveConfigReports);
        initialContent[0] = (byte)'I';
        Assert.Equal("initial", GetConfigContent(initialReport));

        _plugin.ReadConfig = _ => CreateConfig("initial");
        _reporter.NotifyEffectiveConfigChanged();
        await AssertCompletes(_reporter.WaitForIdleAsync());

        _plugin.ReadConfig = _ => CreateConfig("updated");
        _reporter.NotifyEffectiveConfigChanged();
        await AssertCompletes(_reporter.WaitForIdleAsync());

        _plugin.ReadConfig = _ => Array.Empty<EffectiveConfigFile>();
        _reporter.NotifyEffectiveConfigChanged();
        await AssertCompletes(_reporter.WaitForIdleAsync());

        Assert.Collection(
            _reportSender.EffectiveConfigReports,
            report => Assert.Equal("initial", GetConfigContent(report)),
            report => Assert.Equal("initial", GetConfigContent(report)),
            report => Assert.Equal("updated", GetConfigContent(report)),
            report => Assert.Empty(report));
    }

    [Fact]
    public async Task ReportsOnlyWhileServerSupportsReportingAndBeforeShutdown()
    {
        _reporter.NotifyEffectiveConfigChanged();
        await AssertCompletes(_reporter.WaitForIdleAsync());
        Assert.Equal(0, _plugin.GetterCalls);
        Assert.Empty(_reportSender.EffectiveConfigReports);

        _reporter.HandleServerCapabilities(ServerCapabilities);
        await AssertCompletes(_reporter.WaitForIdleAsync());
        Assert.Equal(1, _plugin.GetterCalls);
        Assert.Single(_reportSender.EffectiveConfigReports);

        _reporter.HandleServerCapabilities(ServerCapabilities);
        await AssertCompletes(_reporter.WaitForIdleAsync());
        Assert.Equal(1, _plugin.GetterCalls);
        Assert.Single(_reportSender.EffectiveConfigReports);

        _reporter.HandleServerCapabilities(ServerSentCapabilities.None);
        _reporter.NotifyEffectiveConfigChanged();
        await AssertCompletes(_reporter.WaitForIdleAsync());
        Assert.Equal(2, _plugin.GetterCalls);
        Assert.Equal(2, _reportSender.EffectiveConfigReports.Count);

        _reporter.HandleServerCapabilities(ServerSentCapabilities.AcceptsStatus);
        _reporter.NotifyEffectiveConfigChanged();
        await AssertCompletes(_reporter.WaitForIdleAsync());
        Assert.Equal(2, _plugin.GetterCalls);
        Assert.Equal(2, _reportSender.EffectiveConfigReports.Count);

        _reporter.HandleServerCapabilities(ServerCapabilities);
        await AssertCompletes(_reporter.WaitForIdleAsync());
        Assert.Equal(3, _plugin.GetterCalls);
        Assert.Equal(3, _reportSender.EffectiveConfigReports.Count);

        _reporter.Stop();
        _reporter.NotifyEffectiveConfigChanged();
        _reporter.ReportFullState();
        await AssertCompletes(_reporter.WaitForIdleAsync());
        Assert.Equal(3, _plugin.GetterCalls);
        Assert.Equal(3, _reportSender.EffectiveConfigReports.Count);
        Assert.Empty(_reportSender.FullStateReports);
    }

    [Fact]
    public async Task FullStateReportsConfigurationOnlyWhenSupported()
    {
        _reporter.HandleServerCapabilities(ServerCapabilities);
        await AssertCompletes(_reporter.WaitForIdleAsync());
        _reporter.ReportFullState();
        await AssertCompletes(_reporter.WaitForIdleAsync());

        _reporter.HandleServerCapabilities(ServerSentCapabilities.AcceptsStatus);
        _reporter.ReportFullState();
        await AssertCompletes(_reporter.WaitForIdleAsync());

        Assert.Single(_reportSender.EffectiveConfigReports);
        Assert.Collection(
            _reportSender.FullStateReports,
            report => Assert.Equal("initial", GetConfigContent(report.EffectiveConfigFiles)),
            report => Assert.Null(report.EffectiveConfigFiles));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReportingFailureIsContainedAndNextNotificationSucceeds(bool failSending)
    {
        var failReading = !failSending;
        _plugin.ReadConfig = _ =>
            failReading ? throw new InvalidOperationException("Cannot read configuration.") : CreateConfig("updated");
        _reportSender.FailEffectiveConfigSend = failSending;

        _reporter.HandleServerCapabilities(ServerCapabilities);
        await AssertCompletes(_reporter.WaitForIdleAsync());
        Assert.Empty(_reportSender.EffectiveConfigReports);

        failReading = false;
        _reportSender.FailEffectiveConfigSend = false;
        _reporter.NotifyEffectiveConfigChanged();
        await AssertCompletes(_reporter.WaitForIdleAsync());
        Assert.Equal("updated", GetConfigContent(Assert.Single(_reportSender.EffectiveConfigReports)));
    }

    [Fact]
    public async Task NotificationsAndServerCallbacksReturnWhileRefreshesAreCoalesced()
    {
        using var releaseGetter = new ManualResetEventSlim();
        var getterEntered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var activeGetters = 0;
        var getterOverlapDetected = 0;
        _plugin.ReadConfig = ReadConfigWithBlockedFirstCall;

        EffectiveConfigFile[] ReadConfigWithBlockedFirstCall(int getterCall)
        {
            var isInitialRead = getterCall == 1;
            if (Interlocked.Increment(ref activeGetters) != 1)
            {
                Interlocked.Exchange(ref getterOverlapDetected, 1);
            }

            try
            {
                if (isInitialRead)
                {
                    getterEntered.TrySetResult(true);
                    releaseGetter.Wait();
                }

                return CreateConfig(isInitialRead ? "initial" : "updated");
            }
            finally
            {
                Interlocked.Decrement(ref activeGetters);
            }
        }

        var notificationsTask = Task.Run(async () =>
        {
            _reporter.HandleServerCapabilities(ServerCapabilities);
            await getterEntered.Task.ConfigureAwait(false);
            for (var i = 0; i < 3; i++)
            {
                _reporter.NotifyEffectiveConfigChanged();
            }

            _reporter.HandleServerCapabilities(ServerCapabilities);
            _reporter.ReportFullState();
        });
        try
        {
            await AssertCompletes(notificationsTask);
            Assert.Equal(1, _plugin.GetterCalls);
        }
        finally
        {
            releaseGetter.Set();
            await AssertCompletes(notificationsTask);
            await AssertCompletes(_reporter.WaitForIdleAsync());
        }

        Assert.Equal(2, _plugin.GetterCalls);
        Assert.Equal(0, getterOverlapDetected);
        Assert.Equal("initial", GetConfigContent(Assert.Single(_reportSender.EffectiveConfigReports)));
        Assert.Equal("updated", GetConfigContent(Assert.Single(_reportSender.FullStateReports).EffectiveConfigFiles));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CapabilityChangesDiscardAnActiveRefreshAndPreserveFullStateRequests(bool restoreWhileBlocked)
    {
        _reporter.HandleServerCapabilities(ServerCapabilities);
        await AssertCompletes(_reporter.WaitForIdleAsync());

        using var releaseGetter = new ManualResetEventSlim();
        var getterEntered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _plugin.ReadConfig = getterCall =>
        {
            if (getterCall == 2)
            {
                getterEntered.TrySetResult(true);
                releaseGetter.Wait();
                return CreateConfig("stale");
            }

            return CreateConfig("initial");
        };

        var capabilitiesTask = Task.Run(async () =>
        {
            _reporter.ReportFullState();
            await getterEntered.Task.ConfigureAwait(false);
            _reporter.HandleServerCapabilities(ServerSentCapabilities.AcceptsStatus);
            if (restoreWhileBlocked)
            {
                _reporter.HandleServerCapabilities(ServerCapabilities);
            }
        });
        try
        {
            await AssertCompletes(capabilitiesTask);
        }
        finally
        {
            releaseGetter.Set();
            await AssertCompletes(capabilitiesTask);
            await AssertCompletes(_reporter.WaitForIdleAsync());
        }

        Assert.Single(_reportSender.EffectiveConfigReports);
        var fullState = Assert.Single(_reportSender.FullStateReports);
        if (restoreWhileBlocked)
        {
            Assert.Equal("initial", GetConfigContent(fullState.EffectiveConfigFiles));
            Assert.Equal(3, _plugin.GetterCalls);
        }
        else
        {
            Assert.Null(fullState.EffectiveConfigFiles);
            Assert.Equal(2, _plugin.GetterCalls);
        }
    }

    [Fact]
    public async Task ShutdownDiscardsPendingWorkWithoutWaitingForAnActiveGetter()
    {
        using var releaseGetter = new ManualResetEventSlim();
        var getterEntered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _plugin.ReadConfig = _ =>
        {
            getterEntered.TrySetResult(true);
            releaseGetter.Wait();
            return CreateConfig("updated");
        };

        var stopTask = Task.Run(async () =>
        {
            _reporter.HandleServerCapabilities(ServerCapabilities);
            await getterEntered.Task.ConfigureAwait(false);
            _reporter.NotifyEffectiveConfigChanged();
            _reporter.ReportFullState();
            _reporter.Stop();
        });
        try
        {
            await AssertCompletes(stopTask);
        }
        finally
        {
            releaseGetter.Set();
            await AssertCompletes(stopTask);
            await AssertCompletes(_reporter.WaitForIdleAsync());
        }

        Assert.Equal(1, _plugin.GetterCalls);
        Assert.Empty(_reportSender.EffectiveConfigReports);
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

        public bool FailEffectiveConfigSend { get; set; }

        public void SendEffectiveConfig(IReadOnlyCollection<EffectiveConfigFile> files)
        {
            if (FailEffectiveConfigSend)
            {
                throw new InvalidOperationException("Cannot queue configuration.");
            }

            EffectiveConfigReports.Add(files);
        }

        public void SendFullStateReport(FullStateReport report)
        {
            FullStateReports.Add(report);
        }
    }

    private sealed class TestEffectiveConfigPlugin : IProvideEffectiveConfig
    {
        private int _getterCalls;

        public Func<int, IReadOnlyCollection<EffectiveConfigFile>> ReadConfig { get; set; } = _ => CreateConfig("initial");

        public int GetterCalls => Volatile.Read(ref _getterCalls);

        public IReadOnlyCollection<EffectiveConfigFile> GetEffectiveConfig()
        {
            var getterCall = Interlocked.Increment(ref _getterCalls);
            return ReadConfig(getterCall);
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
