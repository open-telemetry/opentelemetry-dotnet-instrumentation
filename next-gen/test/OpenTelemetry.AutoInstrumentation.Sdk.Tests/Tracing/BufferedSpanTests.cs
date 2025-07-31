// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.Tracing;

using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Sdk.Tests.Tracing;

public sealed class BufferedSpanTests
{
    [Fact]
    public void Constructor_WithValidSpan_CopiesAllData()
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
            StatusCode = ActivityStatusCode.Ok,
            StatusDescription = "Success"
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

        var span = new Span(in spanInfo)
        {
            Attributes = attributes,
            Links = links,
            Events = events
        };

        // Act
        var bufferedSpan = new BufferedSpan(in span);

        // Assert
        Assert.Equal(spanInfo, bufferedSpan.Info);
        Assert.Equal(scope, bufferedSpan.Scope);
        Assert.Null(bufferedSpan.Next);
    }

    [Fact]
    public void Constructor_WithSpanWithoutCollections_CopiesCorrectly()
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

        var span = new Span(in spanInfo);

        // Act
        var bufferedSpan = new BufferedSpan(in span);

        // Assert
        Assert.Equal(spanInfo, bufferedSpan.Info);
        Assert.Equal(scope, bufferedSpan.Scope);
    }

    [Fact]
    public void ToSpan_WithBufferedData_RestoresOriginalSpan()
    {
        // Arrange
        var scope = new InstrumentationScope("test-scope");
        var spanInfo = new SpanInfo(scope, "test-span")
        {
            Kind = ActivityKind.Server,
            TraceId = ActivityTraceId.CreateRandom(),
            SpanId = ActivitySpanId.CreateRandom(),
            TraceFlags = ActivityTraceFlags.Recorded,
            StartTimestampUtc = DateTime.UtcNow,
            EndTimestampUtc = DateTime.UtcNow.AddMilliseconds(200)
        };

        var attributes = new KeyValuePair<string, object?>[]
        {
            new("db.system", "postgresql"),
            new("db.name", "testdb")
        };

        var linkContext = new ActivityContext(ActivityTraceId.CreateRandom(), ActivitySpanId.CreateRandom(), ActivityTraceFlags.Recorded);
        var links = new SpanLink[]
        {
            new(in linkContext)
        };

        var timestamp = DateTime.UtcNow;
        var events = new SpanEvent[]
        {
            new("db.query.start", timestamp),
            new("db.query.end", timestamp.AddMilliseconds(150))
        };

        var originalSpan = new Span(in spanInfo)
        {
            Attributes = attributes,
            Links = links,
            Events = events
        };

        var bufferedSpan = new BufferedSpan(in originalSpan);

        // Act
        bufferedSpan.ToSpan(out Span restoredSpan);

        // Assert
        Assert.Equal(originalSpan.Info, restoredSpan.Info);
        Assert.Equal(originalSpan.Attributes.Length, restoredSpan.Attributes.Length);
        Assert.Equal(originalSpan.Links.Length, restoredSpan.Links.Length);
        Assert.Equal(originalSpan.Events.Length, restoredSpan.Events.Length);

        // Check attributes
        for (int i = 0; i < originalSpan.Attributes.Length; i++)
        {
            Assert.Equal(originalSpan.Attributes[i], restoredSpan.Attributes[i]);
        }

        // Check links
        for (int i = 0; i < originalSpan.Links.Length; i++)
        {
            // Cannot use direct equality comparison due to InlineArray in TagList
            ref readonly var originalContext = ref SpanLink.GetSpanContextReference(in originalSpan.Links[i]);
            ref readonly var restoredContext = ref SpanLink.GetSpanContextReference(in restoredSpan.Links[i]);
            Assert.Equal(originalContext.TraceId, restoredContext.TraceId);
            Assert.Equal(originalContext.SpanId, restoredContext.SpanId);
        }

        // Check events
        for (int i = 0; i < originalSpan.Events.Length; i++)
        {
            // Cannot use direct equality comparison due to InlineArray in TagList
            Assert.Equal(originalSpan.Events[i].Name, restoredSpan.Events[i].Name);
            Assert.Equal(originalSpan.Events[i].TimestampUtc, restoredSpan.Events[i].TimestampUtc);
        }
    }

    [Fact]
    public void ToSpan_WithEmptyCollections_RestoresCorrectly()
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

        var originalSpan = new Span(in spanInfo);
        var bufferedSpan = new BufferedSpan(in originalSpan);

        // Act
        bufferedSpan.ToSpan(out Span restoredSpan);

        // Assert
        Assert.Equal(originalSpan.Info, restoredSpan.Info);
        Assert.True(restoredSpan.Attributes.IsEmpty);
        Assert.True(restoredSpan.Links.IsEmpty);
        Assert.True(restoredSpan.Events.IsEmpty);
    }

    [Fact]
    public void Next_Property_CanBeSetAndRetrieved()
    {
        // Arrange
        var scope = new InstrumentationScope("test-scope");
        var spanInfo1 = new SpanInfo(scope, "span1")
        {
            TraceId = ActivityTraceId.CreateRandom(),
            SpanId = ActivitySpanId.CreateRandom(),
            TraceFlags = ActivityTraceFlags.None,
            StartTimestampUtc = DateTime.UtcNow,
            EndTimestampUtc = DateTime.UtcNow.AddMilliseconds(100)
        };

        var spanInfo2 = new SpanInfo(scope, "span2")
        {
            TraceId = ActivityTraceId.CreateRandom(),
            SpanId = ActivitySpanId.CreateRandom(),
            TraceFlags = ActivityTraceFlags.None,
            StartTimestampUtc = DateTime.UtcNow,
            EndTimestampUtc = DateTime.UtcNow.AddMilliseconds(100)
        };

        var span1 = new Span(in spanInfo1);
        var span2 = new Span(in spanInfo2);

        var bufferedSpan1 = new BufferedSpan(in span1);
        var bufferedSpan2 = new BufferedSpan(in span2);

        // Act
        bufferedSpan1.Next = bufferedSpan2;

        // Assert
        Assert.Equal(bufferedSpan2, bufferedSpan1.Next);
        Assert.Null(bufferedSpan2.Next);
    }

    [Fact]
    public void Scope_Property_ReturnsCorrectScope()
    {
        // Arrange
        var scope = new InstrumentationScope("my-instrumentation-scope");

        var spanInfo = new SpanInfo(scope, "test-span")
        {
            TraceId = ActivityTraceId.CreateRandom(),
            SpanId = ActivitySpanId.CreateRandom(),
            TraceFlags = ActivityTraceFlags.None,
            StartTimestampUtc = DateTime.UtcNow,
            EndTimestampUtc = DateTime.UtcNow.AddMilliseconds(100)
        };

        var span = new Span(in spanInfo);
        var bufferedSpan = new BufferedSpan(in span);

        // Act & Assert
        Assert.NotNull(bufferedSpan.Scope);
        Assert.Equal(scope, bufferedSpan.Scope);
        Assert.Equal("my-instrumentation-scope", bufferedSpan.Scope.Name);
    }

    [Fact]
    public void BufferedSpan_PreservesAttributeValues()
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
            new("string", "value"),
            new("int", 42),
            new("bool", true),
            new("double", 3.14),
            new("null", null),
            new("array", new[] { "a", "b", "c" })
        };

        var span = new Span(in spanInfo) { Attributes = attributes };
        var bufferedSpan = new BufferedSpan(in span);

        // Act
        bufferedSpan.ToSpan(out Span restoredSpan);

        // Assert
        Assert.Equal(6, restoredSpan.Attributes.Length);
        Assert.Contains(new KeyValuePair<string, object?>("string", "value"), restoredSpan.Attributes.ToArray());
        Assert.Contains(new KeyValuePair<string, object?>("int", 42), restoredSpan.Attributes.ToArray());
        Assert.Contains(new KeyValuePair<string, object?>("bool", true), restoredSpan.Attributes.ToArray());
        Assert.Contains(new KeyValuePair<string, object?>("double", 3.14), restoredSpan.Attributes.ToArray());
        Assert.Contains(new KeyValuePair<string, object?>("null", null), restoredSpan.Attributes.ToArray());

        // Check array attribute
        var arrayAttribute = restoredSpan.Attributes.ToArray().First(kvp => kvp.Key == "array");
        Assert.NotNull(arrayAttribute.Value);
        Assert.IsType<string[]>(arrayAttribute.Value);
        var arrayValue = (string[])arrayAttribute.Value!;
        Assert.Equal(new[] { "a", "b", "c" }, arrayValue);
    }

    [Fact]
    public void BufferedSpan_ImplementsIBufferedTelemetryInterface()
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

        var span = new Span(in spanInfo);

        // Act
        var bufferedSpan = new BufferedSpan(in span);

        // Assert - Test interface implementation
        Assert.Equal(scope, bufferedSpan.Scope);
        Assert.Null(bufferedSpan.Next);

        // Test setting Next through interface
        var anotherSpan = new Span(in spanInfo);
        var anotherBufferedSpan = new BufferedSpan(in anotherSpan);
        bufferedSpan.Next = anotherBufferedSpan;
        Assert.Equal(anotherBufferedSpan, bufferedSpan.Next);
    }
}
