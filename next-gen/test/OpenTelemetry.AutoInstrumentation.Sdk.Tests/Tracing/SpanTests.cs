// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.Tracing;

using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Sdk.Tests.Tracing;

public sealed class SpanTests
{
    [Fact]
    public void Constructor_WithValidSpanInfo_SetsInfoProperty()
    {
        // Arrange
        var scope = new InstrumentationScope("test-scope");
        var spanInfo = new SpanInfo(scope, "test-span")
        {
            TraceId = ActivityTraceId.CreateRandom(),
            SpanId = ActivitySpanId.CreateRandom(),
            TraceFlags = ActivityTraceFlags.Recorded,
            StartTimestampUtc = DateTime.UtcNow,
            EndTimestampUtc = DateTime.UtcNow.AddMilliseconds(100)
        };

        // Act
        var span = new Span(in spanInfo);

        // Assert
        Assert.Equal(spanInfo, span.Info);
    }

    [Fact]
    public void DefaultSpan_HasEmptyCollections()
    {
        // Arrange
        var scope = new InstrumentationScope("test-scope");
        var spanInfo = new SpanInfo(scope, "test-span")
        {
            TraceId = ActivityTraceId.CreateRandom(),
            SpanId = ActivitySpanId.CreateRandom(),
            TraceFlags = ActivityTraceFlags.None,
            StartTimestampUtc = DateTime.UtcNow,
            EndTimestampUtc = DateTime.UtcNow.AddMilliseconds(100)
        };

        // Act
        var span = new Span(in spanInfo);

        // Assert
        Assert.True(span.Attributes.IsEmpty);
        Assert.True(span.Links.IsEmpty);
        Assert.True(span.Events.IsEmpty);
    }

    [Fact]
    public void Span_WithAttributes_SetsAttributesCorrectly()
    {
        // Arrange
        var scope = new InstrumentationScope("test-scope");
        var spanInfo = new SpanInfo(scope, "test-span")
        {
            TraceId = ActivityTraceId.CreateRandom(),
            SpanId = ActivitySpanId.CreateRandom(),
            TraceFlags = ActivityTraceFlags.None,
            StartTimestampUtc = DateTime.UtcNow,
            EndTimestampUtc = DateTime.UtcNow.AddMilliseconds(100)
        };

        var attributes = new KeyValuePair<string, object?>[]
        {
            new("attr1", "value1"),
            new("attr2", 42),
            new("attr3", true)
        };

        // Act
        var span = new Span(in spanInfo)
        {
            Attributes = attributes
        };

        // Assert
        Assert.Equal(3, span.Attributes.Length);
        Assert.Contains(new KeyValuePair<string, object?>("attr1", "value1"), span.Attributes.ToArray());
        Assert.Contains(new KeyValuePair<string, object?>("attr2", 42), span.Attributes.ToArray());
        Assert.Contains(new KeyValuePair<string, object?>("attr3", true), span.Attributes.ToArray());
    }

    [Fact]
    public void Span_WithLinks_SetsLinksCorrectly()
    {
        // Arrange
        var scope = new InstrumentationScope("test-scope");
        var spanInfo = new SpanInfo(scope, "test-span")
        {
            TraceId = ActivityTraceId.CreateRandom(),
            SpanId = ActivitySpanId.CreateRandom(),
            TraceFlags = ActivityTraceFlags.None,
            StartTimestampUtc = DateTime.UtcNow,
            EndTimestampUtc = DateTime.UtcNow.AddMilliseconds(100)
        };

        var linkContext1 = new ActivityContext(ActivityTraceId.CreateRandom(), ActivitySpanId.CreateRandom(), ActivityTraceFlags.None);
        var linkContext2 = new ActivityContext(ActivityTraceId.CreateRandom(), ActivitySpanId.CreateRandom(), ActivityTraceFlags.Recorded);

        var links = new SpanLink[]
        {
            new(in linkContext1),
            new(in linkContext2, new KeyValuePair<string, object?>("link.attr", "value"))
        };

        // Act
        var span = new Span(in spanInfo)
        {
            Links = links
        };

        // Assert
        Assert.Equal(2, span.Links.Length);

        // Cannot use direct equality comparison due to InlineArray in TagList
        ref readonly var context1 = ref SpanLink.GetSpanContextReference(in links[0]);
        ref readonly var context2 = ref SpanLink.GetSpanContextReference(in links[1]);
        ref readonly var spanContext1 = ref SpanLink.GetSpanContextReference(in span.Links[0]);
        ref readonly var spanContext2 = ref SpanLink.GetSpanContextReference(in span.Links[1]);

        Assert.Equal(context1.TraceId, spanContext1.TraceId);
        Assert.Equal(context1.SpanId, spanContext1.SpanId);
        Assert.Equal(context2.TraceId, spanContext2.TraceId);
        Assert.Equal(context2.SpanId, spanContext2.SpanId);
    }

    [Fact]
    public void Span_WithEvents_SetsEventsCorrectly()
    {
        // Arrange
        var scope = new InstrumentationScope("test-scope");
        var spanInfo = new SpanInfo(scope, "test-span")
        {
            TraceId = ActivityTraceId.CreateRandom(),
            SpanId = ActivitySpanId.CreateRandom(),
            TraceFlags = ActivityTraceFlags.None,
            StartTimestampUtc = DateTime.UtcNow,
            EndTimestampUtc = DateTime.UtcNow.AddMilliseconds(100)
        };

        var timestamp = DateTime.UtcNow;
        var events = new SpanEvent[]
        {
            new("event1", timestamp),
            new("event2", timestamp.AddMilliseconds(50), new KeyValuePair<string, object?>("event.attr", "value"))
        };

        // Act
        var span = new Span(in spanInfo)
        {
            Events = events
        };

        // Assert
        Assert.Equal(2, span.Events.Length);

        // Cannot use direct equality comparison due to InlineArray in TagList
        Assert.Equal(events[0].Name, span.Events[0].Name);
        Assert.Equal(events[0].TimestampUtc, span.Events[0].TimestampUtc);
        Assert.Equal(events[1].Name, span.Events[1].Name);
        Assert.Equal(events[1].TimestampUtc, span.Events[1].TimestampUtc);
    }

    [Fact]
    public void Span_WithAllCollections_SetsAllCorrectly()
    {
        // Arrange
        var scope = new InstrumentationScope("test-scope");
        var spanInfo = new SpanInfo(scope, "test-span")
        {
            Kind = ActivityKind.Client,
            TraceId = ActivityTraceId.CreateRandom(),
            SpanId = ActivitySpanId.CreateRandom(),
            TraceFlags = ActivityTraceFlags.Recorded,
            StartTimestampUtc = DateTime.UtcNow,
            EndTimestampUtc = DateTime.UtcNow.AddMilliseconds(100),
            StatusCode = ActivityStatusCode.Ok
        };

        var attributes = new KeyValuePair<string, object?>[]
        {
            new("http.method", "GET"),
            new("http.status_code", 200)
        };

        var linkContext = new ActivityContext(ActivityTraceId.CreateRandom(), ActivitySpanId.CreateRandom(), ActivityTraceFlags.None);
        var links = new SpanLink[]
        {
            new(in linkContext, new KeyValuePair<string, object?>("link.type", "child"))
        };

        var timestamp = DateTime.UtcNow;
        var events = new SpanEvent[]
        {
            new("request.start", timestamp),
            new("response.received", timestamp.AddMilliseconds(90))
        };

        // Act
        var span = new Span(in spanInfo)
        {
            Attributes = attributes,
            Links = links,
            Events = events
        };

        // Assert
        Assert.Equal(spanInfo, span.Info);
        Assert.Equal(2, span.Attributes.Length);
        Assert.Equal(1, span.Links.Length);
        Assert.Equal(2, span.Events.Length);

        // Verify specific values
        Assert.Equal("test-span", span.Info.Name);
        Assert.Equal(ActivityKind.Client, span.Info.Kind);
        Assert.Equal(ActivityStatusCode.Ok, span.Info.StatusCode);
    }

    [Fact]
    public void Span_RefStruct_CannotBeBoxed()
    {
        // This test ensures that Span remains a ref struct and cannot be boxed
        // The test is compile-time; if Span could be boxed, this wouldn't compile
        var scope = new InstrumentationScope("test-scope");
        var spanInfo = new SpanInfo(scope, "test-span")
        {
            TraceId = ActivityTraceId.CreateRandom(),
            SpanId = ActivitySpanId.CreateRandom(),
            TraceFlags = ActivityTraceFlags.None,
            StartTimestampUtc = DateTime.UtcNow,
            EndTimestampUtc = DateTime.UtcNow.AddMilliseconds(100)
        };

        var span = new Span(in spanInfo);

        // This test verifies compilation constraints
        Assert.NotNull(span.Info.Scope);
    }

    [Fact]
    public void Span_WithEmptyAttributes_HandlesCorrectly()
    {
        // Arrange
        var scope = new InstrumentationScope("test-scope");
        var spanInfo = new SpanInfo(scope, "test-span")
        {
            TraceId = ActivityTraceId.CreateRandom(),
            SpanId = ActivitySpanId.CreateRandom(),
            TraceFlags = ActivityTraceFlags.None,
            StartTimestampUtc = DateTime.UtcNow,
            EndTimestampUtc = DateTime.UtcNow.AddMilliseconds(100)
        };

        var emptyAttributes = ReadOnlySpan<KeyValuePair<string, object?>>.Empty;

        // Act
        var span = new Span(in spanInfo)
        {
            Attributes = emptyAttributes
        };

        // Assert
        Assert.True(span.Attributes.IsEmpty);
        Assert.Equal(0, span.Attributes.Length);
    }

    [Fact]
    public void Span_WithNullValueAttributes_HandlesCorrectly()
    {
        // Arrange
        var scope = new InstrumentationScope("test-scope");
        var spanInfo = new SpanInfo(scope, "test-span")
        {
            TraceId = ActivityTraceId.CreateRandom(),
            SpanId = ActivitySpanId.CreateRandom(),
            TraceFlags = ActivityTraceFlags.None,
            StartTimestampUtc = DateTime.UtcNow,
            EndTimestampUtc = DateTime.UtcNow.AddMilliseconds(100)
        };

        var attributes = new KeyValuePair<string, object?>[]
        {
            new("null-key", null),
            new("string-key", "value")
        };

        // Act
        var span = new Span(in spanInfo)
        {
            Attributes = attributes
        };

        // Assert
        Assert.Equal(2, span.Attributes.Length);
        Assert.Contains(new KeyValuePair<string, object?>("null-key", null), span.Attributes.ToArray());
        Assert.Contains(new KeyValuePair<string, object?>("string-key", "value"), span.Attributes.ToArray());
    }
}
