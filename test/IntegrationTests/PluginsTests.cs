// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Globalization;
using IntegrationTests.Helpers;
using OpAmp.Proto.V1;
using OpenTelemetry.Proto.Trace.V1;

namespace IntegrationTests;

public class PluginsTests : TestHelper
{
    private const string PostStartCustomCapability = "com.example.opamp.post-start";
    private const string PluginInitPattern = "Plugin.Initializing() invoked.";
    private const string PluginInitDonePattern = "Plugin.Initialized() invoked.";
    private const string OpAmpCustomMessagePattern = "Plugin.HandleMessage(CustomMessageMessage) invoked: Utf8String.";

    public PluginsTests(ITestOutputHelper output)
        : base("Plugins", output)
    {
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void InitPlugin()
    {
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_PLUGINS", "TestApplication.Plugins.Plugin, TestApplication.Plugins, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");

        var (standardOutput, _, _) = RunTestApplication();

        Assert.Contains(PluginInitPattern, standardOutput, StringComparison.Ordinal);
        Assert.Contains(PluginInitDonePattern, standardOutput, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void InitPluginOnlyOnce()
    {
        var pluginName =
            "TestApplication.Plugins.Plugin, TestApplication.Plugins, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null";

        // Replace space with double and triple spaces.
        // Use escape for space as it is not easy to count spaces in the string visually.
        // So, we make sure that plugin with multiple spaces still loads correctly once (type was resolved)
        // But we block second loading of it, even with different original string representation for a type
#if NETFRAMEWORK
        var pluginNameDoubleSpace = pluginName.Replace("\x20", "\x20\x20");
        var pluginNameTripleSpace = pluginName.Replace("\x20", "\x20\x20\x20");
#else
        var pluginNameDoubleSpace = pluginName.Replace("\x20", "\x20\x20", StringComparison.Ordinal);
        var pluginNameTripleSpace = pluginName.Replace("\x20", "\x20\x20\x20", StringComparison.Ordinal);
#endif

        SetEnvironmentVariable(
            "OTEL_DOTNET_AUTO_PLUGINS",
            $"{pluginNameDoubleSpace} : {pluginNameTripleSpace}");

        var (standardOutput, _, _) = RunTestApplication();
        var firstIndex = standardOutput.IndexOf(PluginInitPattern, StringComparison.Ordinal);
        Assert.True(firstIndex != -1, "Plugin not initialized");
        Assert.True(firstIndex == standardOutput.LastIndexOf(PluginInitPattern, StringComparison.Ordinal), "Plugin initialized more than once");
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void SubmitsTraces()
    {
        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);
        collector.Expect("MyCompany.MyProduct.MyLibrary");
#if NETFRAMEWORK
        collector.Expect("OpenTelemetry.Instrumentation.Http.HttpWebRequest", span => span.Attributes.Any(att => att.Key == "example.plugin"));
#else
        collector.Expect("System.Net.Http", span => span.Attributes.Any(att => att.Key == "example.plugin"));
#endif

        SetEnvironmentVariable("OTEL_DOTNET_AUTO_PLUGINS", "TestApplication.Plugins.Plugin, TestApplication.Plugins, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");
        var (standardOutput, _, _) = RunTestApplication();

        collector.AssertExpectations();
        Assert.Contains("Plugin.ConfigureTracesOptions(OtlpExporterOptions options) invoked.", standardOutput, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void SubmitMetrics()
    {
        using var collector = new MockMetricsCollector(Output);
        SetExporter(collector);
        collector.Expect("MyCompany.MyProduct.MyLibrary");

        SetEnvironmentVariable("OTEL_DOTNET_AUTO_PLUGINS", "TestApplication.Plugins.Plugin, TestApplication.Plugins, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");

        var (standardOutput, _, _) = RunTestApplication();

        collector.AssertExpectations();
        Assert.Contains("Plugin.ConfigureMetricsOptions(OtlpExporterOptions options) invoked.", standardOutput, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void OpAmpInitializedWithEnvironmentVariables()
    {
        AssertOpAmpInitialized(() =>
        {
            SetEnvironmentVariable("OTEL_DOTNET_AUTO_PLUGINS", "TestApplication.Plugins.Plugin, TestApplication.Plugins, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");
            SetEnvironmentVariable("OTEL_DOTNET_AUTO_OPAMP_ENABLED", "true");
            SetEnvironmentVariable("OTEL_DOTNET_AUTO_OPAMP_MAX_PENDING_CUSTOM_MESSAGES", "123");
            SetEnvironmentVariable("OTEL_DOTNET_AUTO_OPAMP_MAX_PENDING_CUSTOM_MESSAGE_BYTES", "456");
        });
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void OpAmpInitializedWithFileBasedConfiguration()
    {
        AssertOpAmpInitialized(() => EnableFileBasedConfig());
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void OpAmpPostStartReportsAreFlushedWithoutTransportSpans()
    {
        using var server = new MockOpAmpServer(
            Output,
            host: "127.0.0.1",
            customCapabilities: [PostStartCustomCapability]);
        using var collector = new MockSpansCollector(Output);

        SetExporter(collector);
        collector.Expect("MyCompany.MyProduct.MyLibrary");
#if NETFRAMEWORK
        collector.Expect(
            "OpenTelemetry.Instrumentation.Http.HttpWebRequest",
            span => span.Kind == Span.Types.SpanKind.Client && !IsOpAmpTransportSpan(span, server.Port),
            "Has application HTTP client span");
#else
        collector.Expect(
            "System.Net.Http",
            span => span.Kind == Span.Types.SpanKind.Client && !IsOpAmpTransportSpan(span, server.Port),
            "Has application HTTP client span");
#endif
        collector.ExpectAllCollected(collected => collected.All(span => !IsOpAmpTransportSpan(span.Span, server.Port)));

        server.Expect(frame => frame.AgentDescription != null, "Has AgentDescription frame");
        server.Expect(frame => frame.AgentDisconnect != null, "Has AgentDisconnect frame");

        SetEnvironmentVariable("OTEL_DOTNET_AUTO_PLUGINS", "TestApplication.Plugins.Plugin, TestApplication.Plugins, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_OPAMP_ENABLED", "true");
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_OPAMP_SERVER_URL", $"http://127.0.0.1:{server.Port}/v1/opamp");
        SetEnvironmentVariable("TEST_OPAMP_POST_START_REPORTING", "true");

        var (standardOutput, _, _) = RunTestApplication();

        server.AssertExpectations();
        collector.AssertExpectations();
        Assert.Contains("Plugin post-start OpAMP reporting completed.", standardOutput, StringComparison.Ordinal);
        Assert.True(server.GetFrameCount(HasPostStartCustomCapabilities) > 0);
        Assert.True(server.GetFrameCount(HasPostStartCustomMessage) > 0);
        Assert.True(server.GetFrameCount(HasPostStartEffectiveConfig) > 0);
        Assert.True(server.GetFrameCount(HasPostStartRemoteConfigStatus) > 0);
    }

    [Theory]
    [InlineData(
        "TestApplication.Plugins.Plugin, TestApplication.Plugins, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
        AgentCapabilities.ReportsEffectiveConfig | AgentCapabilities.ReportsRemoteConfig,
        false,
        false)]
    [InlineData(
        "TestApplication.Plugins.EffectiveConfigOnlyOpAmpPlugin, TestApplication.Plugins, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
        AgentCapabilities.ReportsEffectiveConfig,
        false,
        false)]
    [InlineData(
        "TestApplication.Plugins.RemoteConfigStatusOnlyOpAmpPlugin, TestApplication.Plugins, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
        AgentCapabilities.ReportsRemoteConfig,
        false,
        true)]
    [InlineData(
        "TestApplication.Plugins.RemoteConfigStatusOnlyOpAmpPlugin, TestApplication.Plugins, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
        (AgentCapabilities)0,
        false,
        false)]
    [InlineData(
        "TestApplication.Plugins.SettingsOnlyOpAmpPlugin, TestApplication.Plugins, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
        (AgentCapabilities)0,
        true,
        false)]
    [Trait("Category", "EndToEnd")]
    public void OpAmpReportingCapabilitiesRequireProvidersAndPluginOptIn(
        string pluginName,
        AgentCapabilities expectedReportingCapabilities,
        bool expectsRemoteConfigAcceptance,
        bool enableRemoteConfigStatusReporting)
    {
        const AgentCapabilities reportingCapabilities =
            AgentCapabilities.ReportsEffectiveConfig |
            AgentCapabilities.ReportsRemoteConfig;
        var expectsEffectiveConfig = expectedReportingCapabilities.HasFlag(AgentCapabilities.ReportsEffectiveConfig);
        var expectsRemoteConfigStatus = expectedReportingCapabilities.HasFlag(AgentCapabilities.ReportsRemoteConfig);
        using var server = new MockOpAmpServer(Output, host: "127.0.0.1");

        server.Expect(
            frame => frame.AgentDescription != null &&
                     ((AgentCapabilities)frame.Capabilities & reportingCapabilities) == expectedReportingCapabilities &&
                     (((AgentCapabilities)frame.Capabilities & AgentCapabilities.AcceptsRemoteConfig) != 0) == expectsRemoteConfigAcceptance,
            "Initial frame reports only explicitly enabled provider-backed capabilities and preserves remote configuration acceptance");
        if (expectsEffectiveConfig)
        {
            server.Expect(frame => frame.EffectiveConfig != null, "Has initial effective configuration frame");
        }

        if (expectsRemoteConfigStatus)
        {
            server.Expect(frame => frame.RemoteConfigStatus != null, "Has initial remote configuration status frame");
        }

        server.Expect(frame => frame.AgentDisconnect != null, "Has AgentDisconnect frame");

        SetEnvironmentVariable("OTEL_DOTNET_AUTO_PLUGINS", pluginName);
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_OPAMP_ENABLED", "true");
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_OPAMP_SERVER_URL", $"http://127.0.0.1:{server.Port}/v1/opamp");
        if (enableRemoteConfigStatusReporting)
        {
            SetEnvironmentVariable("TEST_OPAMP_REMOTE_CONFIG_STATUS_REPORTING_ENABLED", "true");
        }

        RunTestApplication();

        server.AssertExpectations();
        Assert.Equal(expectsEffectiveConfig ? 1 : 0, server.GetFrameCount(frame => frame.EffectiveConfig != null));
        Assert.Equal(expectsRemoteConfigStatus ? 1 : 0, server.GetFrameCount(frame => frame.RemoteConfigStatus != null));
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void FirstConfiguredOpAmpPluginOwnsTheClient()
    {
        const string selectedPlugin = "SettingsOnlyOpAmpPlugin";
        const string ignoredPlugin = "Plugin";
        const string selectedPluginType = "TestApplication.Plugins.SettingsOnlyOpAmpPlugin";
        const string ignoredPluginType = "TestApplication.Plugins.Plugin";
        const string selectedPluginName = $"{selectedPluginType}, TestApplication.Plugins, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null";
        const string ignoredPluginName = $"{ignoredPluginType}, TestApplication.Plugins, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null";
        const AgentCapabilities reportingCapabilities =
            AgentCapabilities.ReportsEffectiveConfig |
            AgentCapabilities.ReportsRemoteConfig;
        using var server = new MockOpAmpServer(Output, host: "127.0.0.1");

        server.Expect(
            frame => frame.AgentDescription != null &&
                     ((AgentCapabilities)frame.Capabilities & reportingCapabilities) == 0 &&
                     ((AgentCapabilities)frame.Capabilities & AgentCapabilities.AcceptsRemoteConfig) != 0,
            "Initial frame reflects only the selected OpAMP plugin");
        server.Expect(frame => frame.AgentDisconnect != null, "Has AgentDisconnect frame");

        SetEnvironmentVariable("OTEL_DOTNET_AUTO_PLUGINS", $"{selectedPluginName}:{ignoredPluginName}");
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_OPAMP_ENABLED", "true");
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_OPAMP_SERVER_URL", $"http://127.0.0.1:{server.Port}/v1/opamp");
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOGGER", "console");

        var (standardOutput, _, _) = RunTestApplication();
        var outputLines = standardOutput.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

        server.AssertExpectations();
        Assert.Equal(0, server.GetFrameCount(frame => frame.EffectiveConfig != null));
        Assert.Equal(0, server.GetFrameCount(frame => frame.RemoteConfigStatus != null));
        Assert.Contains($"{selectedPlugin}.Initializing() invoked.", outputLines);
        Assert.Contains($"{selectedPlugin}.Initialized() invoked.", outputLines);
        Assert.Contains($"{selectedPlugin}.ConfigureOpAmpOptions() invoked.", outputLines);
        Assert.Contains($"{selectedPlugin}.ConfigureOpAmpClient() invoked.", outputLines);
        Assert.Contains($"{selectedPlugin}.AfterOpAmpClientStarted() invoked.", outputLines);
        Assert.Contains($"{selectedPlugin}.BeforeOpAmpClientStopped() invoked.", outputLines);
        Assert.Contains($"{ignoredPlugin}.Initializing() invoked.", outputLines);
        Assert.Contains($"{ignoredPlugin}.Initialized() invoked.", outputLines);
        Assert.DoesNotContain($"{ignoredPlugin}.ConfigureOpAmpOptions() invoked.", outputLines);
        Assert.DoesNotContain($"{ignoredPlugin}.ConfigureOpAmpClient() invoked.", outputLines);
        Assert.DoesNotContain($"{ignoredPlugin}.AfterOpAmpClientStarted() invoked.", outputLines);
        Assert.DoesNotContain($"{ignoredPlugin}.BeforeOpAmpClientStopped() invoked.", outputLines);
        Assert.Contains("Multiple OpAMP plugins are configured.", standardOutput, StringComparison.Ordinal);
        Assert.Contains($"Using '{selectedPluginType}'", standardOutput, StringComparison.Ordinal);
        Assert.Contains(ignoredPluginType, standardOutput, StringComparison.Ordinal);
    }

    private static bool HasPostStartCustomCapabilities(AgentToServer frame)
    {
        return frame.CustomCapabilities?.Capabilities.SequenceEqual([PostStartCustomCapability]) == true;
    }

    private static bool HasPostStartCustomMessage(AgentToServer frame)
    {
        return frame.CustomMessage?.Capability == PostStartCustomCapability &&
               frame.CustomMessage.Type == "post-start";
    }

    private static bool HasPostStartEffectiveConfig(AgentToServer frame)
    {
        return frame.EffectiveConfig?.ConfigMap?.ConfigMap.TryGetValue("plugin", out var file) == true &&
               file.Body.ToStringUtf8() == "post-start";
    }

    private static bool HasPostStartRemoteConfigStatus(AgentToServer frame)
    {
        return frame.RemoteConfigStatus?.LastRemoteConfigHash.ToStringUtf8() == "post-start";
    }

    private static bool IsOpAmpTransportSpan(Span span, int serverPort)
    {
        var serverPortString = serverPort.ToString(CultureInfo.InvariantCulture);

        return span.Kind == Span.Types.SpanKind.Client &&
               (Contains(span.Name, "/v1/opamp") ||
                span.Attributes.Any(attribute => Contains(attribute.Value.StringValue, "/v1/opamp"))) &&
               span.Attributes.Any(attribute =>
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

    private void AssertOpAmpInitialized(Action configureOpAmp)
    {
        const int maxPendingCustomMessages = 123;
        const int maxPendingCustomMessageBytes = 456;
        using var server = new MockOpAmpServer(
            Output,
            host: "127.0.0.1",
            sendCustomMessageOnlyInFirstResponse: true);

        SetEnvironmentVariable("OTEL_DOTNET_AUTO_OPAMP_SERVER_URL", $"http://127.0.0.1:{server.Port}/v1/opamp");
        configureOpAmp();

        var (standardOutput, _, _) = RunTestApplication();

        Assert.Contains("Plugin.ConfigureOpAmpOptions() invoked.", standardOutput, StringComparison.Ordinal);
        Assert.Contains($"MaxPendingCustomMessages: {maxPendingCustomMessages}", standardOutput, StringComparison.Ordinal);
        Assert.Contains($"MaxPendingCustomMessageBytes: {maxPendingCustomMessageBytes}", standardOutput, StringComparison.Ordinal);
        Assert.Contains("Plugin.ConfigureOpAmpClient() invoked.", standardOutput, StringComparison.Ordinal);
        Assert.Contains(OpAmpCustomMessagePattern, standardOutput, StringComparison.Ordinal);
        Assert.Contains("Plugin.AfterOpAmpClientStarted() invoked.", standardOutput, StringComparison.Ordinal);
        Assert.Contains("Plugin.BeforeOpAmpClientStopped() invoked.", standardOutput, StringComparison.Ordinal);

        var configureOptionsIndex = standardOutput.IndexOf("Plugin.ConfigureOpAmpOptions() invoked.", StringComparison.Ordinal);
        var configureClientIndex = standardOutput.IndexOf("Plugin.ConfigureOpAmpClient() invoked.", StringComparison.Ordinal);
        var initializedIndex = standardOutput.IndexOf(PluginInitDonePattern, StringComparison.Ordinal);
        var messageIndex = standardOutput.IndexOf(OpAmpCustomMessagePattern, StringComparison.Ordinal);
        var afterStartedIndex = standardOutput.IndexOf("Plugin.AfterOpAmpClientStarted() invoked.", StringComparison.Ordinal);

        Assert.True(configureOptionsIndex < configureClientIndex);
        Assert.True(configureClientIndex < initializedIndex);
        Assert.True(initializedIndex < messageIndex);
        Assert.True(initializedIndex < afterStartedIndex);
    }
}
