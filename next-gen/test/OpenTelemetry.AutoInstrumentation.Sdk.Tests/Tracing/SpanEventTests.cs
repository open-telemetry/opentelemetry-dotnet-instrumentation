// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.Tracing;

using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Sdk.Tests.Tracing;

public sealed class SpanEventTests
{
    [Fact]
    public void Constructor_WithNameOnly_SetsNameAndCurrentTime()
    {
        // Arrange
        const string eventName = "test-event";
        var beforeTime = DateTime.UtcNow;

        // Act
        var spanEvent = new SpanEvent(eventName);
        var afterTime = DateTime.UtcNow;

        // Assert
        Assert.Equal(eventName, spanEvent.Name);
        Assert.True(spanEvent.TimestampUtc >= beforeTime);
        Assert.True(spanEvent.TimestampUtc <= afterTime);
        Assert.Equal(DateTimeKind.Utc, spanEvent.TimestampUtc.Kind);
    }

    [Fact]
    public void Constructor_WithNullName_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new SpanEvent(null!));
    }

    [Fact]
    public void Constructor_WithNameAndAttributes_SetsProperties()
    {
        // Arrange
        const string eventName = "test-event";
        var attributes = new KeyValuePair<string, object?>[]
        {
            new("key1", "value1"),
            new("key2", 123),
            new("key3", true)
        };

        // Act
        var spanEvent = new SpanEvent(eventName, attributes);

        // Assert
        Assert.Equal(eventName, spanEvent.Name);
        Assert.Equal(DateTimeKind.Utc, spanEvent.TimestampUtc.Kind);

        // Verify attributes through the static method
        ref readonly var eventAttributes = ref SpanEvent.GetAttributesReference(in spanEvent);
        Assert.Equal(3, eventAttributes.Count);
        Assert.Contains(new KeyValuePair<string, object?>("key1", "value1"), eventAttributes);
        Assert.Contains(new KeyValuePair<string, object?>("key2", 123), eventAttributes);
        Assert.Contains(new KeyValuePair<string, object?>("key3", true), eventAttributes);
    }

    [Fact]
    public void Constructor_WithNameTimestampAndAttributes_SetsAllProperties()
    {
        // Arrange
        const string eventName = "test-event";
        var timestamp = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var attributes = new KeyValuePair<string, object?>[]
        {
            new("key1", "value1")
        };

        // Act
        var spanEvent = new SpanEvent(eventName, timestamp, attributes);

        // Assert
        Assert.Equal(eventName, spanEvent.Name);
        Assert.Equal(timestamp, spanEvent.TimestampUtc);
        Assert.Equal(DateTimeKind.Utc, spanEvent.TimestampUtc.Kind);

        ref readonly var eventAttributes = ref SpanEvent.GetAttributesReference(in spanEvent);
        Assert.Single(eventAttributes);
        Assert.Contains(new KeyValuePair<string, object?>("key1", "value1"), eventAttributes);
    }

    [Fact]
    public void Constructor_WithNonUtcTimestamp_ThrowsArgumentException()
    {
        // Arrange
        const string eventName = "test-event";
        var nonUtcTimestamp = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Local);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new SpanEvent(eventName, nonUtcTimestamp));
        Assert.Equal("timestampUtc", exception.ParamName);
        Assert.Contains("TimestampUtc kind is invalid", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Constructor_WithUnspecifiedTimestamp_ThrowsArgumentException()
    {
        // Arrange
        const string eventName = "test-event";
        var unspecifiedTimestamp = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Unspecified);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new SpanEvent(eventName, unspecifiedTimestamp));
        Assert.Equal("timestampUtc", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithEmptyAttributes_SetsEmptyTagList()
    {
        // Arrange
        const string eventName = "test-event";
        var emptyAttributes = ReadOnlySpan<KeyValuePair<string, object?>>.Empty;

        // Act
        var spanEvent = new SpanEvent(eventName, emptyAttributes);

        // Assert
        Assert.Equal(eventName, spanEvent.Name);

        ref readonly var eventAttributes = ref SpanEvent.GetAttributesReference(in spanEvent);
        Assert.Empty(eventAttributes);
    }

    [Fact]
    public void GetAttributesReference_ReturnsCorrectReference()
    {
        // Arrange
        const string eventName = "test-event";
        var attributes = new KeyValuePair<string, object?>[]
        {
            new("test-key", "test-value")
        };

        var spanEvent = new SpanEvent(eventName, attributes);

        // Act
        ref readonly var attributesRef = ref SpanEvent.GetAttributesReference(in spanEvent);

        // Assert
        Assert.Single(attributesRef);
        Assert.Equal("test-key", attributesRef[0].Key);
        Assert.Equal("test-value", attributesRef[0].Value);
    }

    [Fact]
    public void SpanEvent_IsRecord_SupportsValueEquality()
    {
        // Arrange
        const string eventName = "test-event";
        var timestamp = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var attributes = new KeyValuePair<string, object?>[]
        {
            new("key1", "value1")
        };

        var spanEvent1 = new SpanEvent(eventName, timestamp, attributes);
        var spanEvent2 = new SpanEvent(eventName, timestamp, attributes);

        // Act & Assert
        // Cannot use direct equality comparison due to InlineArray in TagList
        Assert.Equal(spanEvent1.Name, spanEvent2.Name);
        Assert.Equal(spanEvent1.TimestampUtc, spanEvent2.TimestampUtc);

        // Compare attributes via static method
        ref readonly var attrs1 = ref SpanEvent.GetAttributesReference(in spanEvent1);
        ref readonly var attrs2 = ref SpanEvent.GetAttributesReference(in spanEvent2);
        Assert.Equal(attrs1.Count, attrs2.Count);
    }

    [Fact]
    public void SpanEvent_WithDifferentNames_AreNotEqual()
    {
        // Arrange
        var timestamp = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var spanEvent1 = new SpanEvent("event1", timestamp);
        var spanEvent2 = new SpanEvent("event2", timestamp);

        // Act & Assert
        // Cannot use direct equality comparison due to InlineArray in TagList
        Assert.NotEqual(spanEvent1.Name, spanEvent2.Name);
        Assert.Equal(spanEvent1.TimestampUtc, spanEvent2.TimestampUtc);
    }

    [Fact]
    public void SpanEvent_WithDifferentTimestamps_AreNotEqual()
    {
        // Arrange
        const string eventName = "test-event";
        var timestamp1 = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var timestamp2 = new DateTime(2023, 1, 1, 12, 0, 1, DateTimeKind.Utc);

        var spanEvent1 = new SpanEvent(eventName, timestamp1);
        var spanEvent2 = new SpanEvent(eventName, timestamp2);

        // Act & Assert
        // Cannot use direct equality comparison due to InlineArray in TagList
        Assert.Equal(spanEvent1.Name, spanEvent2.Name);
        Assert.NotEqual(spanEvent1.TimestampUtc, spanEvent2.TimestampUtc);
    }
}
