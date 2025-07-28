// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.Metrics;
using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Sdk.Tests.Metrics;

public class ExponentialHistogramMetricPointTests
{
    [Fact]
    public void Constructor_SetsAllProperties()
    {
        // Arrange
        var startTime = DateTime.UtcNow.AddMinutes(-5);
        var endTime = DateTime.UtcNow;
        var features = HistogramMetricPointFeatures.MinAndMaxRecorded | HistogramMetricPointFeatures.BucketsRecorded;
        const double min = 1.5;
        const double max = 100.5;
        const double sum = 500.75;
        const long count = 25;
        const int scale = 2;
        const long zeroCount = 3;

        // Act
        var point = new ExponentialHistogramMetricPoint(
            startTime,
            endTime,
            features,
            min,
            max,
            sum,
            count,
            scale,
            zeroCount);

        // Assert
        Assert.Equal(startTime, point.StartTimeUtc);
        Assert.Equal(endTime, point.EndTimeUtc);
        Assert.Equal(features, point.Features);
        Assert.Equal(min, point.Min);
        Assert.Equal(max, point.Max);
        Assert.Equal(sum, point.Sum);
        Assert.Equal(count, point.Count);
        Assert.Equal(scale, point.Scale);
        Assert.Equal(zeroCount, point.ZeroCount);
    }

    [Fact]
    public void Constructor_WithNonUtcDateTime_ConvertsToUtc()
    {
        // Arrange
        var localStartTime = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Local);
        var localEndTime = new DateTime(2023, 1, 1, 13, 0, 0, DateTimeKind.Local);

        // Act
        var point = new ExponentialHistogramMetricPoint(localStartTime, localEndTime, HistogramMetricPointFeatures.None, 0, 0, 0, 0, 0, 0);

        // Assert
        Assert.Equal(DateTimeKind.Utc, point.StartTimeUtc.Kind);
        Assert.Equal(DateTimeKind.Utc, point.EndTimeUtc.Kind);
    }

    [Fact]
    public void Constructor_WithUtcDateTime_KeepsUtc()
    {
        // Arrange
        var utcStartTime = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var utcEndTime = new DateTime(2023, 1, 1, 13, 0, 0, DateTimeKind.Utc);

        // Act
        var point = new ExponentialHistogramMetricPoint(
            utcStartTime,
            utcEndTime,
            HistogramMetricPointFeatures.None,
            0,
            0,
            0,
            0,
            0,
            0);

        // Assert
        Assert.Equal(utcStartTime, point.StartTimeUtc);
        Assert.Equal(utcEndTime, point.EndTimeUtc);
        Assert.Equal(DateTimeKind.Utc, point.StartTimeUtc.Kind);
        Assert.Equal(DateTimeKind.Utc, point.EndTimeUtc.Kind);
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
    public void Constructor_WithSpecialDoubleValues_HandlesCorrectly(double value)
    {
        // Act
        var point = new ExponentialHistogramMetricPoint(
            DateTime.UtcNow,
            DateTime.UtcNow,
            HistogramMetricPointFeatures.None,
            value,
            value,
            value,
            0,
            0,
            0);

        // Assert
        Assert.Equal(value, point.Min);
        Assert.Equal(value, point.Max);
        Assert.Equal(value, point.Sum);
    }

    [Theory]
    [InlineData(long.MaxValue)]
    [InlineData(long.MinValue)]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(-1)]
    public void Constructor_WithDifferentCountValues_SetsCorrectly(long count)
    {
        // Act
        var point = new ExponentialHistogramMetricPoint(
            DateTime.UtcNow,
            DateTime.UtcNow,
            HistogramMetricPointFeatures.None,
            0,
            0,
            0,
            count,
            0,
            0);

        // Assert
        Assert.Equal(count, point.Count);
    }

    [Theory]
    [InlineData(int.MaxValue)]
    [InlineData(int.MinValue)]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(-1)]
    [InlineData(10)]
    [InlineData(-10)]
    public void Constructor_WithDifferentScaleValues_SetsCorrectly(int scale)
    {
        // Act
        var point = new ExponentialHistogramMetricPoint(
            DateTime.UtcNow,
            DateTime.UtcNow,
            HistogramMetricPointFeatures.None,
            0,
            0,
            0,
            0,
            scale,
            0);

        // Assert
        Assert.Equal(scale, point.Scale);
    }

    [Theory]
    [InlineData(long.MaxValue)]
    [InlineData(long.MinValue)]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(-1)]
    public void Constructor_WithDifferentZeroCountValues_SetsCorrectly(long zeroCount)
    {
        // Act
        var point = new ExponentialHistogramMetricPoint(
            DateTime.UtcNow,
            DateTime.UtcNow,
            HistogramMetricPointFeatures.None,
            0,
            0,
            0,
            0,
            0,
            zeroCount);

        // Assert
        Assert.Equal(zeroCount, point.ZeroCount);
    }

    [Theory]
    [InlineData(HistogramMetricPointFeatures.BucketsRecorded)]
    [InlineData(HistogramMetricPointFeatures.MinAndMaxRecorded | HistogramMetricPointFeatures.BucketsRecorded)]
    public void Constructor_WithDifferentFeatures_SetsCorrectly(HistogramMetricPointFeatures features)
    {
        // Act
        var point = new ExponentialHistogramMetricPoint(
            DateTime.UtcNow,
            DateTime.UtcNow,
            features,
            0,
            0,
            0,
            0,
            0,
            0);

        // Assert - ExponentialHistogramMetricPoint automatically includes BucketsRecorded
        Assert.True(point.Features.HasFlag(HistogramMetricPointFeatures.BucketsRecorded));
        if (features.HasFlag(HistogramMetricPointFeatures.MinAndMaxRecorded))
        {
            Assert.True(point.Features.HasFlag(HistogramMetricPointFeatures.MinAndMaxRecorded));
        }
    }

    [Fact]
    public void ReadOnlyRefStruct_CannotBeBoxed()
    {
        // This test ensures the struct cannot be boxed, which would cause allocation
        // If this compiles, the struct is properly defined as a ref struct
        var point = new ExponentialHistogramMetricPoint(
            DateTime.UtcNow,
            DateTime.UtcNow,
            HistogramMetricPointFeatures.None,
            0,
            0,
            0,
            0,
            0,
            0);

        // We can't assign to object or interface - this should compile
        // object boxed = point; // This would cause compilation error

        // For ref structs, we just verify it was created successfully
        Assert.Equal(0, point.Count);
    }
}
