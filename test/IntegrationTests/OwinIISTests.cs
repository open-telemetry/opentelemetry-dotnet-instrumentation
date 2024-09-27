// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NETFRAMEWORK
using System.Net.Http;
using FluentAssertions;
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
        collector.Expect("OpenTelemetry.Instrumentation.AspNet.Telemetry");

        Dictionary<string, string> environmentVariables = new()
        {
            ["OTEL_EXPORTER_OTLP_ENDPOINT"] = $"http://{DockerNetworkHelper.IntegrationTestsGateway}:{collector.Port}"
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
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        Output.WriteLine("Response:");
        Output.WriteLine(content);
    }
}

#endif
