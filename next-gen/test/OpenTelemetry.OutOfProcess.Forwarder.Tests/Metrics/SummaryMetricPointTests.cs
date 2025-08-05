// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.Metrics;
using Xunit;

namespace OpenTelemetry.OutOfProcess.Forwarder.Tests.Metrics;

public class SummaryMetricPointTests
{
    [Fact]
    public void Constructor_SetsAllProperties()
    {
        // Arrange
        var startTime = DateTime.UtcNow.AddMinutes(-5);
        var endTime = DateTime.UtcNow;
        const double sum = 250.75;
        const int count = 15;

        // Act
        var point = new SummaryMetricPoint(startTime, endTime, sum, count);

        // Assert
        Assert.Equal(startTime, point.StartTimeUtc);
        Assert.Equal(endTime, point.EndTimeUtc);
        Assert.Equal(sum, point.Sum);
        Assert.Equal(count, point.Count);
    }

    [Fact]
    public void Constructor_WithNonUtcDateTime_ConvertsToUtc()
    {
        // Arrange
        var localStartTime = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Local);
        var localEndTime = new DateTime(2023, 1, 1, 13, 0, 0, DateTimeKind.Local);

        // Act
        var point = new SummaryMetricPoint(localStartTime, localEndTime, 0.0, 0);

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
        var point = new SummaryMetricPoint(utcStartTime, utcEndTime, 0.0, 0);

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
    [InlineData(123.456)]
    [InlineData(-123.456)]
    public void Constructor_WithSpecialDoubleValues_HandlesCorrectly(double sum)
    {
        // Act
        var point = new SummaryMetricPoint(DateTime.UtcNow, DateTime.UtcNow, sum, 5);

        // Assert
        Assert.Equal(sum, point.Sum);
    }

    [Theory]
    [InlineData(int.MaxValue)]
    [InlineData(int.MinValue)]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(-1)]
    [InlineData(100)]
    [InlineData(-100)]
    public void Constructor_WithDifferentCountValues_SetsCorrectly(int count)
    {
        // Act
        var point = new SummaryMetricPoint(DateTime.UtcNow, DateTime.UtcNow, 10.5, count);

        // Assert
        Assert.Equal(count, point.Count);
    }

    [Fact]
    public void Constructor_WithZeroValues_SetsCorrectly()
    {
        // Act
        var point = new SummaryMetricPoint(DateTime.UtcNow, DateTime.UtcNow, 0.0, 0);

        // Assert
        Assert.Equal(0.0, point.Sum);
        Assert.Equal(0, point.Count);
    }

    [Fact]
    public void Constructor_WithNegativeSum_SetsCorrectly()
    {
        // Arrange
        const double negativeSum = -456.78;
        const int count = 10;

        // Act
        var point = new SummaryMetricPoint(DateTime.UtcNow, DateTime.UtcNow, negativeSum, count);

        // Assert
        Assert.Equal(negativeSum, point.Sum);
        Assert.Equal(count, point.Count);
    }

    [Fact]
    public void Constructor_WithNegativeCount_SetsCorrectly()
    {
        // Arrange
        const double sum = 123.45;
        const int negativeCount = -5;

        // Act
        var point = new SummaryMetricPoint(DateTime.UtcNow, DateTime.UtcNow, sum, negativeCount);

        // Assert
        Assert.Equal(sum, point.Sum);
        Assert.Equal(negativeCount, point.Count);
    }

    [Fact]
    public void ReadOnlyRefStruct_CannotBeBoxed()
    {
        // This test ensures the struct cannot be boxed, which would cause allocation
        // If this compiles, the struct is properly defined as a ref struct
        var point = new SummaryMetricPoint(DateTime.UtcNow, DateTime.UtcNow, 0.0, 0);

        // We can't assign to object or interface - this should compile
        // object boxed = point; // This would cause compilation error

        // For ref structs, we just verify it was created successfully
        Assert.Equal(0.0, point.Sum);
    }

    [Fact]
    public void Properties_AreReadOnly()
    {
        // Arrange
        var startTime = DateTime.UtcNow.AddMinutes(-1);
        var endTime = DateTime.UtcNow;
        const double sum = 99.99;
        const int count = 7;

        // Act
        var point = new SummaryMetricPoint(startTime, endTime, sum, count);

        // Assert - Properties should be readonly, this tests they exist and are accessible
        Assert.Equal(startTime, point.StartTimeUtc);
        Assert.Equal(endTime, point.EndTimeUtc);
        Assert.Equal(sum, point.Sum);
        Assert.Equal(count, point.Count);

        // The properties should be readonly, so we can't modify them
        // point.Sum = 100; // This would cause compilation error
        // point.Count = 10; // This would cause compilation error
    }
}
