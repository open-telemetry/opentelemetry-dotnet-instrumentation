// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Globalization;
using Google.Protobuf;
using IntegrationTests.Helpers;

namespace IntegrationTests;

public class ProcessContextPropagationTests : TestHelper
{
    private const string SourceName = "TestApplication.ProcessContext";
    private const string TraceId = "11111111111111111111111111111111";
    private const string ParentSpanId = "2222222222222222";
    private const string ZeroSpanId = "0000000000000000";
    private const string ExplicitTraceId = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private const string ExplicitSpanId = "bbbbbbbbbbbbbbbb";

    public ProcessContextPropagationTests(ITestOutputHelper output)
        : base("Smoke", output)
    {
        EnableBytecodeInstrumentation();
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_SOURCES", SourceName);
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_PROCESS_INSTRUMENTATION_ENABLED", "true");
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_METRICS_ENABLED", "false");
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOGS_ENABLED", "false");
        SetEnvironmentVariable("OTEL_PROPAGATORS", "tracecontext,baggage");
        SetEnvironmentVariable("OTEL_TRACES_SAMPLER", "parentbased_always_on");
        SetEnvironmentVariable("TRACESTATE", "vendor=value");
        SetEnvironmentVariable("BAGGAGE", "origin=environment");
    }

    [Theory]
    [Trait("Category", "EndToEnd")]
    [InlineData(null, true)]
    [InlineData("parent", false)]
    [InlineData("link", true)]
    [InlineData("link", false)]
    [InlineData("off", true)]
    public void ExtractsContextBeforeSamplingAndPreservesExistingParents(string? mode, bool sampled)
    {
        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_PROCESS_INSTRUMENTATION_CONTEXT_EXTRACTION_MODE", mode);
        SetEnvironmentVariable("TRACEPARENT", $"00-{TraceId}-{ParentSpanId}-{(sampled ? "01" : "00")}");

        var (output, _, _) = RunTestApplication(new() { Arguments = "process-context-roots" });
        var activities = ReadRows(output, "PROCESS_CONTEXT|activity|").ToDictionary(row => row[0], StringComparer.Ordinal);
        var samples = ReadRows(output, "PROCESS_CONTEXT|sample|").ToDictionary(row => row[0], StringComparer.Ordinal);
        var parentMode = mode is null or "parent";

        foreach (var name in new[] { "root", "repeated", "delayed" })
        {
            var activity = activities[name];
            Assert.Equal(mode == "link" ? $"{TraceId}:{ParentSpanId}" : string.Empty, samples[name][3]);
            if (parentMode)
            {
                Assert.Equal(TraceId, samples[name][1]);
                Assert.Equal(ParentSpanId, samples[name][2]);
                Assert.Equal(TraceId, activity[1]);
                Assert.Equal(ParentSpanId, activity[3]);
                Assert.Equal(sampled ? "1" : "0", activity[4]);
                Assert.Equal("vendor=value", activity[5]);
                Assert.Empty(activity[6]);
                Assert.Equal("1", activity[7]);
            }
            else
            {
                Assert.NotEqual(TraceId, activity[1]);
                Assert.Equal(ZeroSpanId, activity[3]);
                Assert.Equal("1", activity[4]);
                Assert.Equal(mode == "link" ? $"{TraceId}:{ParentSpanId}" : string.Empty, activity[6]);
            }
        }

        var root = activities["root"];
        if (parentMode && !sampled)
        {
            Assert.Equal("child", Assert.Single(ReadRows(output, "PROCESS_CONTEXT|dropped|"))[0]);
            Assert.False(activities.ContainsKey("child"));
        }
        else
        {
            var child = activities["child"];
            Assert.Equal(root[1], child[1]);
            Assert.Equal(root[2], child[3]);
            Assert.Equal(root[4], child[4]);
            Assert.Empty(child[6]);
        }

        Assert.NotEqual(root[2], activities["repeated"][2]);
        if (!parentMode)
        {
            Assert.NotEqual(root[1], activities["repeated"][1]);
        }

        foreach (var name in new[] { "explicit-context", "explicit-string" })
        {
            Assert.Equal(ExplicitTraceId, activities[name][1]);
            Assert.Equal(ExplicitSpanId, activities[name][3]);
            Assert.Empty(activities[name][6]);
        }

        var existingLinks = $"{ExplicitTraceId}:{ExplicitSpanId}" + (mode == "link" ? $",{TraceId}:{ParentSpanId}" : string.Empty);
        Assert.Equal(existingLinks, activities["existing-links"][6]);
        Assert.Equal(existingLinks, samples["existing-links"][3]);

        Assert.Equal(mode == "off" ? string.Empty : "environment", Assert.Single(ReadRows(output, "PROCESS_CONTEXT|baggage|"))[1]);
        ExpectExportedActivities(collector, activities.Values);
        collector.AssertExpectations();
    }

    [Theory]
    [Trait("Category", "EndToEnd")]
    [InlineData(false, true)]
#if NET
    [InlineData(true, false)]
#endif
    public void DisabledProcessInstrumentationLeavesIncomingContextUnused(bool groupEnabled, bool profilerEnabled)
    {
        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_PROCESS_INSTRUMENTATION_ENABLED", groupEnabled ? "true" : "false");
        SetEnvironmentVariable("CORECLR_ENABLE_PROFILING", profilerEnabled ? "1" : "0");
        SetEnvironmentVariable("TRACEPARENT", $"00-{TraceId}-{ParentSpanId}-01");

        var (output, _, _) = RunTestApplication(new() { Arguments = "process-context-roots" });
        var activities = ReadRows(output, "PROCESS_CONTEXT|activity|");
        foreach (var activity in activities.Where(activity => activity[0] is "root" or "repeated" or "delayed"))
        {
            Assert.NotEqual(TraceId, activity[1]);
            Assert.Equal(ZeroSpanId, activity[3]);
            Assert.Empty(activity[6]);
        }

        Assert.Equal(string.Empty, Assert.Single(ReadRows(output, "PROCESS_CONTEXT|baggage|"))[1]);
        ExpectExportedActivities(collector, activities);
        collector.AssertExpectations();
    }

    [Theory]
    [Trait("Category", "EndToEnd")]
    [InlineData("parent", true)]
    [InlineData("link", true)]
    [InlineData("parent", false)]
    public void PropagatesChildProcessContextWithoutChangingReusedStartInfo(string mode, bool injectionEnabled)
    {
        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_PROCESS_INSTRUMENTATION_CONTEXT_EXTRACTION_MODE", mode);
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_PROCESS_INSTRUMENTATION_CONTEXT_INJECTION_ENABLED", injectionEnabled ? "true" : "false");
        SetEnvironmentVariable("TRACEPARENT", $"00-{TraceId}-{ParentSpanId}-01");

        var (output, _, _) = RunTestApplication(new() { Arguments = "process-context-launch" });
        var parents = ReadRows(output, "PROCESS_CONTEXT|activity|").ToDictionary(row => row[0], StringComparer.Ordinal);
        var exported = parents.Values.ToList();

        foreach (var label in new[] { "active", "reused", "fallback", "parallel-first", "parallel-second" })
        {
            var child = Assert.Single(ReadRows(output, $"PROCESS_CHILD|{label}|PROCESS_CONTEXT|activity|"));
            var baggage = Assert.Single(ReadRows(output, $"PROCESS_CHILD|{label}|PROCESS_CONTEXT|baggage|"));
            Assert.Equal(injectionEnabled ? "updated" : "explicit", baggage[1]);

            if (!injectionEnabled)
            {
                Assert.Equal(ExplicitTraceId, child[1]);
                Assert.Equal(ExplicitSpanId, child[3]);
            }
            else
            {
                var traceId = label == "fallback" ? TraceId : parents[$"launch-{label}"][1];
                var parentSpanId = label == "fallback" ? ParentSpanId : parents[$"launch-{label}"][2];
                if (mode == "parent")
                {
                    Assert.Equal(traceId, child[1]);
                    Assert.Equal(parentSpanId, child[3]);
                }
                else
                {
                    Assert.Equal(ZeroSpanId, child[3]);
                    Assert.Equal(label == "fallback" ? string.Empty : $"{traceId}:{parentSpanId}", child[6]);
                    Assert.NotEqual(traceId, child[1]);
                }
            }

            exported.Add(child);
        }

        Assert.Contains("PROCESS_CONTEXT|original|unchanged", output, StringComparison.Ordinal);
        Assert.Contains("PROCESS_CONTEXT|failed-launch|unchanged", output, StringComparison.Ordinal);
        ExpectExportedActivities(collector, exported);
        collector.AssertExpectations();
    }

    private static string[][] ReadRows(string output, string prefix)
    {
        return output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Where(line => line.StartsWith(prefix, StringComparison.Ordinal))
            .Select(line => line.Substring(prefix.Length).Split('|'))
            .ToArray();
    }

    private static void ExpectExportedActivities(MockSpansCollector collector, IEnumerable<string[]> activities)
    {
        var expected = activities.Where(activity => activity[4] == "1").ToArray();
        foreach (var activity in expected)
        {
            collector.Expect(SourceName, span =>
                span.Name == activity[0] && ToHex(span.TraceId) == activity[1] && ToHex(span.SpanId) == activity[2] &&
                (span.ParentSpanId.IsEmpty ? ZeroSpanId : ToHex(span.ParentSpanId)) == activity[3] &&
                string.Join(",", span.Links.Select(link => $"{ToHex(link.TraceId)}:{ToHex(link.SpanId)}")) == activity[6]);
        }

        collector.ExpectAllCollected(spans => spans.Count(span => span.Scope.Name == SourceName) == expected.Length);
    }

    private static string ToHex(ByteString value)
    {
        return string.Concat(value.Select(item => item.ToString("x2", CultureInfo.InvariantCulture)));
    }
}
