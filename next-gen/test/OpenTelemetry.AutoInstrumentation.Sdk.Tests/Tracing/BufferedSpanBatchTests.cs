// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.Resources;
using OpenTelemetry.Tracing;

using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Sdk.Tests.Tracing;

public sealed class BufferedSpanBatchTests
{
    [Fact]
    public void Constructor_WithValidBufferedBatch_SetsCorrectly()
    {
        // Arrange
        var bufferedBatch = CreateBufferedBatch(3);

        // Act
        var spanBatch = new BufferedSpanBatch(bufferedBatch);

        // Assert
        // Verify that the constructor worked by testing that WriteTo can be called
        var writer = new TestSpanBatchWriter();

        // This should not throw an exception if constructor worked correctly
        Assert.NotNull(writer); // Ensure we have a valid writer
        spanBatch.WriteTo(writer);

        // Verify the writer received the expected number of spans
        Assert.Equal(3, writer.WrittenSpans.Count);
    }

    [Fact]
    public void WriteTo_WithValidWriter_CallsWriterCorrectly()
    {
        // Arrange
        var bufferedBatch = CreateBufferedBatch(2);
        var spanBatch = new BufferedSpanBatch(bufferedBatch);
        var writer = new TestSpanBatchWriter();

        // Act
        var result = spanBatch.WriteTo(writer);

        // Assert
        Assert.True(result);
        Assert.Equal(2, writer.WrittenSpans.Count);
        Assert.Equal("span-0", writer.WrittenSpans[0].Info.Name);
        Assert.Equal("span-1", writer.WrittenSpans[1].Info.Name);
    }

    [Fact]
    public void WriteTo_WithEmptyBatch_HandlesCorrectly()
    {
        // Arrange
        var emptyBatch = CreateBufferedBatch(0);
        var spanBatch = new BufferedSpanBatch(emptyBatch);
        var writer = new TestSpanBatchWriter();

        // Act
        var result = spanBatch.WriteTo(writer);

        // Assert
        Assert.True(result); // Should still return true for empty batch
        Assert.Empty(writer.WrittenSpans);
    }

    [Fact]
    public void WriteTo_WithNullWriter_ThrowsException()
    {
        // Arrange
        var bufferedBatch = CreateBufferedBatch(1);
        var spanBatch = new BufferedSpanBatch(bufferedBatch);

        // Act & Assert
        // We cannot use ref structs in lambdas, so we'll use a direct call approach
        try
        {
            spanBatch.WriteTo(null!);
            Assert.Fail("Expected ArgumentNullException was not thrown");
        }
        catch (ArgumentNullException)
        {
            // Expected exception - test passes
        }
    }

    [Fact]
    public void WriteTo_WithSingleSpan_WritesCorrectly()
    {
        // Arrange
        var scope = new InstrumentationScope("test-scope");
        var spanInfo = new SpanInfo(scope, "single-span")
        {
            Kind = ActivityKind.Internal,
            TraceId = ActivityTraceId.CreateRandom(),
            SpanId = ActivitySpanId.CreateRandom(),
            TraceFlags = ActivityTraceFlags.Recorded,
            StartTimestampUtc = DateTime.UtcNow,
            EndTimestampUtc = DateTime.UtcNow.AddMilliseconds(100),
            StatusCode = ActivityStatusCode.Ok
        };

        var attributes = new KeyValuePair<string, object?>[]
        {
            new("operation", "test"),
            new("duration_ms", 100)
        };

        var span = new Span(in spanInfo) { Attributes = attributes };
        var bufferedSpan = new BufferedSpan(in span);

        var resource = new Resource(new Dictionary<string, object> { ["service.name"] = "test-service" });
        var bufferedBatch = new BufferedTelemetryBatch<BufferedSpan>(resource);
        bufferedBatch.Add(bufferedSpan);

        var spanBatch = new BufferedSpanBatch(bufferedBatch);
        var writer = new TestSpanBatchWriter();

        // Act
        var result = spanBatch.WriteTo(writer);

        // Assert
        Assert.True(result);
        Assert.Single(writer.WrittenSpans);

        var writtenSpan = writer.WrittenSpans[0];
        Assert.Equal("single-span", writtenSpan.Info.Name);
        Assert.Equal(ActivityKind.Internal, writtenSpan.Info.Kind);
        Assert.Equal(ActivityStatusCode.Ok, writtenSpan.Info.StatusCode);
        Assert.Equal(2, writtenSpan.Attributes.Length);
    }

    [Fact]
    public void WriteTo_WithSpansContainingLinksAndEvents_WritesAllData()
    {
        // Arrange
        var scope = new InstrumentationScope("test-scope");
        var spanInfo = new SpanInfo(scope, "complex-span")
        {
            TraceId = ActivityTraceId.CreateRandom(),
            SpanId = ActivitySpanId.CreateRandom(),
            TraceFlags = ActivityTraceFlags.Recorded,
            StartTimestampUtc = DateTime.UtcNow,
            EndTimestampUtc = DateTime.UtcNow.AddMilliseconds(200)
        };

        var linkContext = new ActivityContext(ActivityTraceId.CreateRandom(), ActivitySpanId.CreateRandom(), ActivityTraceFlags.None);
        var links = new SpanLink[] { new(in linkContext) };

        var timestamp = DateTime.UtcNow;
        var events = new SpanEvent[] { new("event1", timestamp) };

        var span = new Span(in spanInfo)
        {
            Links = links,
            Events = events
        };

        var bufferedSpan = new BufferedSpan(in span);
        var resource = new Resource(new Dictionary<string, object> { ["service.name"] = "test-service" });
        var bufferedBatch = new BufferedTelemetryBatch<BufferedSpan>(resource);
        bufferedBatch.Add(bufferedSpan);

        var spanBatch = new BufferedSpanBatch(bufferedBatch);
        var writer = new TestSpanBatchWriter();

        // Act
        var result = spanBatch.WriteTo(writer);

        // Assert
        Assert.True(result);
        Assert.Single(writer.WrittenSpans);

        var writtenSpan = writer.WrittenSpans[0];
        Assert.Equal("complex-span", writtenSpan.Info.Name);
        Assert.Single(writtenSpan.Links);
        Assert.Single(writtenSpan.Events);
        Assert.Equal("event1", writtenSpan.Events[0].Name);
    }

    [Fact]
    public void WriteTo_WithWriterThrowingException_PropagatesException()
    {
        // Arrange
        var bufferedBatch = CreateBufferedBatch(1);
        var spanBatch = new BufferedSpanBatch(bufferedBatch);
        var faultyWriter = new FaultySpanBatchWriter();

        // Act & Assert
        // We cannot use ref structs in lambdas, so we'll use a direct call approach
        try
        {
            spanBatch.WriteTo(faultyWriter);
            Assert.Fail("Expected InvalidOperationException was not thrown");
        }
        catch (InvalidOperationException)
        {
            // Expected exception - test passes
        }
    }

    private static BufferedTelemetryBatch<BufferedSpan> CreateBufferedBatch(int spanCount)
    {
        var resource = new Resource(new Dictionary<string, object> { ["service.name"] = "test-service" });
        var bufferedBatch = new BufferedTelemetryBatch<BufferedSpan>(resource);
        var scope = new InstrumentationScope("test-scope");

        for (int i = 0; i < spanCount; i++)
        {
            var spanInfo = new SpanInfo(scope, $"span-{i}")
            {
                TraceId = ActivityTraceId.CreateRandom(),
                SpanId = ActivitySpanId.CreateRandom(),
                TraceFlags = ActivityTraceFlags.None,
                StartTimestampUtc = DateTime.UtcNow,
                EndTimestampUtc = DateTime.UtcNow.AddMilliseconds(100)
            };

            var span = new Span(in spanInfo);
            var bufferedSpan = new BufferedSpan(in span);
            bufferedBatch.Add(bufferedSpan);
        }

        return bufferedBatch;
    }

    private sealed class TestSpanBatchWriter : SpanBatchWriter
    {
        private readonly List<SpanData> _WrittenSpans = new();

        public List<SpanData> WrittenSpans => _WrittenSpans;

        public override void WriteSpan(in Span span)
        {
            // Create a snapshot of the span data since Span is a ref struct
            var spanData = new SpanData
            {
                Info = span.Info,
                Attributes = span.Attributes.ToArray(),
                Links = span.Links.ToArray(),
                Events = span.Events.ToArray()
            };

            _WrittenSpans.Add(spanData);
        }
    }

    private sealed class SpanData
    {
        public SpanInfo Info { get; set; }

        public KeyValuePair<string, object?>[] Attributes { get; set; } = Array.Empty<KeyValuePair<string, object?>>();

        public SpanLink[] Links { get; set; } = Array.Empty<SpanLink>();

        public SpanEvent[] Events { get; set; } = Array.Empty<SpanEvent>();
    }

    private sealed class FaultySpanBatchWriter : SpanBatchWriter
    {
        public override void WriteSpan(in Span span)
        {
            throw new InvalidOperationException("Simulated writer failure");
        }
    }
}
