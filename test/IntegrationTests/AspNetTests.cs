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

    private readonly Dictionary<string, string> _environmentVariables = new();

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
        _environmentVariables["OTEL_EXPORTER_OTLP_ENDPOINT"] = collectorUrl;
        var webPort = TcpPortProvider.GetOpenPort();
        await using var container = await StartContainerAsync(webPort, appPoolMode);
        await CallTestApplicationEndpoint(webPort);

        collector.AssertExpectations();
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    [Trait("Containers", "Windows")]
    public async Task TracesResource()
    {
        Assert.True(EnvironmentTools.IsWindowsAdministrator(), "This test requires Windows Administrator privileges.");

        _environmentVariables["OTEL_SERVICE_NAME"] = ServiceName;

        // Using "*" as host requires Administrator. This is needed to make the mock collector endpoint
        // accessible to the Windows docker container where the test application is executed by binding
        // the endpoint to all network interfaces. In order to do that it is necessary to open the port
        // on the firewall.
        using var collector = new MockSpansCollector(Output, host: "*");
        using var fwPort = FirewallHelper.OpenWinPort(collector.Port, Output);
        collector.ResourceExpector.Expect("service.name", ServiceName); // this is set via env var in Dockerfile and Wep.config, but env var has precedence
        collector.ResourceExpector.Expect("deployment.environment", "test"); // this is set via Wep.config

        var collectorUrl = $"http://{DockerNetworkHelper.IntegrationTestsGateway}:{collector.Port}";
        _environmentVariables["OTEL_TRACES_EXPORTER"] = "otlp";
        _environmentVariables["OTEL_EXPORTER_OTLP_ENDPOINT"] = collectorUrl;
        var webPort = TcpPortProvider.GetOpenPort();
        await using var container = await StartContainerAsync(webPort);
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
        _environmentVariables["OTEL_METRICS_EXPORTER"] = "otlp";
        _environmentVariables["OTEL_EXPORTER_OTLP_ENDPOINT"] = collectorUrl;
        _environmentVariables["OTEL_METRIC_EXPORT_INTERVAL"] = "1000";
        _environmentVariables["OTEL_DOTNET_AUTO_METRICS_INSTRUMENTATION_ENABLED"] = "false"; // Helps to reduce noise by enabling only AspNet metrics.
        _environmentVariables["OTEL_DOTNET_AUTO_METRICS_ASPNET_INSTRUMENTATION_ENABLED"] = "true"; // Helps to reduce noise by enabling only AspNet metrics.
        var webPort = TcpPortProvider.GetOpenPort();
        await using var container = await StartContainerAsync(webPort);
        await CallTestApplicationEndpoint(webPort);

        collector.AssertExpectations();
    }

    private async Task<IContainer> StartContainerAsync(int webPort, string? appPoolMode = null)
    {
        // get path to test application that the profiler will attach to
        var imageName = appPoolMode == "Classic" ? "testapplication-aspnet-netframework-classic" : "testapplication-aspnet-netframework";

        var networkName = await DockerNetworkHelper.SetupIntegrationTestsNetworkAsync();

        var logPath = EnvironmentHelper.IsRunningOnCI()
            ? Path.Combine(Environment.GetEnvironmentVariable("GITHUB_WORKSPACE"), "test-artifacts", "profiler-logs")
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"OpenTelemetry .NET AutoInstrumentation", "logs");
        Directory.CreateDirectory(logPath);
        Output.WriteLine("Collecting docker logs to: " + logPath);

        var builder = new ContainerBuilder()
            .WithImage(imageName)
            .WithCleanUp(cleanUp: true)
            .WithName($"{imageName}-{webPort}")
            .WithNetwork(networkName)
            .WithPortBinding(webPort, 80)
            .WithBindMount(logPath, "c:/inetpub/wwwroot/logs");

        foreach (var env in _environmentVariables)
        {
            builder = builder.WithEnvironment(env.Key, env.Value);
        }

        var container = builder.Build();
        try
        {
            var wasStarted = container.StartAsync().Wait(TimeSpan.FromMinutes(5));
            wasStarted.Should().BeTrue($"Container based on {imageName} has to be operational for the test.");
            Output.WriteLine($"Container was started successfully.");

            await HealthzHelper.TestAsync($"http://localhost:{webPort}/healthz", Output);
            Output.WriteLine($"IIS WebApp was started successfully.");
        }
        catch
        {
            await container.DisposeAsync();
            throw;
        }

        return container;
    }

    private async Task CallTestApplicationEndpoint(int webPort)
    {
        var client = new HttpClient();

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
