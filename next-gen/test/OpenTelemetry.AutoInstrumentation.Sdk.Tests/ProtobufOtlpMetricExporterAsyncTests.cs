// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics.Metrics;

using OpenTelemetry.Resources;
using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Sdk.Tests;

// Note: Metric aggregation is not tested here because it is handled by the .NET runtime before emitting the metrics out-of-process.
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
        collector.AllowMeter("ToOtlpResourceMetricsTest");

        // Create meter and counter
        using var meter = new Meter(name: "ToOtlpResourceMetricsTest", version: "0.0.1", tags: meterTags);
        var counter = meter.CreateCounter<int>("counter");

        // Record measurement
        counter.Add(100);

        // Wait for measurement to be processed
        var collectedData = await collector.WaitForMeasurementAsync(TimeSpan.FromSeconds(5));

        // All assertions are now local to the test
        Assert.NotNull(collectedData);
        var resourceMetric = Assert.Single(collectedData.Request.ResourceMetrics);
        var otlpResource = resourceMetric.Resource;

        // Resource assertions
        Assert.Contains(otlpResource.Attributes, kvp => kvp.Key == "service.name" && kvp.Value.StringValue == "service-name");
        Assert.Contains(otlpResource.Attributes, kvp => kvp.Key == "service.namespace" && kvp.Value.StringValue == "ns1");

        // Scope assertions
        var instrumentationLibraryMetrics = Assert.Single(resourceMetric.ScopeMetrics);
        Assert.Equal(string.Empty, instrumentationLibraryMetrics.SchemaUrl);
        Assert.Equal("ToOtlpResourceMetricsTest", instrumentationLibraryMetrics.Scope.Name);
        Assert.Equal("0.0.1", instrumentationLibraryMetrics.Scope.Version);

        // Meter tags assertions
        Assert.Equal(2, instrumentationLibraryMetrics.Scope.Attributes.Count);
        Assert.Contains(instrumentationLibraryMetrics.Scope.Attributes, kvp => kvp.Key == "key1" && kvp.Value.StringValue == "value1");
        Assert.Contains(instrumentationLibraryMetrics.Scope.Attributes, kvp => kvp.Key == "key2" && kvp.Value.StringValue == "value2");

        // Metric assertions
        var metric = Assert.Single(instrumentationLibraryMetrics.Metrics);
        Assert.Equal("counter", metric.Name);
        Assert.Equal(100, metric.Sum.DataPoints[0].AsInt);
    }

    [Fact]
    public async Task ToOtlpResourceMetricsWithMultipleCountersTest()
    {
        var resource = new Resource(new Dictionary<string, object>
        {
            { "service.name", "multi-counter-service" },
        });

        using var collector = new OtlpMeasurementCollector(resource);
        collector.AllowMeter("MultiCounterTest");

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
        collector.AllowMeter("MixedTypesTest");

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

    [Fact]
    public async Task ToOtlpResourceMetricsWithUpDownCounterTest()
    {
        var resource = new Resource(new Dictionary<string, object>
        {
            { "service.name", "updown-counter-service" },
        });

        using var collector = new OtlpMeasurementCollector(resource);
        collector.AllowMeter("UpDownCounterTest");

        using var meter = new Meter(name: "UpDownCounterTest", version: "1.0.0");
        var upDownCounter = meter.CreateUpDownCounter<int>("requests_active");

        upDownCounter.Add(10);

        var collectedData = await collector.WaitForMeasurementsAsync(1, TimeSpan.FromSeconds(5));

        Assert.Single(collectedData);

        // Verify OTLP structure - UpDownCounter should use non-monotonic sum
        var firstMeasurement = collectedData.FirstOrDefault(d => d.InstrumentName == "requests_active");
        Assert.NotNull(firstMeasurement);

        Assert.Equal(10, firstMeasurement.GetIntValue());

        var resourceMetric = Assert.Single(firstMeasurement.Request.ResourceMetrics);
        var scopeMetric = Assert.Single(resourceMetric.ScopeMetrics);
        Assert.Equal("UpDownCounterTest", scopeMetric.Scope.Name);
        Assert.Equal("1.0.0", scopeMetric.Scope.Version);

        var metric = Assert.Single(scopeMetric.Metrics);
        Assert.Equal("requests_active", metric.Name);
    }

    [Fact]
    public async Task ToOtlpResourceMetricsWithHistogramTest()
    {
        var resource = new Resource(new Dictionary<string, object>
        {
            { "service.name", "histogram-service" },
        });

        using var collector = new OtlpMeasurementCollector(resource);
        collector.AllowMeter("HistogramTest");

        using var meter = new Meter(name: "HistogramTest", version: "1.0.0");
        var histogram = meter.CreateHistogram<double>("request_duration");

        // Record several measurements with different values
        histogram.Record(0.1, new("method", "GET"), new("status", "200"));
        histogram.Record(0.25, new("method", "POST"), new("status", "200"));
        histogram.Record(1.5, new("method", "GET"), new("status", "500"));

        // Wait for all measurements
        var collectedData = await collector.WaitForMeasurementsAsync(3, TimeSpan.FromSeconds(5));

        Assert.Equal(3, collectedData.Count);

        // Verify measurements
        var measurements = collectedData.OrderBy(d => d.Timestamp).ToList();
        var expectedValues = new[] { 0.1, 0.25, 1.5 };

        for (int i = 0; i < measurements.Count; i++)
        {
            Assert.Equal("request_duration", measurements[i].InstrumentName);
            Assert.Equal(expectedValues[i], measurements[i].GetDoubleValue(), precision: 6);
        }

        // Verify OTLP structure
        var firstMeasurement = measurements[0];
        Assert.NotNull(firstMeasurement.Request);
        var resourceMetric = Assert.Single(firstMeasurement.Request.ResourceMetrics);
        var scopeMetric = Assert.Single(resourceMetric.ScopeMetrics);
        Assert.Equal("HistogramTest", scopeMetric.Scope.Name);

        var metric = Assert.Single(scopeMetric.Metrics);
        Assert.Equal("request_duration", metric.Name);
    }

    [Fact]
    public async Task ToOtlpResourceMetricsWithLongUpDownCounterTest()
    {
        var resource = new Resource(new Dictionary<string, object>
        {
            { "service.name", "long-updown-counter-service" },
        });

        using var collector = new OtlpMeasurementCollector(resource);
        collector.AllowMeter("LongUpDownCounterTest");

        using var meter = new Meter(name: "LongUpDownCounterTest", version: "1.0.0");
        var upDownCounter = meter.CreateUpDownCounter<long>("memory_usage_bytes");

        upDownCounter.Add(1048576L);  // Allocate 1MB

        var collectedData = await collector.WaitForMeasurementsAsync(1, TimeSpan.FromSeconds(5));

        Assert.Single(collectedData);

        var firstMeasurement = collectedData.FirstOrDefault(d => d.InstrumentName == "memory_usage_bytes");
        Assert.NotNull(firstMeasurement);
        Assert.Equal(1048576L, firstMeasurement.GetLongValue());

        var resourceMetric = Assert.Single(firstMeasurement.Request.ResourceMetrics);
        var scopeMetric = Assert.Single(resourceMetric.ScopeMetrics);
        Assert.Equal("LongUpDownCounterTest", scopeMetric.Scope.Name);

        var metric = Assert.Single(scopeMetric.Metrics);
        Assert.Equal("memory_usage_bytes", metric.Name);
    }

    [Fact]
    public async Task ToOtlpResourceMetricsWithIntHistogramTest()
    {
        var resource = new Resource(new Dictionary<string, object>
        {
            { "service.name", "int-histogram-service" },
        });

        using var collector = new OtlpMeasurementCollector(resource);
        collector.AllowMeter("IntHistogramTest");

        using var meter = new Meter(name: "IntHistogramTest", version: "1.0.0");
        var histogram = meter.CreateHistogram<int>("response_size_bytes");

        // Record different response sizes
        histogram.Record(1024, new KeyValuePair<string, object?>("endpoint", "/api/users"));
        histogram.Record(2048, new KeyValuePair<string, object?>("endpoint", "/api/orders"));
        histogram.Record(512, new KeyValuePair<string, object?>("endpoint", "/api/health"));
        histogram.Record(4096, new KeyValuePair<string, object?>("endpoint", "/api/data"));

        // Wait for all measurements
        var collectedData = await collector.WaitForMeasurementsAsync(4, TimeSpan.FromSeconds(5));

        Assert.Equal(4, collectedData.Count);

        // Verify measurements
        var measurements = collectedData.OrderBy(d => d.Timestamp).ToList();
        var expectedValues = new[] { 1024, 2048, 512, 4096 };

        for (int i = 0; i < measurements.Count; i++)
        {
            Assert.Equal("response_size_bytes", measurements[i].InstrumentName);
            Assert.Equal(expectedValues[i], measurements[i].GetIntValue());

            // Verify tags are preserved
            Assert.Single(measurements[i].Tags);
            Assert.Equal("endpoint", measurements[i].Tags[0].Key);
        }

        // Verify OTLP structure
        var firstMeasurement = measurements[0];
        var resourceMetric = Assert.Single(firstMeasurement.Request.ResourceMetrics);
        var scopeMetric = Assert.Single(resourceMetric.ScopeMetrics);
        Assert.Equal("IntHistogramTest", scopeMetric.Scope.Name);

        var metric = Assert.Single(scopeMetric.Metrics);
        Assert.Equal("response_size_bytes", metric.Name);
    }

    [Fact]
    public async Task ToOtlpResourceMetricsWithFloatGaugeTest()
    {
        var resource = new Resource(new Dictionary<string, object>
        {
            { "service.name", "float-gauge-service" },
        });

        using var collector = new OtlpMeasurementCollector(resource);
        collector.AllowMeter("FloatGaugeTest");

        using var meter = new Meter(name: "FloatGaugeTest", version: "1.0.0");
        var gauge = meter.CreateGauge<float>("cpu_usage_percent");

        // Record CPU usage values
        gauge.Record(45.2f, new KeyValuePair<string, object?>("core", "0"));
        gauge.Record(52.8f, new KeyValuePair<string, object?>("core", "1"));
        gauge.Record(38.1f, new KeyValuePair<string, object?>("core", "2"));

        // Wait for all measurements
        var collectedData = await collector.WaitForMeasurementsAsync(3, TimeSpan.FromSeconds(5));

        Assert.Equal(3, collectedData.Count);

        // Verify measurements
        var measurements = collectedData.OrderBy(d => d.Timestamp).ToList();
        var expectedValues = new[] { 45.2f, 52.8f, 38.1f };

        for (int i = 0; i < measurements.Count; i++)
        {
            Assert.Equal("cpu_usage_percent", measurements[i].InstrumentName);
            Assert.Equal(expectedValues[i], measurements[i].GetFloatValue(), precision: 6);

            // Verify core tags
            Assert.Single(measurements[i].Tags);
            Assert.Equal("core", measurements[i].Tags[0].Key);
        }

        // Verify OTLP structure
        var firstMeasurement = measurements[0];
        var resourceMetric = Assert.Single(firstMeasurement.Request.ResourceMetrics);
        var scopeMetric = Assert.Single(resourceMetric.ScopeMetrics);
        Assert.Equal("FloatGaugeTest", scopeMetric.Scope.Name);

        var metric = Assert.Single(scopeMetric.Metrics);
        Assert.Equal("cpu_usage_percent", metric.Name);
    }

    [Fact]
    public async Task ToOtlpResourceMetricsWithMixedInstrumentTypesTest()
    {
        var resource = new Resource(new Dictionary<string, object>
        {
            { "service.name", "mixed-instruments-service" },
        });

        using var collector = new OtlpMeasurementCollector(resource);
        collector.AllowMeter("MixedInstrumentsTest");

        using var meter = new Meter(name: "MixedInstrumentsTest", version: "1.0.0");

        // Create different types of instruments
        var counter = meter.CreateCounter<long>("total_requests");
        var upDownCounter = meter.CreateUpDownCounter<int>("active_requests");
        var histogram = meter.CreateHistogram<double>("request_duration_ms");
        var gauge = meter.CreateGauge<float>("cpu_usage");

        // Record measurements
        counter.Add(100);
        upDownCounter.Add(5);
        histogram.Record(156.7);
        gauge.Record(45.2f);

        // Wait for all measurements
        var collectedData = await collector.WaitForMeasurementsAsync(4, TimeSpan.FromSeconds(5));

        Assert.Equal(4, collectedData.Count);

        // Group by instrument name
        var measurementsByInstrument = collectedData.GroupBy(d => d.InstrumentName).ToDictionary(g => g.Key, g => g.First());

        // Verify each instrument type
        Assert.True(measurementsByInstrument.ContainsKey("total_requests"));
        Assert.Equal(100L, measurementsByInstrument["total_requests"].GetLongValue());

        Assert.True(measurementsByInstrument.ContainsKey("active_requests"));
        Assert.Equal(5, measurementsByInstrument["active_requests"].GetIntValue());

        Assert.True(measurementsByInstrument.ContainsKey("request_duration_ms"));
        Assert.Equal(156.7, measurementsByInstrument["request_duration_ms"].GetDoubleValue(), precision: 6);

        Assert.True(measurementsByInstrument.ContainsKey("cpu_usage"));
        Assert.Equal(45.2f, measurementsByInstrument["cpu_usage"].GetFloatValue(), precision: 6);

        // Verify all measurements have proper OTLP structure
        foreach (var measurement in collectedData)
        {
            Assert.NotNull(measurement.Request);
            var resourceMetric = Assert.Single(measurement.Request.ResourceMetrics);
            var scopeMetric = Assert.Single(resourceMetric.ScopeMetrics);
            Assert.Equal("MixedInstrumentsTest", scopeMetric.Scope.Name);
        }
    }

    [Fact]
    public async Task ToOtlpResourceMetricsWithTagsAndAttributesTest()
    {
        var resource = new Resource(new Dictionary<string, object>
        {
            { "service.name", "tags-attributes-service" },
            { "service.version", "2.1.0" },
        });

        using var collector = new OtlpMeasurementCollector(resource);
        collector.AllowMeter("TagsTest");

        using var meter = new Meter(name: "TagsTest", version: "1.0.0");
        var counter = meter.CreateCounter<int>("http_requests");

        // Record measurements with different tags
        counter.Add(1, new("method", "GET"), new("status_code", "200"), new("endpoint", "/api/users"));
        counter.Add(1, new("method", "POST"), new("status_code", "201"), new("endpoint", "/api/users"));
        counter.Add(1, new("method", "GET"), new("status_code", "404"), new("endpoint", "/api/unknown"));

        // Wait for all measurements
        var collectedData = await collector.WaitForMeasurementsAsync(3, TimeSpan.FromSeconds(5));

        Assert.Equal(3, collectedData.Count);

        // Verify tags are preserved
        foreach (var measurement in collectedData)
        {
            Assert.Equal("http_requests", measurement.InstrumentName);
            Assert.Equal(1, measurement.GetIntValue());

            // Each measurement should have 3 tags
            Assert.Equal(3, measurement.Tags.Length);

            // Verify tag names
            var tagNames = measurement.Tags.Select(t => t.Key).ToHashSet();
            Assert.Contains("method", tagNames);
            Assert.Contains("status_code", tagNames);
            Assert.Contains("endpoint", tagNames);
        }

        // Verify resource attributes
        var firstMeasurement = collectedData[0];
        var resourceMetric = Assert.Single(firstMeasurement.Request.ResourceMetrics);
        var resourceAttributes = resourceMetric.Resource.Attributes;

        Assert.Contains(resourceAttributes, attr => attr.Key == "service.name" && attr.Value.StringValue == "tags-attributes-service");
        Assert.Contains(resourceAttributes, attr => attr.Key == "service.version" && attr.Value.StringValue == "2.1.0");
    }

    [Fact]
    public async Task ToOtlpResourceMetricsWithComplexTagsTest()
    {
        var resource = new Resource(new Dictionary<string, object>
        {
            { "service.name", "complex-tags-service" },
        });

        using var collector = new OtlpMeasurementCollector(resource);
        collector.AllowMeter("ComplexTagsTest");

        using var meter = new Meter(name: "ComplexTagsTest", version: "1.0.0");
        var histogram = meter.CreateHistogram<double>("database_query_duration");

        // Record measurements with various tag types
        histogram.Record(
            23.5,
            new("operation", "SELECT"),
            new("table", "users"),
            new("rows_affected", 150),
            new("is_cached", false),
            new("query_id", "abc123"));

        histogram.Record(
            45.2,
            new("operation", "INSERT"),
            new("table", "orders"),
            new("rows_affected", 1),
            new("is_cached", false),
            new("query_id", "def456"));

        // Wait for measurements
        var collectedData = await collector.WaitForMeasurementsAsync(2, TimeSpan.FromSeconds(5));

        Assert.Equal(2, collectedData.Count);

        // Verify complex tags are preserved
        foreach (var measurement in collectedData)
        {
            Assert.Equal("database_query_duration", measurement.InstrumentName);
            Assert.Equal(5, measurement.Tags.Length);

            var tagDict = measurement.Tags.ToDictionary(t => t.Key, t => t.Value);
            Assert.Contains("operation", tagDict.Keys);
            Assert.Contains("table", tagDict.Keys);
            Assert.Contains("rows_affected", tagDict.Keys);
            Assert.Contains("is_cached", tagDict.Keys);
            Assert.Contains("query_id", tagDict.Keys);
        }
    }
}
