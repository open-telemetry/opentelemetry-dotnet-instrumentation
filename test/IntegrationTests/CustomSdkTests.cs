// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET
using Google.Protobuf;
using IntegrationTests.Helpers;
using OpenTelemetry.Proto.Trace.V1;
using Xunit.Abstractions;

namespace IntegrationTests;

[Collection(RedisCollection.Name)]
public class CustomSdkTests : TestHelper
{
    private readonly RedisFixture _redis;

    public CustomSdkTests(ITestOutputHelper output, RedisFixture redis)
        : base("CustomSdk", output)
    {
        _redis = redis;
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    [Trait("Containers", "Linux")]
    public async Task SubmitsTraces()
    {
        using var testServer = TestHttpServer.CreateDefaultTestServer(Output);
        using var collector = await MockSpansCollector.InitializeAsync(Output);
        SetExporter(collector);

        // ensure spans are exported by custom sdk with custom resource
        collector.ResourceExpector.Expect("test_attr", "added_manually");
        collector.Expect("OpenTelemetry.Instrumentation.StackExchangeRedis", span => !IsTopLevel(span));

        collector.Expect("System.Net.Http", span => !IsTopLevel(span));
        collector.Expect("TestApplication.CustomSdk", span => IsTopLevel(span));

        collector.Expect("NServiceBus.Core", span => IsTopLevel(span));

        EnableBytecodeInstrumentation();
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_SETUP_SDK", "false");
        SetEnvironmentVariable("OTEL_EXPORTER_OTLP_PROTOCOL", "http/protobuf");

        RunTestApplication(new()
        {
            Arguments = $"--redis-port {_redis.Port} --test-server-port {testServer.Port}"
        });

        collector.AssertExpectations();
        collector.ResourceExpector.AssertExpectations();
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    [Trait("Containers", "Linux")]
    public async Task SubmitsMetrics()
    {
        using var testServer = TestHttpServer.CreateDefaultTestServer(Output);
        using var collector = await MockMetricsCollector.InitializeAsync(Output);
        SetExporter(collector);

        // ensure metrics are exported by custom sdk with custom resource
        collector.ResourceExpector.Expect("test_attr", "added_manually");

        collector.Expect("OpenTelemetry.Instrumentation.Http");
        collector.Expect("NServiceBus.Core.Pipeline.Incoming");
        collector.Expect("TestApplication.CustomSdk");

        EnableBytecodeInstrumentation();
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_SETUP_SDK", "false");
        SetEnvironmentVariable("OTEL_EXPORTER_OTLP_PROTOCOL", "http/protobuf");
        SetEnvironmentVariable("LONG_RUNNING", "true");
        SetEnvironmentVariable("OTEL_METRIC_EXPORT_INTERVAL", "100");

        var process = StartTestApplication(new()
        {
            Arguments = $"--redis-port {_redis.Port} --test-server-port {testServer.Port}"
        });

        try
        {
            collector.AssertExpectations();
            collector.ResourceExpector.AssertExpectations();
        }
        finally
        {
            process?.Kill();
        }
    }

    private static bool IsTopLevel(Span span)
    {
        return span.ParentSpanId == ByteString.Empty;
    }
}
#endif
