// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0
using IntegrationTests.Helpers;
using OpenTelemetry.Proto.Collector.Profiles.V1Development;
using Xunit.Abstractions;

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

        collector.ExpectCollected(AssertAllProfiles, $"{nameof(AssertAllProfiles)} failed");

        RunTestApplication();

        collector.AssertCollected();
    }

    private bool AssertAllProfiles(ICollection<ExportProfilesServiceRequest> profilesServiceRequests)
    {
        var totalSamplesWithTraceContextCount = 0;
        var managedThreadsWithTraceContext = new HashSet<string>();

        foreach (var batch in profilesServiceRequests)
        {
            var profile = batch.ResourceProfiles.Single().ScopeProfiles.Single().Profiles.Single();

            var samplesInBatch = profile.Sample;

            var samplesWithTraceContext = samplesInBatch.Where(s => s.HasLinkIndex).ToList();
#if NET
            Assert.True(samplesWithTraceContext.Count <= 1, "at most one sample in a batch should have trace context associated.");
#endif
            totalSamplesWithTraceContextCount += samplesWithTraceContext.Count;
            if (samplesWithTraceContext.FirstOrDefault() is { } sampleWithTraceContext)
            {
                var threadId = GetThreadName(profile, sampleWithTraceContext);
                Output.WriteLine($"Found trace context on thread: {threadId}");
                managedThreadsWithTraceContext.Add(threadId!);
            }
        }

        Assert.True(managedThreadsWithTraceContext.Count > 1, "at least 2 distinct threads should have trace context associated.");
        Assert.True(totalSamplesWithTraceContextCount >= 3, "there should be sample with trace context in most of the batches.");
        return true;
    }

    private string GetThreadName(OpenTelemetry.Proto.Profiles.V1Development.Profile profile, OpenTelemetry.Proto.Profiles.V1Development.Sample sample)
    {
        foreach (var attrIndex in sample.AttributeIndices)
        {
            if (attrIndex < profile.AttributeTable.Count)
            {
                var attribute = profile.AttributeTable[(int)attrIndex];
                var key = attribute.Key;

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
