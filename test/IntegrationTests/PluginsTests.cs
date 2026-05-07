// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using IntegrationTests.Helpers;
using Xunit.Abstractions;

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
    public void OpAmpInitialized()
    {
        using var server = new MockOpAmpServer(Output);

        SetEnvironmentVariable("OTEL_DOTNET_AUTO_PLUGINS", "TestApplication.Plugins.Plugin, TestApplication.Plugins, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_OPAMP_ENABLED", "true");
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_OPAMP_SERVER_URL", $"http://localhost:{server.Port}/v1/opamp");

        var (standardOutput, _, _) = RunTestApplication();

        Assert.Contains("Plugin.ConfigureOpAmpOptions() invoked.", standardOutput, StringComparison.Ordinal);
        Assert.Contains("Plugin.AfterOpAmpClientStarted() invoked.", standardOutput, StringComparison.Ordinal);
        Assert.Contains("Plugin.BeforeOpAmpClientStopped() invoked.", standardOutput, StringComparison.Ordinal);
    }
}
