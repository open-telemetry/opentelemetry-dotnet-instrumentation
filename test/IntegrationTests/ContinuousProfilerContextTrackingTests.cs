// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0
using System.Text.RegularExpressions;
using IntegrationTests.Helpers;
using OpenTelemetry.Proto.Collector.Profiles.V1Development;
using OpenTelemetry.Proto.Profiles.V1Development;

namespace IntegrationTests;

public class ContinuousProfilerContextTrackingTests : TestHelper
{
    public ContinuousProfilerContextTrackingTests(ITestOutputHelper output)
        : base("ContinuousProfiler.ContextTracking", output)
    {
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void TraceContextIsCorrectlyAssociatedWithThreadSamples()
    {
        EnableBytecodeInstrumentation();
        using var collector = new MockProfilesCollector(Output);
        SetExporter(collector);
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_PLUGINS", "TestApplication.ContinuousProfiler.ContextTracking.TestPlugin, TestApplication.ContinuousProfiler.ContextTracking, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_SOURCES", "TestApplication.ContinuousProfiler.ContextTracking");

        var (standardOutput, _, _) = RunTestApplication();
        var activityMatch = Regex.Match(standardOutput, @"ContextTracking activity: ([0-9a-f]{32})/([0-9a-f]{16})");
        Assert.True(activityMatch.Success, "The test application did not report its activity context.");

        var traceId = activityMatch.Groups[1].Value;
        var traceIdHigh = Convert.ToUInt64(traceId.Substring(0, 16), 16);
        var traceIdLow = Convert.ToUInt64(traceId.Substring(16), 16);
        var spanId = Convert.ToUInt64(activityMatch.Groups[2].Value, 16);
        collector.ExpectCollected(profiles => AssertAllProfiles(profiles, traceIdHigh, traceIdLow, spanId), $"{nameof(AssertAllProfiles)} failed");

        collector.AssertCollected();
    }

    private static bool AssertAllProfiles(
        ICollection<ExportProfilesServiceRequest> profilesServiceRequests,
        ulong expectedTraceIdHigh,
        ulong expectedTraceIdLow,
        ulong expectedSpanId)
    {
        var totalSamplesWithTraceContextCount = 0;
        var managedThreadsWithTraceContext = new HashSet<string>();

        foreach (var batch in profilesServiceRequests)
        {
            var profile = batch.ResourceProfiles.Single().ScopeProfiles.Single().Profiles.Single();
            var dictionary = batch.Dictionary;

            foreach (var sample in profile.Samples)
            {
                // Other activities, including exporter HTTP requests, can be sampled in the same batch.
                if (sample.LinkIndex == 0)
                {
                    continue;
                }

                var link = dictionary.LinkTable[sample.LinkIndex];
                var traceId = link.TraceId.ToByteArray();
                var spanId = link.SpanId.ToByteArray();
                // The test exporter writes each ID part with BitConverter.GetBytes.
                if (traceId.Length != 16 || spanId.Length != 8 ||
                    BitConverter.ToUInt64(traceId, 0) != expectedTraceIdHigh ||
                    BitConverter.ToUInt64(traceId, 8) != expectedTraceIdLow ||
                    BitConverter.ToUInt64(spanId, 0) != expectedSpanId)
                {
                    continue;
                }

                totalSamplesWithTraceContextCount++;
                var threadId = GetThreadName(dictionary, sample);
                managedThreadsWithTraceContext.Add(threadId!);
            }
        }
#if NET
        Assert.True(managedThreadsWithTraceContext.Count > 1, $"at least 2 distinct threads should have trace context associated. Found: {string.Join(", ", managedThreadsWithTraceContext)}; samples: {totalSamplesWithTraceContextCount}.");
        Assert.True(totalSamplesWithTraceContextCount >= 3, "there should be sample with trace context in most of the batches.");
#else
        // for net fx, thread pool threads do not have names, hence it is not possible to uniquely
        // identify distinct threads. We will restrict our test to ensure we have at least the main thread is reporting context
        Assert.True(managedThreadsWithTraceContext.Count > 0, "at least one thread should have trace context associated.");
        Assert.True(totalSamplesWithTraceContextCount > 0, "there should be at least one sample with trace context .");
#endif

        return true;
    }

    private static string GetThreadName(ProfilesDictionary dictionary, Sample sample)
    {
        foreach (var attrIndex in sample.AttributeIndices)
        {
            if (attrIndex < dictionary.AttributeTable.Count)
            {
                var attribute = dictionary.AttributeTable[attrIndex];
                var key = dictionary.StringTable[attribute.KeyStrindex];

                // Look for thread.name attribute
                if (key == "thread.name" && attribute.Value.HasStringValue)
                {
                    var name = attribute.Value.StringValue;
                    return string.IsNullOrWhiteSpace(name) ? "unknown" : name;
                }
            }
        }

        return "unknown";
    }
}
