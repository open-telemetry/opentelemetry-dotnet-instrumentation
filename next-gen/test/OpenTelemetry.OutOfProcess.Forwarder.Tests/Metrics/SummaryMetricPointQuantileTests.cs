// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.Metrics;
using Xunit;

namespace OpenTelemetry.OutOfProcess.Forwarder.Tests.Metrics;

public class SummaryMetricPointQuantileTests
{
    [Fact]
    public void Constructor_SetsAllProperties()
    {
        // Arrange
        const double quantile = 0.95;
        const double value = 123.45;

        // Act
        var quantilePoint = new SummaryMetricPointQuantile(quantile, value);

        // Assert
        Assert.Equal(quantile, quantilePoint.Quantile);
        Assert.Equal(value, quantilePoint.Value);
    }

    [Theory]
    [InlineData(0.0, 100.0)]
    [InlineData(0.5, 200.0)]
    [InlineData(0.95, 300.0)]
    [InlineData(0.99, 400.0)]
    [InlineData(1.0, 500.0)]
    public void Constructor_WithValidQuantiles_SetsCorrectly(double quantile, double value)
    {
        // Act
        var quantilePoint = new SummaryMetricPointQuantile(quantile, value);

        // Assert
        Assert.Equal(quantile, quantilePoint.Quantile);
        Assert.Equal(value, quantilePoint.Value);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(double.MaxValue)]
    [InlineData(double.MinValue)]
    [InlineData(0.0)]
    [InlineData(1.0)]
    [InlineData(-1.0)]
    public void Constructor_WithSpecialQuantileValues_HandlesCorrectly(double quantile)
    {
        // Act
        var quantilePoint = new SummaryMetricPointQuantile(quantile, 100.0);

        // Assert
        Assert.Equal(quantile, quantilePoint.Quantile);
        Assert.Equal(100.0, quantilePoint.Value);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(double.MaxValue)]
    [InlineData(double.MinValue)]
    [InlineData(0.0)]
    [InlineData(1.0)]
    [InlineData(-1.0)]
    [InlineData(123.456)]
    [InlineData(-123.456)]
    public void Constructor_WithSpecialValueValues_HandlesCorrectly(double value)
    {
        // Act
        var quantilePoint = new SummaryMetricPointQuantile(0.95, value);

        // Assert
        Assert.Equal(0.95, quantilePoint.Quantile);
        Assert.Equal(value, quantilePoint.Value);
    }

    [Fact]
    public void Constructor_WithNegativeQuantile_SetsCorrectly()
    {
        // Arrange
        const double negativeQuantile = -0.5;
        const double value = 100.0;

        // Act
        var quantilePoint = new SummaryMetricPointQuantile(negativeQuantile, value);

        // Assert
        Assert.Equal(negativeQuantile, quantilePoint.Quantile);
        Assert.Equal(value, quantilePoint.Value);
    }

    [Fact]
    public void Constructor_WithQuantileGreaterThanOne_SetsCorrectly()
    {
        // Arrange
        const double quantileGreaterThanOne = 1.5;
        const double value = 100.0;

        // Act
        var quantilePoint = new SummaryMetricPointQuantile(quantileGreaterThanOne, value);

        // Assert
        Assert.Equal(quantileGreaterThanOne, quantilePoint.Quantile);
        Assert.Equal(value, quantilePoint.Value);
    }

    [Fact]
    public void ReadOnlyRecordStruct_IsReadOnly()
    {
        // Arrange
        var quantilePoint = new SummaryMetricPointQuantile(0.5, 150.0);

        // Assert - Properties should be readonly
        Assert.Equal(0.5, quantilePoint.Quantile);
        Assert.Equal(150.0, quantilePoint.Value);

        // The properties should be readonly, so we can't modify them
        // quantilePoint.Quantile = 0.6; // This would cause compilation error
        // quantilePoint.Value = 200.0; // This would cause compilation error
    }

    [Fact]
    public void RecordStruct_SupportsValueEquality()
    {
        // Arrange
        var quantile1 = new SummaryMetricPointQuantile(0.95, 100.0);
        var quantile2 = new SummaryMetricPointQuantile(0.95, 100.0);
        var quantile3 = new SummaryMetricPointQuantile(0.99, 100.0);

        // Assert
        Assert.Equal(quantile1, quantile2);
        Assert.NotEqual(quantile1, quantile3);
        Assert.True(quantile1 == quantile2);
        Assert.False(quantile1 == quantile3);
        Assert.False(quantile1 != quantile2);
        Assert.True(quantile1 != quantile3);
    }

    [Fact]
    public void RecordStruct_SupportsGetHashCode()
    {
        // Arrange
        var quantile1 = new SummaryMetricPointQuantile(0.95, 100.0);
        var quantile2 = new SummaryMetricPointQuantile(0.95, 100.0);
        var quantile3 = new SummaryMetricPointQuantile(0.99, 100.0);

        // Assert
        Assert.Equal(quantile1.GetHashCode(), quantile2.GetHashCode());
        Assert.NotEqual(quantile1.GetHashCode(), quantile3.GetHashCode());
    }

    [Fact]
    public void RecordStruct_SupportsToString()
    {
        // Arrange
        var quantilePoint = new SummaryMetricPointQuantile(0.95, 123.45);

        // Act
        var toString = quantilePoint.ToString();

        // Assert
        Assert.NotNull(toString);
        Assert.NotEmpty(toString);
        Assert.Contains("0.95", toString, StringComparison.Ordinal);
        Assert.Contains("123.45", toString, StringComparison.Ordinal);
    }
}
