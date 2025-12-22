// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NETFRAMEWORK
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Google.Protobuf;
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
        using var collector = await MockSpansCollector.InitializeAsync(Output, host: "*");
        SetExporter(collector);
        using var fwPort = FirewallHelper.OpenWinPort(collector.Port, Output);

        var netTcpPort = TcpPortProvider.GetOpenPort();
        var httpPort = TcpPortProvider.GetOpenPort();

        collector.Expect("OpenTelemetry.Instrumentation.Wcf", span => span.Kind == Span.Types.SpanKind.Server && !IndicatesHealthCheckRequest(span), "Server 1");
        // filtering by name required, because client instrumentation creates some additional spans, e.g for connection registration
        collector.Expect("OpenTelemetry.Instrumentation.Wcf", span => span.Kind == Span.Types.SpanKind.Client && span.Name == "http://opentelemetry.io/StatusService/Ping", "Client 1");
        collector.Expect("OpenTelemetry.Instrumentation.Wcf", span => span.Kind == Span.Types.SpanKind.Server && !IndicatesHealthCheckRequest(span), "Server 2");
        collector.Expect("OpenTelemetry.Instrumentation.Wcf", span => span.Kind == Span.Types.SpanKind.Client && span.Name == "http://opentelemetry.io/StatusService/Ping", "Client 2");

        collector.Expect("OpenTelemetry.Instrumentation.AspNet", span => span.Kind == Span.Types.SpanKind.Server && span.ParentSpanId != ByteString.Empty, "AspNet Server 1");

        collector.Expect("TestApplication.Wcf.Client.NetFramework", span => span.Kind == Span.Types.SpanKind.Internal, "Custom parent");
        collector.Expect("TestApplication.Wcf.Client.NetFramework", span => span.Kind == Span.Types.SpanKind.Internal, "Custom sibling");

        collector.ExpectCollected(ValidateExpectedSpanHierarchy);

        var collectorUrl = $"http://{DockerNetworkHelper.IntegrationTestsGateway}:{collector.Port}";
        _environmentVariables["OTEL_EXPORTER_OTLP_ENDPOINT"] = collectorUrl;

        await using var container = await StartContainerAsync(netTcpPort, httpPort);

        RunTestApplication(new TestSettings
        {
            Arguments = $"{netTcpPort} {httpPort}"
        });

        collector.AssertExpectations();
    }

    private static bool IndicatesHealthCheckRequest(Span span)
    {
        return span.Attributes.Single(attr => attr.Key == "rpc.method").Value.StringValue == string.Empty;
    }

    private static bool ValidateExpectedSpanHierarchy(ICollection<MockSpansCollector.Collected> collectedSpans)
    {
        var wcfServerSpans = collectedSpans.Where(collected =>
            collected.Span.Kind == Span.Types.SpanKind.Server &&
            collected.InstrumentationScopeName == "OpenTelemetry.Instrumentation.Wcf");
        var aspNetServerSpan = collectedSpans.Single(collected =>
            collected.Span.Kind == Span.Types.SpanKind.Server &&
            collected.InstrumentationScopeName == "OpenTelemetry.Instrumentation.AspNet");
        var aspNetParentedWcfServerSpans = wcfServerSpans.Count(sp => sp.Span.ParentSpanId == aspNetServerSpan.Span.SpanId);
        return aspNetParentedWcfServerSpans == 1 && WcfClientInstrumentation.ValidateExpectedSpanHierarchy(collectedSpans);
    }

    private async Task<IContainer> StartContainerAsync(int netTcpPort, int httpPort)
    {
        const string imageName = "testapplication-wcf-server-iis-netframework";

        var networkName = await DockerNetworkHelper.SetupIntegrationTestsNetworkAsync();

        var logPath = EnvironmentHelper.IsRunningOnCI()
            ? Path.Combine(Environment.GetEnvironmentVariable("GITHUB_WORKSPACE"), "test-artifacts", "profiler-logs")
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"OpenTelemetry .NET AutoInstrumentation", "logs");

        Directory.CreateDirectory(logPath);
        Output.WriteLine("Collecting docker logs to: " + logPath);

        var builder = new ContainerBuilder()
            .WithImage(imageName)
            .WithCleanUp(cleanUp: true)
            .WithName(imageName)
            .WithNetwork(networkName)
            .WithPortBinding(netTcpPort, 808)
            .WithPortBinding(httpPort, 80)
            .WithWaitStrategy(Wait.ForWindowsContainer().UntilHttpRequestIsSucceeded(rq
                => rq.ForPort(80).ForPath("/StatusService.svc")))
            .WithBindMount(logPath, "c:/inetpub/wwwroot/logs");

        foreach (var env in _environmentVariables)
        {
            builder = builder.WithEnvironment(env.Key, env.Value);
        }

        var container = builder.Build();
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        try
        {
            await container.StartAsync(cts.Token);
            Output.WriteLine("Container was started successfully.");
        }
        catch
        {
            Output.WriteLine("Container failed to start in a required time frame.");
            await container.DisposeAsync();
            throw;
        }

        return container;
    }
}
#endif
