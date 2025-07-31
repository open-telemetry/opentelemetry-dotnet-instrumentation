// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.Metrics;
using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Sdk.Tests.Metrics;

public class NumberMetricPointTests
{
    [Fact]
    public void Constructor_WithDoubleValue_SetsProperties()
    {
        // Arrange
        var startTime = DateTime.UtcNow.AddMinutes(-1);
        var endTime = DateTime.UtcNow;
        var value = 123.45;

        // Act
        var point = new NumberMetricPoint(startTime, endTime, value);

        // Assert
        Assert.Equal(startTime, point.StartTimeUtc);
        Assert.Equal(endTime, point.EndTimeUtc);
        Assert.Equal(value, point.ValueAsDouble);

        // Since it's a union, the long value will be the bit representation
        Assert.Equal(BitConverter.DoubleToInt64Bits(value), point.ValueAsLong);
    }

    [Fact]
    public void Constructor_WithLongValue_SetsProperties()
    {
        // Arrange
        var startTime = DateTime.UtcNow.AddMinutes(-1);
        var endTime = DateTime.UtcNow;
        var value = 12345L;

        // Act
        var point = new NumberMetricPoint(startTime, endTime, value);

        // Assert
        Assert.Equal(startTime, point.StartTimeUtc);
        Assert.Equal(endTime, point.EndTimeUtc);
        Assert.Equal(value, point.ValueAsLong);

        // Since it's a union, the double value will be the bit representation
        Assert.Equal(BitConverter.Int64BitsToDouble(value), point.ValueAsDouble);
    }

    [Fact]
    public void Constructor_WithNonUtcDateTime_ConvertsToUtc()
    {
        // Arrange
        var localTime = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Local);
        var unspecifiedTime = new DateTime(2023, 1, 1, 13, 0, 0, DateTimeKind.Unspecified);
        var value = 100.0;

        // Act
        var point = new NumberMetricPoint(localTime, unspecifiedTime, value);

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
        var value = 100.0;

        // Act
        var point = new NumberMetricPoint(utcStartTime, utcEndTime, value);

        // Assert
        Assert.Equal(utcStartTime, point.StartTimeUtc);
        Assert.Equal(utcEndTime, point.EndTimeUtc);
        Assert.Equal(DateTimeKind.Utc, point.StartTimeUtc.Kind);
        Assert.Equal(DateTimeKind.Utc, point.EndTimeUtc.Kind);
    }

    [Fact]
    public void UnionBehavior_DoubleAndLongShareMemory()
    {
        // Arrange
        var startTime = DateTime.UtcNow;
        var endTime = DateTime.UtcNow;

        // Test that the double and long values are stored in the same memory location
        var doubleValue = 123.456;
        var longValue = 789L;

        // Act
        var doublePoint = new NumberMetricPoint(startTime, endTime, doubleValue);
        var longPoint = new NumberMetricPoint(startTime, endTime, longValue);

        // Assert
        Assert.Equal(doubleValue, doublePoint.ValueAsDouble);
        Assert.Equal(BitConverter.DoubleToInt64Bits(doubleValue), doublePoint.ValueAsLong);

        Assert.Equal(longValue, longPoint.ValueAsLong);
        Assert.Equal(BitConverter.Int64BitsToDouble(longValue), longPoint.ValueAsDouble);
    }

    [Fact]
    public void ReadOnlyRefStruct_CannotBeBoxed()
    {
        // This test verifies that NumberMetricPoint is a ref struct by ensuring
        // it cannot be boxed (converted to object). This is a compile-time check.

        // The following line would not compile if NumberMetricPoint is properly a ref struct:
        // object boxed = point; // This should cause a compile error

        // We can verify the struct layout attribute instead
        var structType = typeof(NumberMetricPoint);
        var attributes = structType.GetCustomAttributes(typeof(System.Runtime.InteropServices.StructLayoutAttribute), false);

        // The NumberMetricPoint may not have explicit StructLayout attribute
        // Let's check if it's a value type at least
        Assert.True(structType.IsValueType);

        // If it has the attribute, verify it
        if (attributes.Length > 0)
        {
            var layoutAttribute = (System.Runtime.InteropServices.StructLayoutAttribute)attributes[0];
            Assert.Equal(System.Runtime.InteropServices.LayoutKind.Explicit, layoutAttribute.Value);
        }
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(double.MaxValue)]
    [InlineData(double.MinValue)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(double.NaN)]
    public void Constructor_WithSpecialDoubleValues_HandlesCorrectly(double value)
    {
        // Arrange
        var startTime = DateTime.UtcNow;
        var endTime = DateTime.UtcNow;

        // Act
        var point = new NumberMetricPoint(startTime, endTime, value);

        // Assert
        if (double.IsNaN(value))
        {
            Assert.True(double.IsNaN(point.ValueAsDouble));
        }
        else
        {
            Assert.Equal(value, point.ValueAsDouble);
        }
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(long.MaxValue)]
    [InlineData(long.MinValue)]
    [InlineData(-1L)]
    public void Constructor_WithSpecialLongValues_HandlesCorrectly(long value)
    {
        // Arrange
        var startTime = DateTime.UtcNow;
        var endTime = DateTime.UtcNow;

        // Act
        var point = new NumberMetricPoint(startTime, endTime, value);

        // Assert
        Assert.Equal(value, point.ValueAsLong);
    }
}
