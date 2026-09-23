// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Globalization;
using IntegrationTests.Helpers;
using OpAmp.Proto.V1;
using OpenTelemetry.AutoInstrumentation.Configurations;
using OpenTelemetry.AutoInstrumentation.OpAmp;
using OpenTelemetry.AutoInstrumentation.Plugins;
using OpenTelemetry.OpAmp.Client.Listeners;
using OpenTelemetry.OpAmp.Client.Messages;
using OpenTelemetry.Proto.Trace.V1;
using OpenTelemetry.Resources;

namespace IntegrationTests;

public class OpAmpTests : TestHelper
{
    private const string PrimaryCustomCapability = "com.example.opamp.primary/1";
    private const string SecondaryCustomCapability = "com.example.opamp.secondary/1";

    public OpAmpTests(ITestOutputHelper output)
#if NET
        : base("Http", output)
#else
        : base("Http.NetFramework", output)
#endif
    {
    }

    [Fact]
    public void OpAmpClient_CanConnect()
    {
        using var server = new MockOpAmpServer(Output);

        SetEnvironmentVariable("OTEL_DOTNET_AUTO_OPAMP_ENABLED", "true");
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_OPAMP_SERVER_URL", $"http://localhost:{server.Port}/v1/opamp");
        SetEnvironmentVariable(
            "OTEL_RESOURCE_ATTRIBUTES",
            "opamp.test=true,service.namespace=my-namespace");

        AgentDescription? agentDescriptionFrame = null;

        server.Expect(
            f =>
            {
                agentDescriptionFrame = f.AgentDescription;
                return f.AgentDescription != null;
            },
            "Has AgentDescription frame");

        server.Expect(f => f.AgentDisconnect != null, "Has AgentDisconnect frame");

        RunTestApplication();

        server.AssertExpectations();

        Assert.NotNull(agentDescriptionFrame);
        Assert.Contains(
            agentDescriptionFrame.IdentifyingAttributes,
            a => a.Key == "service.namespace" && a.Value.StringValue == "my-namespace");
        Assert.Contains(agentDescriptionFrame.NonIdentifyingAttributes, a => a.Key == "opamp.test");
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void OpAmpClient_DoesNotCollectInternalTransportTraces()
    {
        using var server = new MockOpAmpServer(Output);
        using var collector = new MockSpansCollector(Output);

        SetExporter(collector);
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_OPAMP_ENABLED", "true");
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_OPAMP_SERVER_URL", $"http://localhost:{server.Port}/v1/opamp");

#if NET
        collector.Expect(
            "System.Net.Http",
            span => span.Kind == Span.Types.SpanKind.Client && !IsOpAmpTransportSpan(span, server.Port),
            "Has application HTTP client span");
        collector.Expect(
            "Microsoft.AspNetCore",
            span => span.Kind == Span.Types.SpanKind.Server,
            "Has application ASP.NET Core server span");
        collector.Expect(
            "TestApplication.Http",
            span => span.Name == "manual span",
            "Has application manual span");
#else
        collector.Expect(
            "OpenTelemetry.Instrumentation.Http.HttpWebRequest",
            span => span.Kind == Span.Types.SpanKind.Client && !IsOpAmpTransportSpan(span, server.Port),
            "Has application HTTP client span");
#endif
        collector.ExpectAllCollected(collected => collected.All(span => !IsOpAmpTransportSpan(span.Span, server.Port)));

        server.Expect(f => f.AgentDescription != null, "Has AgentDescription frame");
        server.Expect(f => f.AgentDisconnect != null, "Has AgentDisconnect frame");

        RunTestApplication();

        server.AssertExpectations();
        collector.AssertExpectations();
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public async Task OpAmpClient_QueuesCustomCapabilitiesReportedBeforeStartup()
    {
        using var server = new MockOpAmpServer(Output);
        PluginOpAmpClient? client = null;
        using var manager = CreateOpAmpManager(
            server,
            preparedClient =>
            {
                client = preparedClient;
                preparedClient.ReportCustomCapabilities([PrimaryCustomCapability]);
                preparedClient.ReportCustomCapabilities([SecondaryCustomCapability]);
                Assert.Equal(0, server.GetFrameCount(_ => true));
            });

        try
        {
            server.WaitForFrameCount(
                frame => frame.CustomCapabilities?.Capabilities.SequenceEqual([SecondaryCustomCapability]) == true,
                1,
                "latest custom capabilities reported after startup");
            await client!.FlushAsync(CancellationToken.None).ConfigureAwait(true);

            Assert.Equal(1, server.GetFrameCount(frame => frame.CustomCapabilities != null));
        }
        finally
        {
            await manager.StopOpAmpClientAsync().ConfigureAwait(true);
        }

        await Assert.ThrowsAsync<InvalidOperationException>(() => client!.FlushAsync(CancellationToken.None)).ConfigureAwait(true);
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public async Task OpAmpClient_ReportsCustomCapabilityState()
    {
        using var server = new MockOpAmpServer(Output, customCapabilities: [PrimaryCustomCapability]);

        using var manager = CreateOpAmpManager(server);
        var client = new PluginOpAmpClient(manager);
        using var serverCapabilitiesReceived = new ServerCustomCapabilitiesListener(PrimaryCustomCapability);
        client.Subscribe(serverCapabilitiesReceived);

        try
        {
            client.ReportCustomCapabilities([SecondaryCustomCapability, PrimaryCustomCapability, PrimaryCustomCapability]);
            server.WaitForFrameCount(HasReportedCustomCapabilities, 1, "normalized custom capabilities");
            Assert.True(serverCapabilitiesReceived.Wait(TestTimeout.Expectation));

            client.ReportCustomCapabilities([PrimaryCustomCapability, SecondaryCustomCapability]);

            client.SendCustomMessage(PrimaryCustomCapability, "supported", new byte[] { 1 });
            client.SendCustomMessage(SecondaryCustomCapability, "not-supported-by-server", new byte[] { 2 });
            client.SendCustomMessage("com.example.opamp.not-reported/1", "not-supported-by-client", new byte[] { 3 });
            manager.RequestFullStateReport();

            server.WaitForFrameCount(HasReportedCustomCapabilities, 2, "custom capabilities in full state");
            server.WaitForFrameCount(HasSupportedCustomMessage, 1, "mutually supported custom message");

            client.ReportCustomCapabilities([]);
            server.WaitForFrameCount(HasNoReportedCustomCapabilities, 1, "custom capabilities clearing state");

            client.SendCustomMessage(PrimaryCustomCapability, "after-clear", new byte[] { 4 });
            manager.RequestFullStateReport();

            server.WaitForFrameCount(HasNoReportedCustomCapabilities, 2, "empty custom capabilities in full state");
            Assert.Equal(4, server.GetFrameCount(frame => frame.CustomCapabilities != null));
            Assert.Equal(1, server.GetFrameCount(frame => frame.CustomMessage != null));
        }
        finally
        {
            client.Unsubscribe(serverCapabilitiesReceived);
            await manager.StopOpAmpClientAsync().ConfigureAwait(true);
        }
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public async Task OpAmpClient_RejectsCustomMessageUntilCapabilityIsSubmitted()
    {
        using var server = new MockOpAmpServer(Output, host: "127.0.0.1", customCapabilities: [PrimaryCustomCapability]);
        var (manager, plugin) = CreateStateReportingManager(server);
        var client = plugin.Client;
        plugin.BlockEffectiveConfigRequest = true;
        client.NotifyEffectiveConfigChanged();

        try
        {
            Assert.True(plugin.WaitForEffectiveConfigRequest(TestTimeout.Expectation));
            client.ReportCustomCapabilities([PrimaryCustomCapability]);
            client.SendCustomMessage(PrimaryCustomCapability, "before-publication", new byte[] { 1 });

            plugin.ReleaseEffectiveConfigRequest();
            await client.FlushAsync(CancellationToken.None).ConfigureAwait(true);

            server.WaitForFrameCount(
                frame => frame.CustomCapabilities?.Capabilities.SequenceEqual([PrimaryCustomCapability]) == true,
                1,
                "submitted custom capability");
            Assert.Equal(0, server.GetFrameCount(frame => frame.CustomMessage != null));

            client.SendCustomMessage(PrimaryCustomCapability, "after-publication", new byte[] { 2 });
            await client.FlushAsync(CancellationToken.None).ConfigureAwait(true);

            server.WaitForFrameCount(
                frame => frame.CustomMessage?.Type == "after-publication",
                1,
                "custom message after capability publication");
            Assert.Equal(
                1,
                server.GetFrameCount(
                    frame => frame.CustomCapabilities?.Capabilities.SequenceEqual([PrimaryCustomCapability]) == true));
            Assert.Equal(1, server.GetFrameCount(frame => frame.CustomMessage != null));
            Assert.Equal(0, server.GetFrameCount(frame => frame.CustomMessage?.Type == "before-publication"));
        }
        finally
        {
            plugin.ReleaseEffectiveConfigRequest();
            await manager.StopOpAmpClientAsync().ConfigureAwait(true);
        }
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public async Task OpAmpClient_ReportsProviderStateChanges()
    {
        using var server = new MockOpAmpServer(Output, host: "127.0.0.1");
        var (manager, plugin) = CreateStateReportingManager(server);
        var client = plugin.Client;

        try
        {
            server.WaitForFrameCount(
                frame => HasEffectiveConfig(frame, string.Empty, "one") &&
                         HasEffectiveConfig(frame, "second", "two"),
                1,
                "initial effective configuration with empty and named keys");
            server.WaitForFrameCount(frame => frame.RemoteConfigStatus != null, 1, "initial remote configuration status");

            plugin.EffectiveConfigState =
            [
                CreateEffectiveConfigFile("second", "two"),
                CreateEffectiveConfigFile(string.Empty, "one")
            ];
            client.NotifyEffectiveConfigChanged();
            client.NotifyRemoteConfigStatusChanged();
            await client.FlushAsync(CancellationToken.None).ConfigureAwait(true);

            Assert.Equal(1, server.GetFrameCount(frame => frame.EffectiveConfig != null));
            Assert.Equal(1, server.GetFrameCount(frame => frame.RemoteConfigStatus != null));

            plugin.EffectiveConfigState = [CreateEffectiveConfigFile("config", "changed")];
            plugin.RemoteConfigStatusState = CreateRemoteConfigStatus("changed-hash");
            client.NotifyEffectiveConfigChanged();
            client.NotifyRemoteConfigStatusChanged();
            await client.FlushAsync(CancellationToken.None).ConfigureAwait(true);

            server.WaitForFrameCount(frame => HasEffectiveConfig(frame, "config", "changed"), 1, "changed effective configuration");
            server.WaitForFrameCount(frame => HasRemoteConfigStatus(frame, "changed-hash"), 1, "changed remote configuration status");
            Assert.Equal(2, server.GetFrameCount(frame => frame.EffectiveConfig != null));
            Assert.Equal(2, server.GetFrameCount(frame => frame.RemoteConfigStatus != null));

            manager.HandleServerCapabilities(ServerSentCapabilities.None);
            await client.FlushAsync(CancellationToken.None).ConfigureAwait(true);
            Assert.Equal(2, server.GetFrameCount(frame => frame.EffectiveConfig != null));
            Assert.Equal(2, server.GetFrameCount(frame => frame.RemoteConfigStatus != null));

            manager.HandleServerCapabilities(ServerSentCapabilities.AcceptsStatus);
            plugin.EffectiveConfigState = [CreateEffectiveConfigFile("config", "changed-while-disabled")];
            plugin.RemoteConfigStatusState = CreateRemoteConfigStatus("changed-while-disabled-hash");
            client.NotifyEffectiveConfigChanged();
            client.NotifyRemoteConfigStatusChanged();
            await client.FlushAsync(CancellationToken.None).ConfigureAwait(true);
            Assert.Equal(2, server.GetFrameCount(frame => frame.EffectiveConfig != null));
            Assert.Equal(2, server.GetFrameCount(frame => frame.RemoteConfigStatus != null));

            manager.HandleServerCapabilities(
                ServerSentCapabilities.AcceptsEffectiveConfig |
                ServerSentCapabilities.OffersRemoteConfig);
            await client.FlushAsync(CancellationToken.None).ConfigureAwait(true);
            server.WaitForFrameCount(
                frame => HasEffectiveConfig(frame, "config", "changed-while-disabled"),
                1,
                "effective configuration changed while server support was disabled");
            server.WaitForFrameCount(
                frame => HasRemoteConfigStatus(frame, "changed-while-disabled-hash"),
                1,
                "remote status changed while server support was disabled");
            Assert.Equal(3, server.GetFrameCount(frame => frame.EffectiveConfig != null));
            Assert.Equal(3, server.GetFrameCount(frame => frame.RemoteConfigStatus != null));

            manager.HandleServerCapabilities(ServerSentCapabilities.AcceptsStatus);
            manager.HandleServerCapabilities(
                ServerSentCapabilities.AcceptsEffectiveConfig |
                ServerSentCapabilities.OffersRemoteConfig);
            await client.FlushAsync(CancellationToken.None).ConfigureAwait(true);
            Assert.Equal(3, server.GetFrameCount(frame => frame.EffectiveConfig != null));
            Assert.Equal(3, server.GetFrameCount(frame => frame.RemoteConfigStatus != null));

            manager.HandleServerCapabilities(ServerSentCapabilities.AcceptsStatus);
            plugin.ThrowOnStateRequest = true;
            manager.HandleServerCapabilities(
                ServerSentCapabilities.AcceptsEffectiveConfig |
                ServerSentCapabilities.OffersRemoteConfig);
            await client.FlushAsync(CancellationToken.None).ConfigureAwait(true);
            Assert.Equal(3, server.GetFrameCount(frame => frame.EffectiveConfig != null));
            Assert.Equal(3, server.GetFrameCount(frame => frame.RemoteConfigStatus != null));

            manager.RequestFullStateReport();
            await client.FlushAsync(CancellationToken.None).ConfigureAwait(true);
            server.WaitForFrameCount(
                frame => HasFullState(frame, "changed-while-disabled", "changed-while-disabled-hash"),
                1,
                "full state retains the last valid provider snapshots");

            plugin.ThrowOnStateRequest = false;
            plugin.RemoteConfigStatusState = null;
            client.NotifyRemoteConfigStatusChanged();
            manager.RequestFullStateReport();
            await client.FlushAsync(CancellationToken.None).ConfigureAwait(true);
            server.WaitForFrameCount(
                frame => frame.CustomCapabilities != null &&
                         frame.EffectiveConfig != null &&
                         frame.RemoteConfigStatus == null,
                1,
                "full state omits a nullable remote status");
        }
        finally
        {
            await manager.StopOpAmpClientAsync().ConfigureAwait(true);
        }
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public async Task OpAmpClient_RespondsToCombinedFullStateFlag()
    {
        const ServerSentFlags combinedFlags =
            ServerSentFlags.ReportFullState |
            ServerSentFlags.ReportAvailableComponents;
        using var server = new MockOpAmpServer(
            Output,
            host: "127.0.0.1",
            firstResponseFlags: (ulong)combinedFlags);
        var stateReportingManager = CreateStateReportingManager(server);
        using var manager = stateReportingManager.Manager;
        var client = stateReportingManager.Plugin.Client;

        try
        {
            server.WaitForFrameCount(
                frame => frame.CustomCapabilities != null &&
                         HasEffectiveConfig(frame, string.Empty, "one") &&
                         HasRemoteConfigStatus(frame, "initial-hash"),
                1,
                "full state response uses capabilities from the same frame");
            await client.FlushAsync(CancellationToken.None).ConfigureAwait(true);
        }
        finally
        {
            await manager.StopOpAmpClientAsync().ConfigureAwait(true);
        }
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public async Task OpAmpClient_DoesNotResendCachedEffectiveConfigWhenReenabledRefreshIsInvalid()
    {
        using var server = new MockOpAmpServer(Output, host: "127.0.0.1");
        var (manager, plugin) = CreateStateReportingManager(server);
        var client = plugin.Client;

        try
        {
            server.WaitForFrameCount(frame => frame.EffectiveConfig != null, 1, "initial effective configuration");
            plugin.EffectiveConfigState = [CreateEffectiveConfigFile("config", "changed")];
            client.NotifyEffectiveConfigChanged();
            await client.FlushAsync(CancellationToken.None).ConfigureAwait(true);
            server.WaitForFrameCount(frame => HasEffectiveConfig(frame, "config", "changed"), 1, "changed effective configuration");

            manager.HandleServerCapabilities(ServerSentCapabilities.AcceptsStatus);
            plugin.ReturnInvalidEffectiveConfig = true;
            manager.HandleServerCapabilities(
                ServerSentCapabilities.AcceptsEffectiveConfig |
                ServerSentCapabilities.OffersRemoteConfig);
            await client.FlushAsync(CancellationToken.None).ConfigureAwait(true);
            Assert.Equal(2, server.GetFrameCount(frame => frame.EffectiveConfig != null));

            manager.RequestFullStateReport();
            await client.FlushAsync(CancellationToken.None).ConfigureAwait(true);
            server.WaitForFrameCount(
                frame => HasFullState(frame, "changed", "initial-hash"),
                1,
                "full state retains cached effective configuration after invalid output");
        }
        finally
        {
            await manager.StopOpAmpClientAsync().ConfigureAwait(true);
        }
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public async Task OpAmpClient_OmitsProviderStateWhenInitialRefreshFails()
    {
        using var server = new MockOpAmpServer(Output, host: "127.0.0.1");
        var (manager, plugin) = CreateStateReportingManager(server, configuredPlugin => configuredPlugin.ThrowOnStateRequest = true);
        var client = plugin.Client;

        try
        {
            manager.RequestFullStateReport();
            await client.FlushAsync(CancellationToken.None).ConfigureAwait(true);
            server.WaitForFrameCount(
                frame => frame.CustomCapabilities != null &&
                         frame.EffectiveConfig == null &&
                         frame.RemoteConfigStatus == null,
                1,
                "full state omits provider state when no valid snapshot exists");
            Assert.Equal(0, server.GetFrameCount(frame => frame.EffectiveConfig != null));
            Assert.Equal(0, server.GetFrameCount(frame => frame.RemoteConfigStatus != null));
        }
        finally
        {
            await manager.StopOpAmpClientAsync().ConfigureAwait(true);
        }
    }

    private static OpAmpManager CreateOpAmpManager(
        MockOpAmpServer server,
        Action<PluginOpAmpClient>? configurePreparedClient = null)
    {
        var settings = new OpAmpSettings();
        settings.LoadEnvVar(new Configuration(
            false,
            new OpAmpServerConfigurationSource($"http://localhost:{server.Port}/v1/opamp")));

        var pluginManager = new PluginManager(new PluginsSettings());
        Assert.True(OpAmpManager.TryCreate(Resource.Empty, settings, pluginManager, out var manager));
        configurePreparedClient?.Invoke(new PluginOpAmpClient(manager));
        pluginManager.Initialized();
        manager.StartClient();
        return manager;
    }

    private static (OpAmpManager Manager, StateReportingOpAmpPlugin Plugin) CreateStateReportingManager(
        MockOpAmpServer server,
        Action<StateReportingOpAmpPlugin>? configurePlugin = null)
    {
        var settings = new OpAmpSettings();
        settings.LoadEnvVar(new Configuration(
            false,
            new OpAmpServerConfigurationSource($"http://127.0.0.1:{server.Port}/v1/opamp")));

        var pluginSettings = new PluginsSettings();
        pluginSettings.Plugins.Add(typeof(StateReportingOpAmpPlugin).AssemblyQualifiedName!);
        var pluginManager = new PluginManager(pluginSettings);
        var plugin = Assert.IsType<StateReportingOpAmpPlugin>(Assert.Single(pluginManager.Plugins).Instance);
        configurePlugin?.Invoke(plugin);
        Assert.True(OpAmpManager.TryCreate(Resource.Empty, settings, pluginManager, out var manager));
        pluginManager.Initialized();
        manager.StartClient();
        Assert.True(plugin.WaitForStarted(TestTimeout.Expectation));
        return (manager, plugin);
    }

    private static EffectiveConfigFile CreateEffectiveConfigFile(string fileName, string content)
    {
        return new EffectiveConfigFile(System.Text.Encoding.UTF8.GetBytes(content), "text/plain", fileName);
    }

    private static RemoteConfigStatusReport CreateRemoteConfigStatus(string hash)
    {
        return new RemoteConfigStatusReport(
            System.Text.Encoding.UTF8.GetBytes(hash),
            RemoteConfigStatusCode.Applied);
    }

    private static bool HasEffectiveConfig(AgentToServer frame, string fileName, string content)
    {
        return frame.EffectiveConfig?.ConfigMap?.ConfigMap.TryGetValue(fileName, out var file) == true &&
               file.Body.ToStringUtf8() == content;
    }

    private static bool HasRemoteConfigStatus(AgentToServer frame, string hash)
    {
        return frame.RemoteConfigStatus?.LastRemoteConfigHash.ToStringUtf8() == hash;
    }

    private static bool HasFullState(AgentToServer frame, string effectiveConfig, string remoteConfigHash)
    {
        return frame.CustomCapabilities != null &&
               HasEffectiveConfig(frame, "config", effectiveConfig) &&
               HasRemoteConfigStatus(frame, remoteConfigHash);
    }

    private static bool HasReportedCustomCapabilities(AgentToServer frame)
    {
        return frame.CustomCapabilities != null &&
               frame.CustomCapabilities.Capabilities.SequenceEqual(
                   [PrimaryCustomCapability, SecondaryCustomCapability]);
    }

    private static bool HasNoReportedCustomCapabilities(AgentToServer frame)
    {
        return frame.CustomCapabilities != null && frame.CustomCapabilities.Capabilities.Count == 0;
    }

    private static bool HasSupportedCustomMessage(AgentToServer frame)
    {
        return frame.CustomMessage?.Capability == PrimaryCustomCapability && frame.CustomMessage.Type == "supported";
    }

    private static bool IsOpAmpTransportSpan(Span span, int serverPort)
    {
        return span.Kind == Span.Types.SpanKind.Client &&
               HasOpAmpEndpointAttribute(span) &&
               HasServerPortAttribute(span, serverPort);
    }

    private static bool HasOpAmpEndpointAttribute(Span span)
    {
        return Contains(span.Name, "/v1/opamp") ||
               span.Attributes.Any(attribute => Contains(attribute.Value.StringValue, "/v1/opamp"));
    }

    private static bool HasServerPortAttribute(Span span, int serverPort)
    {
        var serverPortString = serverPort.ToString(CultureInfo.InvariantCulture);

        return span.Attributes.Any(attribute =>
            (attribute.Key == "server.port" && attribute.Value.IntValue == serverPort) ||
            Contains(attribute.Value.StringValue, $":{serverPortString}/v1/opamp"));
    }

    private static bool Contains(string value, string expected)
    {
#if NET
        return value.Contains(expected, StringComparison.OrdinalIgnoreCase);
#else
        return value.IndexOf(expected, StringComparison.OrdinalIgnoreCase) >= 0;
#endif
    }

    private sealed class OpAmpServerConfigurationSource : IConfigurationSource
    {
        private readonly string _serverUrl;

        public OpAmpServerConfigurationSource(string serverUrl)
        {
            _serverUrl = serverUrl;
        }

        public string? GetString(string key)
        {
            return key == ConfigurationKeys.OpAmpServerUrl ? _serverUrl : null;
        }

        public int? GetInt32(string key) => null;

        public double? GetDouble(string key) => null;

        public bool? GetBool(string key) => null;
    }

    private sealed class ServerCustomCapabilitiesListener : IOpAmpListener<CustomCapabilitiesMessage>, IDisposable
    {
        private readonly string _expectedCapability;
        private readonly ManualResetEventSlim _received = new();

        public ServerCustomCapabilitiesListener(string expectedCapability)
        {
            _expectedCapability = expectedCapability;
        }

        public void HandleMessage(CustomCapabilitiesMessage message)
        {
            if (message.Capabilities.Contains(_expectedCapability, StringComparer.Ordinal))
            {
                _received.Set();
            }
        }

        public bool Wait(TimeSpan timeout) => _received.Wait(timeout);

        public void Dispose() => _received.Dispose();
    }
}
