// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET
using Google.Protobuf;
using IntegrationTests.Helpers;

namespace IntegrationTests;

public class ProcessContextCustomSdkTests : TestHelper
{
    private const string SourceName = "TestApplication.CustomSdk.ProcessContext";
    private const string TraceId = "11111111111111111111111111111111";
    private const string ParentSpanId = "2222222222222222";

    public ProcessContextCustomSdkTests(ITestOutputHelper output)
        : base("CustomSdk", output)
    {
        EnableBytecodeInstrumentation();
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_SETUP_SDK", "false");
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_INSTRUMENTATION_ENABLED", "false");
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_PROCESS_INSTRUMENTATION_ENABLED", "true");
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_METRICS_ENABLED", "false");
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOGS_ENABLED", "false");
        SetEnvironmentVariable("OTEL_PROPAGATORS", null);
        SetEnvironmentVariable("OTEL_EXPORTER_OTLP_PROTOCOL", "http/protobuf");
        SetEnvironmentVariable("TRACEPARENT", $"00-{TraceId}-{ParentSpanId}-01");
        SetEnvironmentVariable("TRACESTATE", "vendor=value");
        SetEnvironmentVariable("BAGGAGE", "origin=environment");
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void UsesDefaultPropagatorsBeforeApplicationSdkInitialization()
    {
        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);

        var (output, _, _) = RunTestApplication(new() { Arguments = "--process-context" });
        const string prefix = "PROCESS_CONTEXT_CUSTOM_SDK|";
        var rows = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Where(line => line.StartsWith(prefix, StringComparison.Ordinal))
            .Select(line => line.Substring(prefix.Length).Split('|'))
            .ToDictionary(row => row[0], StringComparer.Ordinal);

        var root = rows["root"];
        var child = rows["child"];
        Assert.Equal(TraceId, root[1]);
        Assert.Equal(ParentSpanId, root[3]);
        Assert.Equal("environment", root[7]);
        Assert.Equal(root[1], child[1]);
        Assert.Equal(root[2], child[3]);
        Assert.Equal("updated", child[7]);
        foreach (var activity in new[] { root, child })
        {
            Assert.Equal("1", activity[4]);
            Assert.Equal("1", activity[5]);
            Assert.Equal("vendor=value", activity[6]);
            collector.Expect(SourceName, span =>
                span.Name == activity[0] &&
                span.TraceId == ByteString.CopyFrom(Convert.FromHexString(activity[1])) &&
                span.SpanId == ByteString.CopyFrom(Convert.FromHexString(activity[2])) &&
                span.ParentSpanId == ByteString.CopyFrom(Convert.FromHexString(activity[3])) &&
                span.TraceState == activity[6]);
        }

        collector.ExpectAllCollected(spans => spans.Count(span => span.Scope.Name == SourceName) == 2);
        collector.AssertExpectations();
    }
}
#endif
