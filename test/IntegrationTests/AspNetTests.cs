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
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using FluentAssertions;
using IntegrationTests.Helpers;
using IntegrationTests.Helpers.Models;
using Xunit;
using Xunit.Abstractions;

namespace IntegrationTests;

public class AspNetTests : TestHelper
{
    public AspNetTests(ITestOutputHelper output)
        : base("AspNet", output)
    {
    }

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
        using var agent = await MockZipkinCollector.Start(Output, host: "*");
        using var fwPort = FirewallHelper.OpenWinPort(agent.Port, Output);
        var testSettings = new TestSettings
        {
            TracesSettings = new TracesSettings { Port = agent.Port }
        };
        var webPort = TcpPortProvider.GetOpenPort();
        await using var container = await StartContainerAsync(testSettings, webPort);

        var client = new HttpClient();

        var response = await client.GetAsync($"http://localhost:{webPort}");
        var content = await response.Content.ReadAsStringAsync();

        Output.WriteLine("Sample response:");
        Output.WriteLine(content);

        agent.SpanFilters.Add(x => x.Name != "healthz");

        var spans = await agent.WaitForSpansAsync(1);

        Assert.True(spans.Count >= 1, $"Expecting at least 1 span, only received {spans.Count}");
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
        using var collector = await MockMetricsCollector.Start(Output, host: "*");
        collector.Expect("OpenTelemetry.Instrumentation.AspNet");

        // Helps to reduce noise by enabling only AspNet metrics.
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_METRICS_ENABLED_INSTRUMENTATIONS", "AspNet");
        using var fwPort = FirewallHelper.OpenWinPort(collector.Port, Output);
        var testSettings = new TestSettings
        {
            MetricsSettings = new MetricsSettings { Port = collector.Port },
        };
        var webPort = TcpPortProvider.GetOpenPort();
        await using var container = await StartContainerAsync(testSettings, webPort);

        var client = new HttpClient();
        var response = await client.GetAsync($"http://localhost:{webPort}");
        var content = await response.Content.ReadAsStringAsync();
        Output.WriteLine("Sample response:");
        Output.WriteLine(content);

        collector.AssertExpectations();
    }

    private async Task<TestcontainersContainer> StartContainerAsync(TestSettings testSettings, int webPort)
    {
        // get path to test application that the profiler will attach to
        string testApplicationName = $"testapplication-{EnvironmentHelper.TestApplicationName.ToLowerInvariant()}";

        string networkName = DockerNetworkHelper.IntegrationTestsNetworkName;
        string networkId = await DockerNetworkHelper.SetupIntegrationTestsNetworkAsync();

        string logPath = EnvironmentHelper.IsRunningOnCI()
            ? Path.Combine(Environment.GetEnvironmentVariable("GITHUB_WORKSPACE"), "build_data", "profiler-logs")
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"OpenTelemetry .NET AutoInstrumentation", "logs");

        Directory.CreateDirectory(logPath);

        Output.WriteLine("Collecting docker logs to: " + logPath);

        var agentPort = testSettings.TracesSettings?.Port ?? testSettings.MetricsSettings?.Port;
        var builder = new TestcontainersBuilder<TestcontainersContainer>()
            .WithImage(testApplicationName)
            .WithCleanUp(cleanUp: true)
            .WithOutputConsumer(Consume.RedirectStdoutAndStderrToConsole())
            .WithName($"{testApplicationName}-{agentPort}-{webPort}")
            .WithNetwork(networkId, networkName)
            .WithPortBinding(webPort, 80)
            .WithBindMount(logPath, "c:/inetpub/wwwroot/logs")
            .WithBindMount(EnvironmentHelper.GetNukeBuildOutput(), "c:/opentelemetry");

        string agentBaseUrl = $"http://{DockerNetworkHelper.IntegrationTestsGateway}:{agentPort}";
        string agentHealthzUrl = $"{agentBaseUrl}/healthz";

        if (testSettings.TracesSettings != null)
        {
            string zipkinEndpoint = $"{agentBaseUrl}/api/v2/spans";
            Output.WriteLine($"Zipkin Endpoint: {zipkinEndpoint}");

            builder = builder.WithEnvironment("OTEL_EXPORTER_ZIPKIN_ENDPOINT", zipkinEndpoint);
        }

        if (testSettings.MetricsSettings != null)
        {
            Output.WriteLine($"Otlp Endpoint: {agentBaseUrl}");
            builder = builder.WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", agentBaseUrl);
            builder = builder.WithEnvironment("OTEL_METRIC_EXPORT_INTERVAL", "1000");
        }

        foreach (var env in EnvironmentHelper.CustomEnvironmentVariables)
        {
            builder = builder.WithEnvironment(env.Key, env.Value);
        }

        var container = builder.Build();
        var wasStarted = container.StartAsync().Wait(TimeSpan.FromMinutes(5));

        wasStarted.Should().BeTrue($"Container based on {testApplicationName} has to be operational for the test.");

        Output.WriteLine($"Container was started successfully.");

        PowershellHelper.RunCommand($"docker exec {container.Name} curl -v {agentHealthzUrl}", Output);

        var webAppHealthzUrl = $"http://localhost:{webPort}/healthz";
        var webAppHealthzResult = await HealthzHelper.TestHealtzAsync(webAppHealthzUrl, "IIS WebApp", Output);

        webAppHealthzResult.Should().BeTrue("IIS WebApp health check never returned OK.");

        Output.WriteLine($"IIS WebApp was started successfully.");

        return container;
    }
}
#endif
