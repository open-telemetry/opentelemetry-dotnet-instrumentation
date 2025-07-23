// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics.Metrics;

using OpenTelemetry.Resources;
using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Sdk.Tests;

public sealed class ProtobufOtlpMetricExporterAsyncTests
{
    [Fact]
    public async Task ToOtlpResourceMetricsTest()
    {
        var meterTags = new KeyValuePair<string, object?>[]
        {
            new("key1", "value1"),
            new("key2", "value2"),
        };

        var resource = new Resource(new Dictionary<string, object>
        {
            { "service.name", "service-name" },
            { "service.namespace", "ns1" },
        });

        // Create the measurement collector
        using var collector = new OtlpMeasurementCollector(resource);

        // Create meter and counter
        using var meter = new Meter(name: "ToOtlpResourceMetricsTest", version: "0.0.1", tags: meterTags);
        var counter = meter.CreateCounter<int>("counter");

        // Record measurement
        counter.Add(100);

        // Wait for measurement to be processed
        var collectedData = await collector.WaitForMeasurementAsync(TimeSpan.FromSeconds(5));

        // All assertions are now local to the test
        Assert.NotNull(collectedData);
        Assert.Single(collectedData.Request.ResourceMetrics);

        var resourceMetric = collectedData.Request.ResourceMetrics.First();
        var otlpResource = resourceMetric.Resource;

        // Resource assertions
        Assert.Contains(otlpResource.Attributes, kvp => kvp.Key == "service.name" && kvp.Value.StringValue == "service-name");
        Assert.Contains(otlpResource.Attributes, kvp => kvp.Key == "service.namespace" && kvp.Value.StringValue == "ns1");

        // Scope assertions
        Assert.Single(resourceMetric.ScopeMetrics);
        var instrumentationLibraryMetrics = resourceMetric.ScopeMetrics.First();
        Assert.Equal(string.Empty, instrumentationLibraryMetrics.SchemaUrl);
        Assert.Equal("ToOtlpResourceMetricsTest", instrumentationLibraryMetrics.Scope.Name);
        Assert.Equal("0.0.1", instrumentationLibraryMetrics.Scope.Version);

        // Meter tags assertions
        Assert.Equal(2, instrumentationLibraryMetrics.Scope.Attributes.Count);
        Assert.Contains(instrumentationLibraryMetrics.Scope.Attributes, kvp => kvp.Key == "key1" && kvp.Value.StringValue == "value1");
        Assert.Contains(instrumentationLibraryMetrics.Scope.Attributes, kvp => kvp.Key == "key2" && kvp.Value.StringValue == "value2");

        // Metric assertions
        Assert.Single(instrumentationLibraryMetrics.Metrics);
        var metric = instrumentationLibraryMetrics.Metrics.First();
        Assert.Equal("counter", metric.Name);
        Assert.Equal(100, metric.Sum.DataPoints.First().AsInt);
    }

    [Fact]
    public async Task ToOtlpResourceMetricsWithMultipleCountersTest()
    {
        var resource = new Resource(new Dictionary<string, object>
        {
            { "service.name", "multi-counter-service" },
        });

        using var collector = new OtlpMeasurementCollector(resource);

        using var meter = new Meter(name: "MultiCounterTest", version: "1.0.0");
        var counter1 = meter.CreateCounter<int>("requests");
        var counter2 = meter.CreateCounter<int>("errors");

        // Record multiple measurements
        counter1.Add(50);
        counter2.Add(5);

        // Wait for all measurements
        var collectedData = await collector.WaitForMeasurementsAsync(2, TimeSpan.FromSeconds(5));

        Assert.Equal(2, collectedData.Count);

        // Find specific measurements
        var requestsData = collectedData.FirstOrDefault(d => d.InstrumentName == "requests");
        var errorsData = collectedData.FirstOrDefault(d => d.InstrumentName == "errors");

        Assert.NotNull(requestsData);
        Assert.NotNull(errorsData);
        Assert.Equal(50, requestsData.GetIntValue());
        Assert.Equal(5, errorsData.GetIntValue());
    }

    [Fact]
    public async Task ToOtlpResourceMetricsWithDifferentTypesTest()
    {
        var resource = new Resource(new Dictionary<string, object>
        {
            { "service.name", "mixed-types-service" },
        });

        using var collector = new OtlpMeasurementCollector(resource);

        using var meter = new Meter(name: "MixedTypesTest", version: "1.0.0");
        var intCounter = meter.CreateCounter<int>("int_counter");
        var longCounter = meter.CreateCounter<long>("long_counter");
        var doubleGauge = meter.CreateGauge<double>("double_gauge");

        // Record measurements of different types
        intCounter.Add(42);
        longCounter.Add(1000000L);
        doubleGauge.Record(3.14159);

        // Wait for all measurements
        var collectedData = await collector.WaitForMeasurementsAsync(3, TimeSpan.FromSeconds(5));

        Assert.Equal(3, collectedData.Count);

        // Verify different measurement types
        var intData = collectedData.FirstOrDefault(d => d.InstrumentName == "int_counter");
        var longData = collectedData.FirstOrDefault(d => d.InstrumentName == "long_counter");
        var doubleData = collectedData.FirstOrDefault(d => d.InstrumentName == "double_gauge");

        Assert.NotNull(intData);
        Assert.NotNull(longData);
        Assert.NotNull(doubleData);

        Assert.Equal(42, intData.GetIntValue());
        Assert.Equal(1000000L, longData.GetLongValue());
        Assert.Equal(3.14159, doubleData.GetDoubleValue(), precision: 5);
    }
}
