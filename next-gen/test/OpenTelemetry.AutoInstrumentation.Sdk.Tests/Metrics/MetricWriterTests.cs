// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.Metrics;
using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Sdk.Tests.Metrics;

public class MetricWriterTests
{
    [Fact]
    public void Constructor_CreatesInstance()
    {
        // Act
        var writer = new TestMetricWriter();

        // Assert
        Assert.NotNull(writer);
    }

    [Fact]
    public void BeginInstrumentationScope_DefaultImplementation_DoesNotThrow()
    {
        // Arrange
        var writer = new TestMetricWriter();
        var scope = new InstrumentationScope("test") { Version = "1.0" };

        // Act & Assert - Should not throw
        var exception = Record.Exception(() => writer.BeginInstrumentationScope(scope));
        Assert.Null(exception);
    }

    [Fact]
    public void EndInstrumentationScope_DefaultImplementation_DoesNotThrow()
    {
        // Arrange
        var writer = new TestMetricWriter();

        // Act & Assert - Should not throw
        var exception = Record.Exception(() => writer.EndInstrumentationScope());
        Assert.Null(exception);
    }

    [Fact]
    public void BeginMetric_DefaultImplementation_DoesNotThrow()
    {
        // Arrange
        var writer = new TestMetricWriter();
        var metric = new Metric(MetricType.LongSum, "test-metric", AggregationTemporality.Cumulative);

        // Act & Assert - Should not throw
        var exception = Record.Exception(() => writer.BeginMetric(metric));
        Assert.Null(exception);
    }

    [Fact]
    public void EndMetric_DefaultImplementation_DoesNotThrow()
    {
        // Arrange
        var writer = new TestMetricWriter();

        // Act & Assert - Should not throw
        var exception = Record.Exception(() => writer.EndMetric());
        Assert.Null(exception);
    }

    [Fact]
    public void WriteNumberMetricPoint_DefaultImplementation_DoesNotThrow()
    {
        // Arrange
        var writer = new TestMetricWriter();
        var numberMetricPoint = new NumberMetricPoint(DateTime.UtcNow, DateTime.UtcNow, 123.45);

        // Act & Assert - Should not throw, ref structs work when not in lambdas
        writer.WriteNumberMetricPoint(numberMetricPoint, ReadOnlySpan<KeyValuePair<string, object?>>.Empty, ReadOnlySpan<Exemplar>.Empty);
        Assert.True(true); // If we get here, no exception was thrown
    }

    [Fact]
    public void WriteHistogramMetricPoint_DefaultImplementation_DoesNotThrow()
    {
        // Arrange
        var writer = new TestMetricWriter();
        var histogramMetricPoint = new HistogramMetricPoint(
            DateTime.UtcNow,
            DateTime.UtcNow,
            HistogramMetricPointFeatures.None,
            0,
            0,
            0,
            0);

        // Act & Assert - Should not throw, ref structs work when not in lambdas
        writer.WriteHistogramMetricPoint(
            histogramMetricPoint,
            ReadOnlySpan<HistogramMetricPointBucket>.Empty,
            ReadOnlySpan<KeyValuePair<string, object?>>.Empty,
            ReadOnlySpan<Exemplar>.Empty);
        Assert.True(true); // If we get here, no exception was thrown
    }

    [Fact]
    public void WriteExponentialHistogramMetricPoint_DefaultImplementation_DoesNotThrow()
    {
        // Arrange
        var writer = new TestMetricWriter();
        var exponentialHistogramMetricPoint = new ExponentialHistogramMetricPoint(
            DateTime.UtcNow,
            DateTime.UtcNow,
            HistogramMetricPointFeatures.None,
            0,
            0,
            0,
            0,
            0,
            0);

        // Act & Assert - Should not throw, ref structs work when not in lambdas
        writer.WriteExponentialHistogramMetricPoint(
            exponentialHistogramMetricPoint,
            default(ExponentialHistogramMetricPointBuckets),
            default(ExponentialHistogramMetricPointBuckets),
            ReadOnlySpan<KeyValuePair<string, object?>>.Empty,
            ReadOnlySpan<Exemplar>.Empty);
        Assert.True(true); // If we get here, no exception was thrown
    }

    [Fact]
    public void WriteSummaryMetricPoint_DefaultImplementation_DoesNotThrow()
    {
        // Arrange
        var writer = new TestMetricWriter();
        var summaryMetricPoint = new SummaryMetricPoint(DateTime.UtcNow, DateTime.UtcNow, 100.0, 5);

        // Act & Assert - Should not throw, ref structs work when not in lambdas
        writer.WriteSummaryMetricPoint(
            summaryMetricPoint,
            ReadOnlySpan<SummaryMetricPointQuantile>.Empty,
            ReadOnlySpan<KeyValuePair<string, object?>>.Empty);
        Assert.True(true); // If we get here, no exception was thrown
    }

    [Fact]
    public void VirtualMethods_CanBeOverridden()
    {
        // Arrange
        var writer = new TestMetricWriterWithOverrides();
        var scope = new InstrumentationScope("test") { Version = "1.0" };
        var metric = new Metric(MetricType.LongSum, "test-metric", AggregationTemporality.Cumulative);

        // Act
        writer.BeginInstrumentationScope(scope);
        writer.BeginMetric(metric);
        writer.EndMetric();
        writer.EndInstrumentationScope();

        // Assert
        Assert.True(writer.BeginInstrumentationScopeCalled);
        Assert.True(writer.BeginMetricCalled);
        Assert.True(writer.EndMetricCalled);
        Assert.True(writer.EndInstrumentationScopeCalled);
    }

    [Fact]
    public void AbstractClass_CanBeInherited()
    {
        // This test verifies that MetricWriter is abstract and can be inherited
        var writer = new TestMetricWriter();

        // Assert
        Assert.IsAssignableFrom<MetricWriter>(writer);
        Assert.NotNull(writer);
    }

    private sealed class TestMetricWriter : MetricWriter
    {
        // Uses default implementations
    }

    private sealed class TestMetricWriterWithOverrides : MetricWriter
    {
        public bool BeginInstrumentationScopeCalled { get; private set; }

        public bool EndInstrumentationScopeCalled { get; private set; }

        public bool BeginMetricCalled { get; private set; }

        public bool EndMetricCalled { get; private set; }

        public override void BeginInstrumentationScope(InstrumentationScope instrumentationScope)
        {
            BeginInstrumentationScopeCalled = true;
        }

        public override void EndInstrumentationScope()
        {
            EndInstrumentationScopeCalled = true;
        }

        public override void BeginMetric(Metric metric)
        {
            BeginMetricCalled = true;
        }

        public override void EndMetric()
        {
            EndMetricCalled = true;
        }
    }
}
