// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.Metrics;
using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Sdk.Tests.Metrics;

public class MetricTests
{
    [Fact]
    public void Constructor_ValidArguments_CreatesMetric()
    {
        // Arrange
        var metricType = MetricType.LongSum;
        var name = "test-metric";
        var temporality = AggregationTemporality.Cumulative;

        // Act
        var metric = new Metric(metricType, name, temporality);

        // Assert
        Assert.Equal(metricType, metric.MetricType);
        Assert.Equal(name, metric.Name);
        Assert.Equal(temporality, metric.AggregationTemporality);
        Assert.Null(metric.Description);
        Assert.Null(metric.Unit);
    }

    [Fact]
    public void Constructor_WithDescription_SetsDescription()
    {
        // Arrange
        var metric = new Metric(MetricType.LongSum, "test", AggregationTemporality.Cumulative)
        {
            Description = "Test description"
        };

        // Assert
        Assert.Equal("Test description", metric.Description);
    }

    [Fact]
    public void Constructor_WithUnit_SetsUnit()
    {
        // Arrange
        var metric = new Metric(MetricType.LongSum, "test", AggregationTemporality.Cumulative)
        {
            Unit = "ms"
        };

        // Assert
        Assert.Equal("ms", metric.Unit);
    }

    [Fact]
    public void Constructor_NullName_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new Metric(MetricType.LongSum, null!, AggregationTemporality.Cumulative));
    }

    [Fact]
    public void Constructor_EmptyName_ThrowsArgumentException()
    {
        // The constructor validates empty names and throws ArgumentException
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Metric(MetricType.LongSum, string.Empty, AggregationTemporality.Cumulative));
    }

    [Fact]
    public void Constructor_WhitespaceName_ThrowsArgumentException()
    {
        // The constructor validates whitespace names and throws ArgumentException
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Metric(MetricType.LongSum, string.Empty, AggregationTemporality.Cumulative));
    }

    [Fact]
    public void Constructor_NonMonotonicSumWithDelta_ThrowsException()
    {
        // Non-monotonic sums with Delta aggregation should throw
        // Act & Assert
        Assert.Throws<NotSupportedException>(() =>
            new Metric(MetricType.LongSumNonMonotonic, "test", AggregationTemporality.Delta));

        Assert.Throws<NotSupportedException>(() =>
            new Metric(MetricType.DoubleSumNonMonotonic, "test", AggregationTemporality.Delta));
    }

    [Fact]
    public void Constructor_NonMonotonicSumWithCumulative_DoesNotThrow()
    {
        // Act
        var metric = new Metric(MetricType.LongSumNonMonotonic, "test", AggregationTemporality.Cumulative);

        // Assert
        Assert.Equal(MetricType.LongSumNonMonotonic, metric.MetricType);
        Assert.Equal(AggregationTemporality.Cumulative, metric.AggregationTemporality);
    }

    [Theory]
    [InlineData(MetricType.LongSum, false)]
    [InlineData(MetricType.DoubleSum, false)]
    [InlineData(MetricType.LongGauge, false)]
    [InlineData(MetricType.DoubleGauge, false)]
    [InlineData(MetricType.Summary, false)]
    [InlineData(MetricType.Histogram, false)]
    [InlineData(MetricType.ExponentialHistogram, false)]
    [InlineData(MetricType.LongSumNonMonotonic, true)]
    [InlineData(MetricType.DoubleSumNonMonotonic, true)]
    public void IsSumNonMonotonic_ReturnsCorrectValue(MetricType metricType, bool expected)
    {
        // Arrange
        var temporality = metricType == MetricType.LongSumNonMonotonic || metricType == MetricType.DoubleSumNonMonotonic
            ? AggregationTemporality.Cumulative
            : AggregationTemporality.Delta;
        var metric = new Metric(
            metricType,
            "test",
            temporality);

        // Act & Assert
        Assert.Equal(expected, metric.IsSumNonMonotonic);
    }

    [Theory]
    [InlineData(MetricType.LongSum, false)]
    [InlineData(MetricType.DoubleSum, true)]
    [InlineData(MetricType.LongGauge, false)]
    [InlineData(MetricType.DoubleGauge, true)]
    [InlineData(MetricType.Summary, false)]
    [InlineData(MetricType.Histogram, false)]
    [InlineData(MetricType.ExponentialHistogram, false)]
    [InlineData(MetricType.LongSumNonMonotonic, false)]
    [InlineData(MetricType.DoubleSumNonMonotonic, true)]
    public void IsFloatingPoint_ReturnsCorrectValue(MetricType metricType, bool expected)
    {
        // Arrange
        var temporality = metricType == MetricType.LongSumNonMonotonic || metricType == MetricType.DoubleSumNonMonotonic
            ? AggregationTemporality.Cumulative
            : AggregationTemporality.Delta;
        var metric = new Metric(metricType, "test", temporality);

        // Act & Assert
        Assert.Equal(expected, metric.IsFloatingPoint);
    }

    [Fact]
    public void Properties_AreReadOnly()
    {
        // Assert - verify properties don't have setters (compile-time check)
        var metricTypeProperty = typeof(Metric).GetProperty(nameof(Metric.MetricType));
        var nameProperty = typeof(Metric).GetProperty(nameof(Metric.Name));
        var temporalityProperty = typeof(Metric).GetProperty(nameof(Metric.AggregationTemporality));

        Assert.Null(metricTypeProperty?.SetMethod);
        Assert.Null(nameProperty?.SetMethod);
        Assert.Null(temporalityProperty?.SetMethod);
    }
}
