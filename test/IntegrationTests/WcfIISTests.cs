// <copyright file="WcfIISTests.cs" company="OpenTelemetry Authors">
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
using OpenTelemetry.Proto.Trace.V1;
using Xunit.Abstractions;

namespace IntegrationTests;

public class WcfIISTests : TestHelper
{
    private readonly Dictionary<string, string> _environmentVariables = new();

    public WcfIISTests(ITestOutputHelper output)
        : base("Wcf.Client.NetFramework", output)
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
        using var collector = new MockSpansCollector(Output, host: "*");
        SetExporter(collector);
        using var fwPort = FirewallHelper.OpenWinPort(collector.Port, Output);

        collector.Expect("OpenTelemetry.Instrumentation.Wcf", span => span.Kind == Span.Types.SpanKind.Server, "Server1");
        collector.Expect("OpenTelemetry.Instrumentation.Wcf", span => span.Kind == Span.Types.SpanKind.Client, "Client1");
        collector.Expect("OpenTelemetry.Instrumentation.Wcf", span => span.Kind == Span.Types.SpanKind.Server, "Server2");
        collector.Expect("OpenTelemetry.Instrumentation.Wcf", span => span.Kind == Span.Types.SpanKind.Client, "Client2");

        var collectorUrl = $"http://{DockerNetworkHelper.IntegrationTestsGateway}:{collector.Port}";
        _environmentVariables["OTEL_EXPORTER_OTLP_ENDPOINT"] = collectorUrl;

        var netTcpPort = TcpPortProvider.GetOpenPort();
        var httpPort = TcpPortProvider.GetOpenPort();

        await using var container = await StartContainerAsync(netTcpPort, httpPort);

        RunTestApplication(new TestSettings
        {
            Arguments = $"{netTcpPort} {httpPort}"
        });

        collector.AssertExpectations();
    }

    private async Task<IContainer> StartContainerAsync(int netTcpPort, int httpPort)
    {
        const string imageName = "testapplication-wcf-server-iis-netframework";

        var networkId = await DockerNetworkHelper.SetupIntegrationTestsNetworkAsync();

        var logPath = EnvironmentHelper.IsRunningOnCI()
            ? Path.Combine(Environment.GetEnvironmentVariable("GITHUB_WORKSPACE"), "build_data", "profiler-logs")
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"OpenTelemetry .NET AutoInstrumentation", "logs");

        Directory.CreateDirectory(logPath);
        Output.WriteLine("Collecting docker logs to: " + logPath);

        var builder = new ContainerBuilder()
            .WithImage(imageName)
            .WithCleanUp(cleanUp: true)
            .WithOutputConsumer(Consume.RedirectStdoutAndStderrToConsole())
            .WithName($"{imageName}")
            .WithNetwork(networkId, DockerNetworkHelper.IntegrationTestsNetworkName)
            .WithPortBinding(netTcpPort, 808)
            .WithPortBinding(httpPort, 80)
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
            Output.WriteLine("Container was started successfully.");
        }
        catch
        {
            await container.DisposeAsync();
            throw;
        }

        return container;
    }
}
#endif
