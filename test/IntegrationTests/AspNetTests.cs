// <copyright file="AspNetTests.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

#if NETFRAMEWORK
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

        string collectorUrl = $"http://{DockerNetworkHelper.IntegrationTestsGateway}:{collector.Port}";
        _environmentVariables["OTEL_TRACES_EXPORTER"] = "otlp";
        _environmentVariables["OTEL_EXPORTER_OTLP_ENDPOINT"] = collectorUrl;
        var webPort = TcpPortProvider.GetOpenPort();
        await using var container = await StartContainerAsync(webPort);
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

        string collectorUrl = $"http://{DockerNetworkHelper.IntegrationTestsGateway}:{collector.Port}";
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

        string collectorUrl = $"http://{DockerNetworkHelper.IntegrationTestsGateway}:{collector.Port}";
        _environmentVariables["OTEL_METRICS_EXPORTER"] = "otlp";
        _environmentVariables["OTEL_EXPORTER_OTLP_ENDPOINT"] = collectorUrl;
        _environmentVariables["OTEL_METRIC_EXPORT_INTERVAL"] = "1000";
        _environmentVariables["OTEL_DOTNET_AUTO_METRICS_ENABLED_INSTRUMENTATIONS"] = "AspNet"; // Helps to reduce noise by enabling only AspNet metrics.
        var webPort = TcpPortProvider.GetOpenPort();
        await using var container = await StartContainerAsync(webPort);
        await CallTestApplicationEndpoint(webPort);

        collector.AssertExpectations();
    }

    private async Task<TestcontainersContainer> StartContainerAsync(int webPort)
    {
        // get path to test application that the profiler will attach to
        string imageName = $"testapplication-aspnet-netframework";

        string networkName = DockerNetworkHelper.IntegrationTestsNetworkName;
        string networkId = await DockerNetworkHelper.SetupIntegrationTestsNetworkAsync();

        string logPath = EnvironmentHelper.IsRunningOnCI()
            ? Path.Combine(Environment.GetEnvironmentVariable("GITHUB_WORKSPACE"), "build_data", "profiler-logs")
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"OpenTelemetry .NET AutoInstrumentation", "logs");
        Directory.CreateDirectory(logPath);
        Output.WriteLine("Collecting docker logs to: " + logPath);

        var builder = new TestcontainersBuilder<TestcontainersContainer>()
            .WithImage(imageName)
            .WithCleanUp(cleanUp: true)
            .WithOutputConsumer(Consume.RedirectStdoutAndStderrToConsole())
            .WithName($"{imageName}-{webPort}")
            .WithNetwork(networkId, networkName)
            .WithPortBinding(webPort, 80)
            .WithBindMount(logPath, "c:/inetpub/wwwroot/logs")
            .WithBindMount(EnvironmentHelper.GetNukeBuildOutput(), "c:/opentelemetry");

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
        Output.WriteLine("Response:");
        Output.WriteLine(content);
    }
}
#endif
