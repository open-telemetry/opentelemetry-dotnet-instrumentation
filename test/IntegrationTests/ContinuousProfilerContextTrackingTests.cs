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

        var exportedSampleBatches = standardOutput.TrimEnd().Split(batchSeparator);

        foreach (var sampleBatch in exportedSampleBatches)
        {
            var batch = JsonDocument.Parse(sampleBatch.TrimStart());

            var samplesWithTraceContextCount = batch
                .RootElement
                .EnumerateArray()
                .Select(
                    sample =>
                        ConvertToPropertyList(sample))
                .Count(
                    sampleProperties =>
                        HasTraceContextAssociated(sampleProperties));
            samplesWithTraceContextCount.Should().BeLessOrEqualTo(1, "at most one sample in a batch should have trace context associated.");

            totalSamplesWithTraceContextCount += samplesWithTraceContextCount;
        }

        totalSamplesWithTraceContextCount.Should().BeGreaterOrEqualTo(3, "there should be sample with trace context in most of the batches.");
    }

    private static bool HasTraceContextAssociated(List<JsonProperty> sample)
    {
        const int defaultTraceContextValue = 0;
        const int defaultManagedThreadIdValue = -1;

        return GetPropertyValue("SpanId", sample).GetInt64() != defaultTraceContextValue &&
               GetPropertyValue("TraceIdHigh", sample).GetInt64() != defaultTraceContextValue &&
               GetPropertyValue("TraceIdLow", sample).GetInt64() != defaultTraceContextValue &&
               GetPropertyValue("ManagedId", sample).GetInt32() != defaultManagedThreadIdValue;
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