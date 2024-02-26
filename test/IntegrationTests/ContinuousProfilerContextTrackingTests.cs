// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET6_0_OR_GREATER

using System.Text.Json;
using FluentAssertions;
using IntegrationTests.Helpers;
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
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_PLUGINS", "TestApplication.ContinuousProfiler.ContextTracking.TestPlugin, TestApplication.ContinuousProfiler.ContextTracking, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_SOURCES", "TestApplication.ContinuousProfiler.ContextTracking");

        var (standardOutput, _, _) = RunTestApplication();

        var batchSeparator = $"{Environment.NewLine}{Environment.NewLine}";

        var totalSamplesWithTraceContextCount = 0;
        var managedThreadsWithTraceContext = new HashSet<string>();

        var exportedSampleBatches = standardOutput.TrimEnd().Split(batchSeparator);

        foreach (var sampleBatch in exportedSampleBatches)
        {
            var batch = JsonDocument.Parse(sampleBatch.TrimStart());

            var samplesWithTraceContext = batch
                .RootElement
                .EnumerateArray()
                .Select(
                    sample =>
                        ConvertToPropertyList(sample))
                .Where(
                    sampleProperties =>
                        HasTraceContextAssociated(sampleProperties))
                .ToList();
            samplesWithTraceContext.Count.Should().BeLessOrEqualTo(1, "at most one sample in a batch should have trace context associated.");

            totalSamplesWithTraceContextCount += samplesWithTraceContext.Count;
            if (samplesWithTraceContext.FirstOrDefault() is { } sampleWithTraceContext)
            {
                managedThreadsWithTraceContext.Add(GetPropertyValue("ThreadName", sampleWithTraceContext).GetString()!);
            }
        }

        managedThreadsWithTraceContext.Should().HaveCountGreaterThan(1, "at least 2 distinct threads should have trace context associated.");
        totalSamplesWithTraceContextCount.Should().BeGreaterOrEqualTo(3, "there should be sample with trace context in most of the batches.");
    }

    private static bool HasTraceContextAssociated(List<JsonProperty> sample)
    {
        const int defaultTraceContextValue = 0;

        return GetPropertyValue("SpanId", sample).GetInt64() != defaultTraceContextValue &&
               GetPropertyValue("TraceIdHigh", sample).GetInt64() != defaultTraceContextValue &&
               GetPropertyValue("TraceIdLow", sample).GetInt64() != defaultTraceContextValue &&
               !string.IsNullOrWhiteSpace(GetPropertyValue("ThreadName", sample).GetString());
    }

    private static JsonElement GetPropertyValue(string propertyName, List<JsonProperty> jsonProperties)
    {
        return jsonProperties
            .Single(
                property =>
                    property.Name == propertyName)
            .Value;
    }

    private static List<JsonProperty> ConvertToPropertyList(JsonElement threadSampleDocument)
    {
        return threadSampleDocument
            .EnumerateObject()
            .ToList();
    }
}
#endif
