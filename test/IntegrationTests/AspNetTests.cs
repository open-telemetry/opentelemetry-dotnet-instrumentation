// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NETFRAMEWORK
using System.Net.Http;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using FluentAssertions;
using IntegrationTests.Helpers;
using Xunit.Abstractions;

namespace IntegrationTests;

public class AspNetTests
{
    private const string ServiceName = "TestApplication.AspNet.NetFramework";

    public AspNetTests(ITestOutputHelper output)
    {
        Output = output;
    }

    private ITestOutputHelper Output { get; }

    [Theory]
    [Trait("Category", "EndToEnd")]
    [Trait("Containers", "Windows")]
    [InlineData("Classic")]
    [InlineData("Integrated")]
    public async Task SubmitsTraces(string appPoolMode)
    {
        Assert.True(EnvironmentTools.IsWindowsAdministrator(), "This test requires Windows Administrator privileges.");

        // Using "*" as host requires Administrator. This is needed to make the mock collector endpoint
        // accessible to the Windows docker container where the test application is executed by binding
        // the endpoint to all network interfaces. In order to do that it is necessary to open the port
        // on the firewall.
        using var collector = new MockSpansCollector(Output, host: "*");
        using var fwPort = FirewallHelper.OpenWinPort(collector.Port, Output);
        collector.Expect("OpenTelemetry.Instrumentation.AspNet.Telemetry"); // Expect Mvc span
        collector.Expect("OpenTelemetry.Instrumentation.AspNet.Telemetry"); // Expect WebApi span

        var collectorUrl = $"http://{DockerNetworkHelper.IntegrationTestsGateway}:{collector.Port}";
        Dictionary<string, string> environmentVariables = new()
        {
            ["OTEL_EXPORTER_OTLP_ENDPOINT"] = collectorUrl
        };
        var webPort = TcpPortProvider.GetOpenPort();
        var imageName = GetTestImageName(appPoolMode);
        await using var container = await IISContainerTestHelper.StartContainerAsync(imageName, webPort, environmentVariables, Output);
        await CallTestApplicationEndpoint(webPort);

        collector.AssertExpectations();
    }

    [Theory]
    [Trait("Category", "EndToEnd")]
    [Trait("Containers", "Windows")]
    [InlineData("Classic")]
    [InlineData("Integrated")]
    public async Task SubmitTracesCapturesHttpHeaders(string appPoolMode)
    {
        Assert.True(EnvironmentTools.IsWindowsAdministrator(), "This test requires Windows Administrator privileges.");

        // Using "*" as host requires Administrator. This is needed to make the mock collector endpoint
        // accessible to the Windows docker container where the test application is executed by binding
        // the endpoint to all network interfaces. In order to do that it is necessary to open the port
        // on the firewall.
        using var collector = new MockSpansCollector(Output, host: "*");
        using var fwPort = FirewallHelper.OpenWinPort(collector.Port, Output);
        collector.Expect("OpenTelemetry.Instrumentation.AspNet.Telemetry", span => // Expect Mvc span
        {
            if (appPoolMode == "Classic")
            {
                return span.Attributes.Any(x => x.Key == "http.request.header.custom-request-test-header2" && x.Value.StringValue == "Test-Value2")
                       && span.Attributes.All(x => x.Key != "http.request.header.custom-request-test-header1")
                       && span.Attributes.All(x => x.Key != "http.request.header.custom-request-test-header3")
                       && span.Attributes.All(x => x.Key != "http.response.header.custom-response-test-header1")
                       && span.Attributes.All(x => x.Key != "http.response.header.custom-response-test-header3")
                       && span.Attributes.All(x => x.Key != "http.response.header.custom-response-test-header2");
            }

            return span.Attributes.Any(x => x.Key == "http.request.header.custom-request-test-header2" && x.Value.StringValue == "Test-Value2")
                   && span.Attributes.All(x => x.Key != "http.request.header.custom-request-test-header1")
                   && span.Attributes.All(x => x.Key != "http.request.header.custom-request-test-header3")
                   && span.Attributes.Any(x => x.Key == "http.response.header.custom-response-test-header1" && x.Value.StringValue == "Test-Value4")
                   && span.Attributes.Any(x => x.Key == "http.response.header.custom-response-test-header3" && x.Value.StringValue == "Test-Value6")
                   && span.Attributes.All(x => x.Key != "http.response.header.custom-response-test-header2");
        });

        collector.Expect("OpenTelemetry.Instrumentation.AspNet.Telemetry", span => // Expect WebApi span
        {
            if (appPoolMode == "Classic")
            {
                return span.Attributes.Any(x => x.Key == "http.request.header.custom-request-test-header2" && x.Value.StringValue == "Test-Value2")
                       && span.Attributes.All(x => x.Key != "http.request.header.custom-request-test-header1")
                       && span.Attributes.All(x => x.Key != "http.request.header.custom-request-test-header3")
                       && span.Attributes.All(x => x.Key != "http.response.header.custom-response-test-header1")
                       && span.Attributes.All(x => x.Key != "http.response.header.custom-response-test-header3")
                       && span.Attributes.All(x => x.Key != "http.response.header.custom-response-test-header2");
            }

            return span.Attributes.Any(x => x.Key == "http.request.header.custom-request-test-header2" && x.Value.StringValue == "Test-Value2")
                   && span.Attributes.All(x => x.Key != "http.request.header.custom-request-test-header1")
                   && span.Attributes.All(x => x.Key != "http.request.header.custom-request-test-header3")
                   && span.Attributes.Any(x => x.Key == "http.response.header.custom-response-test-header1" && x.Value.StringValue == "Test-Value1")
                   && span.Attributes.Any(x => x.Key == "http.response.header.custom-response-test-header3" && x.Value.StringValue == "Test-Value3")
                   && span.Attributes.All(x => x.Key != "http.response.header.custom-response-test-header2");
        });

        var collectorUrl = $"http://{DockerNetworkHelper.IntegrationTestsGateway}:{collector.Port}";
        Dictionary<string, string> environmentVariables = new()
        {
            ["OTEL_EXPORTER_OTLP_ENDPOINT"] = collectorUrl,
            ["OTEL_DOTNET_AUTO_TRACES_ASPNET_INSTRUMENTATION_CAPTURE_REQUEST_HEADERS"] = "Custom-Request-Test-Header2",
            ["OTEL_DOTNET_AUTO_TRACES_ASPNET_INSTRUMENTATION_CAPTURE_RESPONSE_HEADERS"] = "Custom-Response-Test-Header1,Custom-Response-Test-Header3"
        };
        var webPort = TcpPortProvider.GetOpenPort();
        var imageName = GetTestImageName(appPoolMode);
        await using var container = await IISContainerTestHelper.StartContainerAsync(imageName, webPort, environmentVariables, Output);
        await CallTestApplicationEndpoint(webPort);

        collector.AssertExpectations();
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    [Trait("Containers", "Windows")]
    public async Task TracesResource()
    {
        Assert.True(EnvironmentTools.IsWindowsAdministrator(), "This test requires Windows Administrator privileges.");

        // Using "*" as host requires Administrator. This is needed to make the mock collector endpoint
        // accessible to the Windows docker container where the test application is executed by binding
        // the endpoint to all network interfaces. In order to do that it is necessary to open the port
        // on the firewall.
        using var collector = new MockSpansCollector(Output, host: "*");
        using var fwPort = FirewallHelper.OpenWinPort(collector.Port, Output);
        collector.ResourceExpector.Expect("service.name", ServiceName); // this is set via env var in Dockerfile and Wep.config, but env var has precedence
        collector.ResourceExpector.Expect("deployment.environment", "test"); // this is set via Wep.config

        var collectorUrl = $"http://{DockerNetworkHelper.IntegrationTestsGateway}:{collector.Port}";

        Dictionary<string, string> environmentVariables = new()
        {
            ["OTEL_SERVICE_NAME"] = ServiceName,
            ["OTEL_TRACES_EXPORTER"] = "otlp",
            ["OTEL_EXPORTER_OTLP_ENDPOINT"] = collectorUrl
        };

        var webPort = TcpPortProvider.GetOpenPort();
        await using var container = await IISContainerTestHelper.StartContainerAsync("testapplication-aspnet-netframework", webPort, environmentVariables, Output);
        await CallTestApplicationEndpoint(webPort);

        collector.ResourceExpector.AssertExpectations();
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

        var collectorUrl = $"http://{DockerNetworkHelper.IntegrationTestsGateway}:{collector.Port}";
        Dictionary<string, string> environmentVariables = new()
        {
            ["OTEL_METRICS_EXPORTER"] = "otlp", ["OTEL_EXPORTER_OTLP_ENDPOINT"] = collectorUrl, ["OTEL_METRIC_EXPORT_INTERVAL"] = "1000",
            ["OTEL_DOTNET_AUTO_METRICS_INSTRUMENTATION_ENABLED"] = "false", // Helps to reduce noise by enabling only AspNet metrics.
            ["OTEL_DOTNET_AUTO_METRICS_ASPNET_INSTRUMENTATION_ENABLED"] = "true" // Helps to reduce noise by enabling only AspNet metrics.
        };
        var webPort = TcpPortProvider.GetOpenPort();
        await using var container = await IISContainerTestHelper.StartContainerAsync("testapplication-aspnet-netframework", webPort, environmentVariables, Output);
        await CallTestApplicationEndpoint(webPort);

        collector.AssertExpectations();
    }

    private static string GetTestImageName(string appPoolMode)
    {
        return appPoolMode == "Classic" ? "testapplication-aspnet-netframework-classic" : "testapplication-aspnet-netframework";
    }

    private async Task CallTestApplicationEndpoint(int webPort)
    {
        var client = new HttpClient();

        client.DefaultRequestHeaders.Add("Custom-Request-Test-Header1", "Test-Value1");
        client.DefaultRequestHeaders.Add("Custom-Request-Test-Header2", "Test-Value2");
        client.DefaultRequestHeaders.Add("Custom-Request-Test-Header3", "Test-Value3");

        var response = await client.GetAsync($"http://localhost:{webPort}");
        var content = await response.Content.ReadAsStringAsync();
        Output.WriteLine("MVC Response:");
        Output.WriteLine(content);

        response = await client.GetAsync($"http://localhost:{webPort}/api/values");
        content = await response.Content.ReadAsStringAsync();
        Output.WriteLine("WebApi Response:");
        Output.WriteLine(content);
    }
}
#endif
