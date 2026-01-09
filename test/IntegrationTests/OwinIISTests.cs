// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NETFRAMEWORK
using System.Net.Http;
using Google.Protobuf;
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
        collector.Expect("OpenTelemetry.Instrumentation.AspNet", x => x.ParentSpanId != ByteString.Empty); // verify that parent span id is propagated

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
        client.DefaultRequestHeaders.Add("traceparent", "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01"); // send a traceparent header to verify that parent span id is propagated
        var response = await client.GetAsync(new Uri($"http://localhost:{webPort}/test/")).ConfigureAwait(false);
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        Output.WriteLine("Response:");
        Output.WriteLine(content);
    }
}

#endif
