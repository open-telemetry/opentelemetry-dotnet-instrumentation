// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using IntegrationTests.Helpers;
using OpAmp.Proto.V1;

namespace IntegrationTests;

public class PluginsTests : TestHelper
{
    private const string PluginInitPattern = "Plugin.Initializing() invoked.";
    private const string PluginInitDonePattern = "Plugin.Initialized() invoked.";

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
    public void InvalidPluginDoesNotCrashApplicationWhenFailFastDisabled()
    {
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_PLUGINS", "Missing.PluginType, Missing.Assembly");
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_FAIL_FAST_ENABLED", "false");

        RunTestApplication();
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
    public void OpAmpPluginConfigurationAndShutdownWithEnvironmentVariables()
    {
        AssertOpAmpPluginConfigurationAndShutdown(() =>
        {
            SetEnvironmentVariable("OTEL_DOTNET_AUTO_PLUGINS", "TestApplication.Plugins.Plugin, TestApplication.Plugins, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");
            SetEnvironmentVariable("OTEL_DOTNET_AUTO_OPAMP_ENABLED", "true");
            SetEnvironmentVariable("OTEL_DOTNET_AUTO_OPAMP_MAX_PENDING_CUSTOM_MESSAGES", "123");
            SetEnvironmentVariable("OTEL_DOTNET_AUTO_OPAMP_MAX_PENDING_CUSTOM_MESSAGE_BYTES", "456");
        });
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void OpAmpPluginConfigurationAndShutdownWithFileBasedConfiguration()
    {
        AssertOpAmpPluginConfigurationAndShutdown(() => EnableFileBasedConfig());
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void OnlyFirstConfiguredOpAmpPluginReceivesOpAmpCallbacks()
    {
        const string pluginTypePrefix = "TestApplication.Plugins.";
        const string pluginAssemblySuffix = ", TestApplication.Plugins, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null";
        const string selectedPlugin = "SelectedOpAmpPlugin";
        const string ignoredPlugin = "Plugin";
        const string selectedPluginType = pluginTypePrefix + selectedPlugin;
        const string ignoredPluginType = pluginTypePrefix + ignoredPlugin;
        const string selectedPluginName = selectedPluginType + pluginAssemblySuffix;
        const string ignoredPluginName = ignoredPluginType + pluginAssemblySuffix;
        using var server = new MockOpAmpServer(
            Output,
            capabilities: ServerCapabilities.AcceptsStatus | ServerCapabilities.AcceptsEffectiveConfig | ServerCapabilities.OffersRemoteConfig,
            flags: ServerToAgentFlags.ReportFullState);
        ulong? initialSequenceNum = null;
        server.Expect(
            frame =>
            {
                const AgentCapabilities ignoredCapabilities = AgentCapabilities.ReportsEffectiveConfig | AgentCapabilities.ReportsRemoteConfig | AgentCapabilities.AcceptsRemoteConfig;
                if (frame.AgentDescription == null || (frame.Capabilities & (ulong)ignoredCapabilities) != 0)
                {
                    return false;
                }

                initialSequenceNum = frame.SequenceNum;
                return true;
            },
            "Does not advertise reporting for an ignored provider");
        server.Expect(
            frame => initialSequenceNum.HasValue && frame.SequenceNum > initialSequenceNum.Value &&
                     frame.AgentDescription != null && frame.EffectiveConfig == null && frame.RemoteConfigStatus == null,
            "Reports full state without configuration or status from an ignored provider");

        SetEnvironmentVariable("OTEL_DOTNET_AUTO_PLUGINS", $"{selectedPluginName}:{ignoredPluginName}");
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_OPAMP_ENABLED", "true");
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_OPAMP_SERVER_URL", server.Endpoint);
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOGGER", "console");

        var standardOutput = RunOpAmpTestApplication(server);
        var outputLines = standardOutput.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

        Assert.Contains($"{selectedPlugin}.Initializing() invoked.", outputLines);
        Assert.Contains($"{selectedPlugin}.Initialized() invoked.", outputLines);
        Assert.Contains($"{ignoredPlugin}.Initializing() invoked.", outputLines);
        Assert.Contains($"{ignoredPlugin}.Initialized() invoked.", outputLines);
        Assert.Contains($"{selectedPlugin}.ConfigureOpAmpOptions() invoked.", outputLines);
        Assert.Contains($"{selectedPlugin}.ConfigureOpAmpClient() invoked.", outputLines);
        Assert.Contains($"{selectedPlugin}.BeforeOpAmpClientStopped() invoked.", outputLines);
        Assert.DoesNotContain($"{ignoredPlugin}.ConfigureOpAmpOptions() invoked.", outputLines);
        Assert.DoesNotContain($"{ignoredPlugin}.ConfigureOpAmpClient() invoked.", outputLines);
        Assert.DoesNotContain($"{ignoredPlugin}.BeforeOpAmpClientStopped() invoked.", outputLines);
        Assert.DoesNotContain($"{ignoredPlugin}.GetEffectiveConfig() invoked.", outputLines);
        Assert.DoesNotContain($"{ignoredPlugin}.GetRemoteConfigStatus() invoked.", outputLines);
        Assert.Contains("Multiple OpAMP plugins are configured.", standardOutput, StringComparison.Ordinal);
        Assert.Contains($"Using '{selectedPluginType}'", standardOutput, StringComparison.Ordinal);
        Assert.Contains(ignoredPluginType, standardOutput, StringComparison.Ordinal);
    }

    private static bool HasEffectiveConfig(AgentToServer frame, string content)
    {
        return frame.EffectiveConfig?.ConfigMap?.ConfigMap.TryGetValue("config.yaml", out var file) == true &&
               file.ContentType == "text/yaml" && file.Body.ToStringUtf8() == content;
    }

    private void AssertOpAmpPluginConfigurationAndShutdown(Action configureOpAmp)
    {
        const int maxPendingCustomMessages = 123;
        const int maxPendingCustomMessageBytes = 456;
        using var server = new MockOpAmpServer(
            Output,
            capabilities: ServerCapabilities.AcceptsStatus | ServerCapabilities.AcceptsEffectiveConfig);

        const AgentCapabilities expectedCapabilities = AgentCapabilities.ReportsEffectiveConfig | AgentCapabilities.ReportsRemoteConfig | AgentCapabilities.AcceptsRemoteConfig;
        server.Expect(frame => (frame.Capabilities & (ulong)expectedCapabilities) == (ulong)expectedCapabilities, "Advertises configuration reporting and acceptance");
        var reportedConfig = false;
        var reportedStatus = false;
        // The client may batch these fields into one frame or send them separately.
        server.Expect(
            frame =>
            {
                reportedConfig |= HasEffectiveConfig(frame, "updated: true");
                reportedStatus |= frame.RemoteConfigStatus?.LastRemoteConfigHash.ToStringUtf8() == "config-hash" &&
                                  frame.RemoteConfigStatus.Status == RemoteConfigStatuses.Applied && frame.RemoteConfigStatus.ErrorMessage.Length == 0;
                return reportedConfig && reportedStatus;
            },
            "Reports changed effective configuration and remote configuration status");

        SetEnvironmentVariable("OTEL_DOTNET_AUTO_OPAMP_SERVER_URL", server.Endpoint);
        configureOpAmp();

        var standardOutput = RunOpAmpTestApplication(server);

        Assert.Contains("Plugin.ConfigureOpAmpOptions() invoked.", standardOutput, StringComparison.Ordinal);
        Assert.Contains($"MaxPendingCustomMessages: {maxPendingCustomMessages}", standardOutput, StringComparison.Ordinal);
        Assert.Contains($"MaxPendingCustomMessageBytes: {maxPendingCustomMessageBytes}", standardOutput, StringComparison.Ordinal);
        Assert.Contains("Plugin.ConfigureOpAmpClient() invoked.", standardOutput, StringComparison.Ordinal);
        Assert.Contains("Plugin.BeforeOpAmpClientStopped() invoked.", standardOutput, StringComparison.Ordinal);

        var configureClientIndex = standardOutput.IndexOf("Plugin.ConfigureOpAmpClient() invoked.", StringComparison.Ordinal);
        var initializedIndex = standardOutput.IndexOf(PluginInitDonePattern, StringComparison.Ordinal);
        var beforeStopIndex = standardOutput.IndexOf("Plugin.BeforeOpAmpClientStopped() invoked.", StringComparison.Ordinal);

        Assert.True(configureClientIndex < initializedIndex);
        Assert.True(initializedIndex < beforeStopIndex);
    }

    private string RunOpAmpTestApplication(MockOpAmpServer server)
    {
        var timeout = TimeSpan.FromSeconds(10);
        using var process = StartTestApplication(new TestSettings { Arguments = "--wait-for-release", RedirectStandardInput = true });
        Assert.NotNull(process);
        using var output = new ProcessHelper(process);

        try
        {
            server.AssertExpectations(timeout);
        }
        finally
        {
            // Closing stdin releases the app after delivery is verified, including when an assertion fails.
            process.StandardInput.Close();

            if (!process.WaitForExit((int)timeout.TotalMilliseconds))
            {
                process.Kill();
                Assert.True(process.WaitForExit((int)timeout.TotalMilliseconds), "Test application did not exit after being killed.");
            }

            Output.WriteResult(output);
        }

        Assert.Equal(0, process.ExitCode);
        return output.StandardOutput;
    }
}
