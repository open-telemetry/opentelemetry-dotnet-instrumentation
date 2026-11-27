// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET
using System.Globalization;
using Google.Protobuf;
using IntegrationTests.Helpers;

namespace IntegrationTests;

public class ProcessContextCustomSdkTests : TestHelper
{
    private const string SourceName = "TestApplication.ProcessContextCustomSdk";
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

    [Theory]
    [Trait("Category", "EndToEnd")]
    [InlineData("parent")]
    [InlineData("link")]
    public void UsesDefaultPropagatorsAndApplicationSamplerForOrdinaryActivities(string mode)
    {
        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);

        var path = Path.GetTempFileName();
        var configuration = $$"""
        file_format: "1.0"
        disabled: true
        tracer_provider:
          processors:
            - batch:
                exporter:
                  otlp_http:
                    endpoint: ${OTEL_EXPORTER_OTLP_TRACES_ENDPOINT}
        instrumentation/development:
          dotnet:
            traces:
              process:
                context_extraction:
                  mode: {{mode}}
        """;
        File.WriteAllText(path, configuration);
        SetEnvironmentVariable("OTEL_EXPERIMENTAL_FILE_BASED_CONFIGURATION_ENABLED", "true");
        SetEnvironmentVariable("OTEL_CONFIG_FILE", path);
        string output;
        try
        {
            output = RunTestApplication(new() { Arguments = "--process-context" }).StandardOutput;
        }
        finally
        {
            File.Delete(path);
        }

        const string prefix = "PROCESS_CONTEXT_CUSTOM_SDK|";
        var rows = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Where(line => line.StartsWith(prefix, StringComparison.Ordinal))
            .Select(line => line.Substring(prefix.Length).Split('|'))
            .ToDictionary(row => row[0], StringComparer.Ordinal);

        var root = rows["root"];
        var child = rows["child"];
        var samples = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Where(line => line.StartsWith("PROCESS_CONTEXT_CUSTOM_SDK_SAMPLE|", StringComparison.Ordinal))
            .Select(line => line.Substring("PROCESS_CONTEXT_CUSTOM_SDK_SAMPLE|".Length).Split('|'))
            .ToDictionary(row => row[0], StringComparer.Ordinal);
        if (mode == "parent")
        {
            Assert.Equal(TraceId, root[1]);
            Assert.Equal(ParentSpanId, root[3]);
            Assert.Equal(root[1], child[1]);
            Assert.Equal(root[2], child[3]);
            Assert.Equal(TraceId, samples["root"][1]);
            Assert.Equal(ParentSpanId, samples["root"][2]);
        }
        else
        {
            Assert.NotEqual(TraceId, root[1]);
            Assert.NotEqual(root[1], child[1]);
            Assert.Equal($"{TraceId}:{ParentSpanId}", root[8]);
            Assert.Equal($"{root[1]}:{root[2]}", child[8]);
            Assert.Equal(root[8], samples["root"][3]);
            Assert.Equal(child[8], samples["child"][3]);
        }

        Assert.Equal("environment", root[7]);
        Assert.Equal("updated", child[7]);
        foreach (var activity in new[] { root, child })
        {
            Assert.Equal(mode == "parent" ? "1" : "0", activity[4]);
            Assert.Equal("1", activity[5]);
            Assert.Equal(mode == "parent" ? "vendor=value" : string.Empty, activity[6]);
            collector.Expect(SourceName, span =>
                span.Name == activity[0] &&
                span.TraceId == ByteString.CopyFrom(Convert.FromHexString(activity[1])) &&
                span.SpanId == ByteString.CopyFrom(Convert.FromHexString(activity[2])) &&
                (span.ParentSpanId.IsEmpty ? "0000000000000000" : ToHex(span.ParentSpanId)) == activity[3] &&
                span.TraceState == activity[6] &&
                string.Join(",", span.Links.Select(link => $"{ToHex(link.TraceId)}:{ToHex(link.SpanId)}")) == activity[8]);
        }

        collector.ExpectAllCollected(spans => spans.Count(span => span.Scope.Name == SourceName) == 2);
        collector.AssertExpectations();
    }

    private static string ToHex(ByteString value)
    {
        return string.Concat(value.Select(item => item.ToString("x2", CultureInfo.InvariantCulture)));
    }
}
#endif
