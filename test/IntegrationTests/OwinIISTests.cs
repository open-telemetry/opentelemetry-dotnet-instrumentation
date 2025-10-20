// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NETFRAMEWORK
using System.Net.Http;
using IntegrationTests.Helpers;
using Xunit.Abstractions;

namespace IntegrationTests;

public class OwinIISTests
{
    public OwinIISTests(ITestOutputHelper output)
    {
        Output = output;
    }

    private ITestOutputHelper Output { get; }

    [Fact]
    [Trait("Category", "EndToEnd")]
    [Trait("Containers", "Windows")]
    public async Task SubmitsTraces()
    {
        Assert.True(EnvironmentTools.IsWindowsAdministrator(), "This test requires Windows Administrator privileges.");

        // Using "*" as host requires Administrator. This is needed to make the mock collector endpoint
        // accessible to the Windows docker container where the test application is executed by binding
        // the endpoint to all network interfaces. In order to do that it is necessary to open the port
        // on the firewall.
        using var collector = new MockSpansCollector(Output, host: "*");
        using var fwPort = FirewallHelper.OpenWinPort(collector.Port, Output);
        collector.Expect("OpenTelemetry.Instrumentation.AspNet");

        Dictionary<string, string> environmentVariables = new()
        {
            ["OTEL_EXPORTER_OTLP_ENDPOINT"] = $"http://{DockerNetworkHelper.IntegrationTestsGateway}:{collector.Port}"
        };
        var webPort = TcpPortProvider.GetOpenPort();
        await using var container = await IISContainerTestHelper.StartContainerAsync("testapplication-owin-iis-netframework", webPort, environmentVariables, Output);

        await CallWebEndpoint(webPort);

        collector.AssertExpectations();
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    [Trait("Containers", "Windows")]
    public async Task SubmitMetrics()
    {
        Assert.True(EnvironmentTools.IsWindowsAdministrator(), "This test requires Windows Administrator privileges.");

        // Using "*" as host requires Administrator. This is needed to make the mock collector endpoint
        // accessible to the Windows docker container where the test application is executed by binding
        // the endpoint to all network interfaces. In order to do that it is necessary to open the port
        // on the firewall.
        using var collector = new MockMetricsCollector(Output, host: "*");
        using var fwPort = FirewallHelper.OpenWinPort(collector.Port, Output);
        collector.Expect("OpenTelemetry.Instrumentation.AspNet");

        Dictionary<string, string> environmentVariables = new()
        {
            ["OTEL_METRICS_EXPORTER"] = "otlp",
            ["OTEL_EXPORTER_OTLP_ENDPOINT"] = $"http://{DockerNetworkHelper.IntegrationTestsGateway}:{collector.Port}",
            ["OTEL_METRIC_EXPORT_INTERVAL"] = "1000",
            ["OTEL_DOTNET_AUTO_METRICS_INSTRUMENTATION_ENABLED"] = "false", // Helps to reduce noise by enabling only AspNet metrics.
            ["OTEL_DOTNET_AUTO_METRICS_ASPNET_INSTRUMENTATION_ENABLED"] = "true" // Helps to reduce noise by enabling only AspNet metrics.
        };
        var webPort = TcpPortProvider.GetOpenPort();
        await using var container = await IISContainerTestHelper.StartContainerAsync("testapplication-owin-iis-netframework", webPort, environmentVariables, Output);

        await CallWebEndpoint(webPort);

        collector.AssertExpectations();
    }

    private async Task CallWebEndpoint(int webPort)
    {
        var client = new HttpClient();
        var response = await client.GetAsync($"http://localhost:{webPort}/test/");
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Output.WriteLine("Response:");
        Output.WriteLine(content);
    }
}

#endif
