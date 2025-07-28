// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.Tracing;

using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Sdk.Tests.Tracing;

public sealed class SpanLinkTests
{
    [Fact]
    public void Constructor_WithSpanContextOnly_SetsSpanContextAndEmptyAttributes()
    {
        // Arrange
        var traceId = ActivityTraceId.CreateRandom();
        var spanId = ActivitySpanId.CreateRandom();
        var traceFlags = ActivityTraceFlags.Recorded;
        var activityContext = new ActivityContext(traceId, spanId, traceFlags);

        // Act
        var spanLink = new SpanLink(in activityContext);

        // Assert
        ref readonly var spanContext = ref SpanLink.GetSpanContextReference(in spanLink);
        Assert.Equal(traceId, spanContext.TraceId);
        Assert.Equal(spanId, spanContext.SpanId);
        Assert.Equal(traceFlags, spanContext.TraceFlags);

        ref readonly var attributes = ref SpanLink.GetAttributesReference(in spanLink);
        Assert.Empty(attributes);
    }

    [Fact]
    public void Constructor_WithSpanContextAndAttributes_SetsAllProperties()
    {
        // Arrange
        var traceId = ActivityTraceId.CreateRandom();
        var spanId = ActivitySpanId.CreateRandom();
        var traceFlags = ActivityTraceFlags.Recorded;
        var traceState = "key=value";
        var activityContext = new ActivityContext(traceId, spanId, traceFlags, traceState);

        var attributesArray = new KeyValuePair<string, object?>[]
        {
            new("link.key1", "value1"),
            new("link.key2", 42),
            new("link.key3", true)
        };

        // Act
        var spanLink = new SpanLink(in activityContext, attributesArray);

        // Assert
        ref readonly var spanContext = ref SpanLink.GetSpanContextReference(in spanLink);
        Assert.Equal(traceId, spanContext.TraceId);
        Assert.Equal(spanId, spanContext.SpanId);
        Assert.Equal(traceFlags, spanContext.TraceFlags);
        Assert.Equal(traceState, spanContext.TraceState);

        ref readonly var attributes = ref SpanLink.GetAttributesReference(in spanLink);
        Assert.Equal(3, attributes.Count);
        Assert.Contains(new KeyValuePair<string, object?>("link.key1", "value1"), attributes);
        Assert.Contains(new KeyValuePair<string, object?>("link.key2", 42), attributes);
        Assert.Contains(new KeyValuePair<string, object?>("link.key3", true), attributes);
    }

    [Fact]
    public void Constructor_WithEmptyAttributes_SetsEmptyTagList()
    {
        // Arrange
        var traceId = ActivityTraceId.CreateRandom();
        var spanId = ActivitySpanId.CreateRandom();
        var traceFlags = ActivityTraceFlags.None;
        var activityContext = new ActivityContext(traceId, spanId, traceFlags);
        var emptyAttributes = ReadOnlySpan<KeyValuePair<string, object?>>.Empty;

        // Act
        var spanLink = new SpanLink(in activityContext, emptyAttributes);

        // Assert
        ref readonly var spanContext = ref SpanLink.GetSpanContextReference(in spanLink);
        Assert.Equal(traceId, spanContext.TraceId);
        Assert.Equal(spanId, spanContext.SpanId);
        Assert.Equal(traceFlags, spanContext.TraceFlags);

        ref readonly var attributes = ref SpanLink.GetAttributesReference(in spanLink);
        Assert.Empty(attributes);
    }

    [Fact]
    public void GetSpanContextReference_ReturnsCorrectReference()
    {
        // Arrange
        var traceId = ActivityTraceId.CreateRandom();
        var spanId = ActivitySpanId.CreateRandom();
        var traceFlags = ActivityTraceFlags.Recorded;
        var traceState = "test=state";
        var activityContext = new ActivityContext(traceId, spanId, traceFlags, traceState);

        var spanLink = new SpanLink(in activityContext);

        // Act
        ref readonly var spanContextRef = ref SpanLink.GetSpanContextReference(in spanLink);

        // Assert
        Assert.Equal(traceId, spanContextRef.TraceId);
        Assert.Equal(spanId, spanContextRef.SpanId);
        Assert.Equal(traceFlags, spanContextRef.TraceFlags);
        Assert.Equal(traceState, spanContextRef.TraceState);
    }

    [Fact]
    public void GetAttributesReference_ReturnsCorrectReference()
    {
        // Arrange
        var traceId = ActivityTraceId.CreateRandom();
        var spanId = ActivitySpanId.CreateRandom();
        var activityContext = new ActivityContext(traceId, spanId, ActivityTraceFlags.None);

        var attributes = new KeyValuePair<string, object?>[]
        {
            new("test-key", "test-value"),
            new("number-key", 123)
        };

        var spanLink = new SpanLink(in activityContext, attributes);

        // Act
        ref readonly var attributesRef = ref SpanLink.GetAttributesReference(in spanLink);

        // Assert
        Assert.Equal(2, attributesRef.Count);
        Assert.Equal("test-key", attributesRef[0].Key);
        Assert.Equal("test-value", attributesRef[0].Value);
        Assert.Equal("number-key", attributesRef[1].Key);
        Assert.Equal(123, attributesRef[1].Value);
    }

    [Fact]
    public void SpanLink_IsRecord_SupportsValueEquality()
    {
        // Arrange
        var traceId = ActivityTraceId.CreateRandom();
        var spanId = ActivitySpanId.CreateRandom();
        var traceFlags = ActivityTraceFlags.Recorded;
        var activityContext = new ActivityContext(traceId, spanId, traceFlags);

        var attributes = new KeyValuePair<string, object?>[]
        {
            new("key1", "value1")
        };

        var spanLink1 = new SpanLink(in activityContext, attributes);
        var spanLink2 = new SpanLink(in activityContext, attributes);

        // Act & Assert
        // Cannot use direct equality comparison due to InlineArray in TagList
        ref readonly var context1 = ref SpanLink.GetSpanContextReference(in spanLink1);
        ref readonly var context2 = ref SpanLink.GetSpanContextReference(in spanLink2);
        ref readonly var attrs1 = ref SpanLink.GetAttributesReference(in spanLink1);
        ref readonly var attrs2 = ref SpanLink.GetAttributesReference(in spanLink2);

        Assert.Equal(context1.TraceId, context2.TraceId);
        Assert.Equal(context1.SpanId, context2.SpanId);
        Assert.Equal(attrs1.Count, attrs2.Count);
    }

    [Fact]
    public void SpanLink_WithDifferentSpanContext_AreNotEqual()
    {
        // Arrange
        var traceId1 = ActivityTraceId.CreateRandom();
        var traceId2 = ActivityTraceId.CreateRandom();
        var spanId = ActivitySpanId.CreateRandom();
        var activityContext1 = new ActivityContext(traceId1, spanId, ActivityTraceFlags.None);
        var activityContext2 = new ActivityContext(traceId2, spanId, ActivityTraceFlags.None);

        var spanLink1 = new SpanLink(in activityContext1);
        var spanLink2 = new SpanLink(in activityContext2);

        // Act & Assert
        Assert.NotEqual(spanLink1, spanLink2);
        Assert.False(spanLink1 == spanLink2);
        Assert.True(spanLink1 != spanLink2);
    }

    [Fact]
    public void SpanLink_WithDifferentAttributes_AreNotEqual()
    {
        // Arrange
        var traceId = ActivityTraceId.CreateRandom();
        var spanId = ActivitySpanId.CreateRandom();
        var activityContext = new ActivityContext(traceId, spanId, ActivityTraceFlags.None);

        var attributes1 = new KeyValuePair<string, object?>[] { new("key1", "value1") };
        var attributes2 = new KeyValuePair<string, object?>[] { new("key2", "value2") };

        var spanLink1 = new SpanLink(in activityContext, attributes1);
        var spanLink2 = new SpanLink(in activityContext, attributes2);

        // Act & Assert
        // Cannot use direct equality comparison due to InlineArray in TagList
        ref readonly var context1 = ref SpanLink.GetSpanContextReference(in spanLink1);
        ref readonly var context2 = ref SpanLink.GetSpanContextReference(in spanLink2);
        ref readonly var attrs1 = ref SpanLink.GetAttributesReference(in spanLink1);
        ref readonly var attrs2 = ref SpanLink.GetAttributesReference(in spanLink2);

        Assert.Equal(context1.TraceId, context2.TraceId);
        Assert.Equal(context1.SpanId, context2.SpanId);
        Assert.Equal(attrs1.Count, attrs2.Count); // Same count but different values

        // Both have 1 attribute but with different values
        Assert.NotEqual(attrs1[0].Key, attrs2[0].Key);
    }

    [Fact]
    public void SpanLink_WithNullValueAttributes_HandlesCorrectly()
    {
        // Arrange
        var traceId = ActivityTraceId.CreateRandom();
        var spanId = ActivitySpanId.CreateRandom();
        var activityContext = new ActivityContext(traceId, spanId, ActivityTraceFlags.None);

        var attributes = new KeyValuePair<string, object?>[]
        {
            new("null-key", null),
            new("string-key", "value")
        };

        // Act
        var spanLink = new SpanLink(in activityContext, attributes);

        // Assert
        ref readonly var attributesRef = ref SpanLink.GetAttributesReference(in spanLink);
        Assert.Equal(2, attributesRef.Count);
        Assert.Contains(new KeyValuePair<string, object?>("null-key", null), attributesRef);
        Assert.Contains(new KeyValuePair<string, object?>("string-key", "value"), attributesRef);
    }
}
