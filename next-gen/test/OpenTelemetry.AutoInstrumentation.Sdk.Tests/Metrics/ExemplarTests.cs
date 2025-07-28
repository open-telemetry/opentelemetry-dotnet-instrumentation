// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.Metrics;
using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Sdk.Tests.Metrics;

public class ExemplarTests
{
    [Fact]
    public void Constructor_WithLongValue_SetsProperties()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        var traceId = ActivityTraceId.CreateRandom();
        var spanId = ActivitySpanId.CreateRandom();
        var value = 123L;
        var attributes = new KeyValuePair<string, object?>[]
        {
            new("key1", "value1"),
            new("key2", 42)
        };

        // Act
        var exemplar = new Exemplar(timestamp, traceId, spanId, value, attributes);

        // Assert
        Assert.Equal(timestamp, exemplar.TimestampUtc);
        Assert.Equal(traceId, exemplar.TraceId);
        Assert.Equal(spanId, exemplar.SpanId);
        Assert.Equal(value, exemplar.ValueAsLong);

        // Since it's a union, the double value will be the bit representation
        Assert.Equal(BitConverter.Int64BitsToDouble(value), exemplar.ValueAsDouble);
    }

    [Fact]
    public void Constructor_WithDoubleValue_SetsProperties()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        var traceId = ActivityTraceId.CreateRandom();
        var spanId = ActivitySpanId.CreateRandom();
        var value = 123.45;
        var attributes = new KeyValuePair<string, object?>[]
        {
            new("key1", "value1"),
            new("key2", 42)
        };

        // Act
        var exemplar = new Exemplar(timestamp, traceId, spanId, value, attributes);

        // Assert
        Assert.Equal(timestamp, exemplar.TimestampUtc);
        Assert.Equal(traceId, exemplar.TraceId);
        Assert.Equal(spanId, exemplar.SpanId);
        Assert.Equal(value, exemplar.ValueAsDouble);

        // Since it's a union, the long value will be the bit representation
        Assert.Equal(BitConverter.DoubleToInt64Bits(value), exemplar.ValueAsLong);
    }

    [Fact]
    public void Constructor_WithEmptyAttributes_CreatesExemplar()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        var traceId = ActivityTraceId.CreateRandom();
        var spanId = ActivitySpanId.CreateRandom();
        var value = 100L;

        // Act
        var exemplar = new Exemplar(timestamp, traceId, spanId, value);

        // Assert
        Assert.Equal(timestamp, exemplar.TimestampUtc);
        Assert.Equal(traceId, exemplar.TraceId);
        Assert.Equal(spanId, exemplar.SpanId);
        Assert.Equal(value, exemplar.ValueAsLong);
    }

    [Fact]
    public void GetFilteredAttributesReference_ReturnsCorrectReference()
    {
        // Arrange
        var attributes = new KeyValuePair<string, object?>[]
        {
            new("key1", "value1"),
            new("key2", 42)
        };
        var exemplar = new Exemplar(
            DateTime.UtcNow,
            ActivityTraceId.CreateRandom(),
            ActivitySpanId.CreateRandom(),
            100L,
            attributes);

        // Act
        ref readonly var filteredAttributes = ref Exemplar.GetFilteredAttributesReference(in exemplar);

        // Assert
        Assert.Equal(2, filteredAttributes.Count);
        Assert.Contains(new KeyValuePair<string, object?>("key1", "value1"), filteredAttributes);
        Assert.Contains(new KeyValuePair<string, object?>("key2", 42), filteredAttributes);
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(long.MaxValue)]
    [InlineData(long.MinValue)]
    [InlineData(-1L)]
    public void Constructor_WithSpecialLongValues_HandlesCorrectly(long value)
    {
        // Arrange & Act
        var exemplar = new Exemplar(
            DateTime.UtcNow,
            ActivityTraceId.CreateRandom(),
            ActivitySpanId.CreateRandom(),
            value);

        // Assert
        Assert.Equal(value, exemplar.ValueAsLong);
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
        // Arrange & Act
        var exemplar = new Exemplar(
            DateTime.UtcNow,
            ActivityTraceId.CreateRandom(),
            ActivitySpanId.CreateRandom(),
            value);

        // Assert
        if (double.IsNaN(value))
        {
            Assert.True(double.IsNaN(exemplar.ValueAsDouble));
        }
        else
        {
            Assert.Equal(value, exemplar.ValueAsDouble);
        }
    }

    [Fact]
    public void UnionBehavior_DoubleAndLongShareMemory()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        var traceId = ActivityTraceId.CreateRandom();
        var spanId = ActivitySpanId.CreateRandom();

        // Test that the double and long values are stored in the same memory location
        var doubleValue = 123.456;
        var longValue = 789L;

        // Act
        var doubleExemplar = new Exemplar(timestamp, traceId, spanId, doubleValue);
        var longExemplar = new Exemplar(timestamp, traceId, spanId, longValue);

        // Assert
        Assert.Equal(doubleValue, doubleExemplar.ValueAsDouble);
        Assert.Equal(BitConverter.DoubleToInt64Bits(doubleValue), doubleExemplar.ValueAsLong);

        Assert.Equal(longValue, longExemplar.ValueAsLong);
        Assert.Equal(BitConverter.Int64BitsToDouble(longValue), longExemplar.ValueAsDouble);
    }

    [Fact]
    public void Exemplar_IsReadOnlyRecordStruct()
    {
        // Verify that Exemplar is a readonly record struct
        var exemplarType = typeof(Exemplar);
        Assert.True(exemplarType.IsValueType);

        // Check that it has record struct characteristics
        var toStringMethod = exemplarType.GetMethod("ToString", Type.EmptyTypes);
        Assert.NotNull(toStringMethod);
        Assert.True(toStringMethod!.IsVirtual); // Records override ToString
    }

    [Fact]
    public void Constructor_WithNullAttributeValues_HandlesCorrectly()
    {
        // Arrange
        var attributes = new KeyValuePair<string, object?>[]
        {
            new("key1", null),
            new("key2", "value2")
        };

        // Act
        var exemplar = new Exemplar(
            DateTime.UtcNow,
            ActivityTraceId.CreateRandom(),
            ActivitySpanId.CreateRandom(),
            100L,
            attributes);

        // Assert
        ref readonly var filteredAttributes = ref Exemplar.GetFilteredAttributesReference(in exemplar);
        Assert.Equal(2, filteredAttributes.Count);
        Assert.Contains(new KeyValuePair<string, object?>("key1", null), filteredAttributes);
        Assert.Contains(new KeyValuePair<string, object?>("key2", "value2"), filteredAttributes);
    }
}
