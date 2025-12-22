// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NETFRAMEWORK
using System.Net.Http;
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

    public enum Gac
    {
        /// <summary>
        /// Use image with OTEL assemblies registered in GAC
        /// </summary>
        UseGac,

        /// <summary>
        /// Use image with OTEL assemblies not registered in GAC
        /// </summary>
        UseLocal
    }

    public enum AppPoolMode
    {
        /// <summary>
        /// Use image with Classic AppPoolMode
        /// </summary>
        Classic,

        /// <summary>
        /// Use image with Integrated AppPoolMode
        /// </summary>
        Integrated
    }

    private ITestOutputHelper Output { get; }

    [Theory]
    [Trait("Category", "EndToEnd")]
    [Trait("Containers", "Windows")]
    [InlineData(AppPoolMode.Classic, Gac.UseGac)]
    [InlineData(AppPoolMode.Classic, Gac.UseLocal)]
    [InlineData(AppPoolMode.Integrated, Gac.UseGac)]
    [InlineData(AppPoolMode.Integrated, Gac.UseLocal)]
    public async Task SubmitsTraces(AppPoolMode appPoolMode, Gac useGac)
    {
        Assert.True(EnvironmentTools.IsWindowsAdministrator(), "This test requires Windows Administrator privileges.");

        // Using "*" as host requires Administrator. This is needed to make the mock collector endpoint
        // accessible to the Windows docker container where the test application is executed by binding
        // the endpoint to all network interfaces. In order to do that it is necessary to open the port
        // on the firewall.
        using var collector = await MockSpansCollector.InitializeAsync(Output, host: "*");
        using var fwPort = FirewallHelper.OpenWinPort(collector.Port, Output);
        collector.Expect("OpenTelemetry.Instrumentation.AspNet"); // Expect Mvc span
        collector.Expect("OpenTelemetry.Instrumentation.AspNet"); // Expect WebApi span

        var collectorUrl = $"http://{DockerNetworkHelper.IntegrationTestsGateway}:{collector.Port}";
        Dictionary<string, string> environmentVariables = new()
        {
            ["OTEL_EXPORTER_OTLP_ENDPOINT"] = collectorUrl
        };
        var webPort = TcpPortProvider.GetOpenPort();
        var imageName = GetTestImageName(appPoolMode, useGac);
        await using var container = await IISContainerTestHelper.StartContainerAsync(imageName, webPort, environmentVariables, Output);
        await CallTestApplicationEndpoint(webPort);

        collector.AssertExpectations();
    }

    [Theory]
    [Trait("Category", "EndToEnd")]
    [Trait("Containers", "Windows")]
    [InlineData(AppPoolMode.Classic, Gac.UseGac)]
    [InlineData(AppPoolMode.Classic, Gac.UseLocal)]
    [InlineData(AppPoolMode.Integrated, Gac.UseGac)]
    [InlineData(AppPoolMode.Integrated, Gac.UseLocal)]
    public async Task SubmitTracesCapturesHttpHeaders(AppPoolMode appPoolMode, Gac useGac)
    {
        Assert.True(EnvironmentTools.IsWindowsAdministrator(), "This test requires Windows Administrator privileges.");

        // Using "*" as host requires Administrator. This is needed to make the mock collector endpoint
        // accessible to the Windows docker container where the test application is executed by binding
        // the endpoint to all network interfaces. In order to do that it is necessary to open the port
        // on the firewall.
        using var collector = await MockSpansCollector.InitializeAsync(Output, host: "*");
        using var fwPort = FirewallHelper.OpenWinPort(collector.Port, Output);
        collector.Expect("OpenTelemetry.Instrumentation.AspNet", span => // Expect Mvc span
        {
            if (appPoolMode == AppPoolMode.Classic)
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

        collector.Expect("OpenTelemetry.Instrumentation.AspNet", span => // Expect WebApi span
        {
            if (appPoolMode == AppPoolMode.Classic)
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
        var imageName = GetTestImageName(appPoolMode, useGac);
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
        using var collector = await MockSpansCollector.InitializeAsync(Output, host: "*");
        using var fwPort = FirewallHelper.OpenWinPort(collector.Port, Output);
        collector.ResourceExpector.Expect("service.name", ServiceName); // this is set via env var in Dockerfile and Wep.config, but env var has precedence
        collector.ResourceExpector.Expect("deployment.environment.name", "test"); // this is set via Wep.config

        var collectorUrl = $"http://{DockerNetworkHelper.IntegrationTestsGateway}:{collector.Port}";

        Dictionary<string, string> environmentVariables = new()
        {
            ["OTEL_SERVICE_NAME"] = ServiceName,
            ["OTEL_TRACES_EXPORTER"] = "otlp",
            ["OTEL_EXPORTER_OTLP_ENDPOINT"] = collectorUrl
        };

        var webPort = TcpPortProvider.GetOpenPort();
        await using var container = await IISContainerTestHelper.StartContainerAsync("testapplication-aspnet-netframework-integrated", webPort, environmentVariables, Output);
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
        using var collector = await MockMetricsCollector.InitializeAsync(Output, host: "*");
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
        await using var container = await IISContainerTestHelper.StartContainerAsync("testapplication-aspnet-netframework-integrated", webPort, environmentVariables, Output);
        await CallTestApplicationEndpoint(webPort);

        collector.AssertExpectations();
    }

    private static string GetTestImageName(AppPoolMode appPoolMode, Gac useGac)
    {
        return (appPoolMode, useGac) switch
        {
            (AppPoolMode.Classic, Gac.UseGac) => "testapplication-aspnet-netframework-classic",
            (AppPoolMode.Classic, Gac.UseLocal) => "testapplication-aspnet-netframework-classic-nogac",
            (AppPoolMode.Integrated, Gac.UseGac) => "testapplication-aspnet-netframework-integrated",
            (AppPoolMode.Integrated, Gac.UseLocal) => "testapplication-aspnet-netframework-integrated-nogac",
            _ => throw new ArgumentOutOfRangeException()
        };
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
