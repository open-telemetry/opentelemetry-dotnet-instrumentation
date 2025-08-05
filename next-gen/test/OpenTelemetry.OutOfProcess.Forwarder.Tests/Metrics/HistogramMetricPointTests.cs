// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.Metrics;
using Xunit;

namespace OpenTelemetry.OutOfProcess.Forwarder.Tests.Metrics;

public class HistogramMetricPointTests
{
    [Fact]
    public void Constructor_SetsAllProperties()
    {
        // Arrange
        var startTime = DateTime.UtcNow.AddMinutes(-1);
        var endTime = DateTime.UtcNow;
        var features = HistogramMetricPointFeatures.MinAndMaxRecorded | HistogramMetricPointFeatures.BucketsRecorded;
        var min = 1.0;
        var max = 100.0;
        var sum = 550.0;
        var count = 10L;

        // Act
        var point = new HistogramMetricPoint(startTime, endTime, features, min, max, sum, count);

        // Assert
        Assert.Equal(startTime, point.StartTimeUtc);
        Assert.Equal(endTime, point.EndTimeUtc);
        Assert.Equal(features, point.Features);
        Assert.Equal(min, point.Min);
        Assert.Equal(max, point.Max);
        Assert.Equal(sum, point.Sum);
        Assert.Equal(count, point.Count);
    }

    [Fact]
    public void Constructor_WithNonUtcDateTime_ConvertsToUtc()
    {
        // Arrange
        var localTime = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Local);
        var unspecifiedTime = new DateTime(2023, 1, 1, 13, 0, 0, DateTimeKind.Unspecified);

        // Act
        var point = new HistogramMetricPoint(
            localTime,
            unspecifiedTime,
            HistogramMetricPointFeatures.None,
            0.0,
            0.0,
            0.0,
            0L);

        // Assert
        Assert.Equal(DateTimeKind.Utc, point.StartTimeUtc.Kind);
        Assert.Equal(DateTimeKind.Utc, point.EndTimeUtc.Kind);
        Assert.Equal(localTime.ToUniversalTime(), point.StartTimeUtc);
        Assert.Equal(unspecifiedTime.ToUniversalTime(), point.EndTimeUtc);
    }

    [Fact]
    public void Constructor_WithUtcDateTime_KeepsUtc()
    {
        // Arrange
        var utcStartTime = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var utcEndTime = new DateTime(2023, 1, 1, 13, 0, 0, DateTimeKind.Utc);

        // Act
        var point = new HistogramMetricPoint(
            utcStartTime,
            utcEndTime,
            HistogramMetricPointFeatures.None,
            0.0,
            0.0,
            0.0,
            0L);

        // Assert
        Assert.Equal(utcStartTime, point.StartTimeUtc);
        Assert.Equal(utcEndTime, point.EndTimeUtc);
        Assert.Equal(DateTimeKind.Utc, point.StartTimeUtc.Kind);
        Assert.Equal(DateTimeKind.Utc, point.EndTimeUtc.Kind);
    }

    [Theory]
    [InlineData(HistogramMetricPointFeatures.None)]
    [InlineData(HistogramMetricPointFeatures.MinAndMaxRecorded)]
    [InlineData(HistogramMetricPointFeatures.BucketsRecorded)]
    [InlineData(HistogramMetricPointFeatures.MinAndMaxRecorded | HistogramMetricPointFeatures.BucketsRecorded)]
    public void Constructor_WithDifferentFeatures_SetsCorrectly(HistogramMetricPointFeatures features)
    {
        // Arrange & Act
        var point = new HistogramMetricPoint(
            DateTime.UtcNow,
            DateTime.UtcNow,
            features,
            0.0,
            0.0,
            0.0,
            0L);

        // Assert
        Assert.Equal(features, point.Features);
    }

    [Theory]
    [InlineData(double.MinValue)]
    [InlineData(double.MaxValue)]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(1.0)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(double.NaN)]
    public void Constructor_WithSpecialDoubleValues_HandlesCorrectly(double value)
    {
        // Arrange & Act
        var point = new HistogramMetricPoint(
            DateTime.UtcNow,
            DateTime.UtcNow,
            HistogramMetricPointFeatures.MinAndMaxRecorded,
            value, // min
            value, // max
            value, // sum
            1L);

        // Assert
        if (double.IsNaN(value))
        {
            Assert.True(double.IsNaN(point.Min));
            Assert.True(double.IsNaN(point.Max));
            Assert.True(double.IsNaN(point.Sum));
        }
        else
        {
            Assert.Equal(value, point.Min);
            Assert.Equal(value, point.Max);
            Assert.Equal(value, point.Sum);
        }
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(1L)]
    [InlineData(long.MaxValue)]
    [InlineData(long.MinValue)]
    public void Constructor_WithDifferentCountValues_SetsCorrectly(long count)
    {
        // Arrange & Act
        var point = new HistogramMetricPoint(
            DateTime.UtcNow,
            DateTime.UtcNow,
            HistogramMetricPointFeatures.None,
            0.0,
            0.0,
            0.0,
            count);

        // Assert
        Assert.Equal(count, point.Count);
    }
}
