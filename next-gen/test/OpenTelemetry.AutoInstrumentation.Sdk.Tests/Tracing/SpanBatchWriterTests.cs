// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.Resources;
using OpenTelemetry.Tracing;

using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Sdk.Tests.Tracing;

public sealed class SpanBatchWriterTests
{
    [Fact]
    public void BeginBatch_WithResource_DoesNotThrow()
    {
        // Arrange
        var writer = new TestSpanBatchWriter();
        var resource = new Resource(new Dictionary<string, object> { ["service.name"] = "test-service" });

        // Act & Assert
        writer.BeginBatch(resource);
        Assert.True(writer.BeginBatchCalled);
        Assert.Equal(resource, writer.LastResource);
    }

    [Fact]
    public void EndBatch_CallSucceeds()
    {
        // Arrange
        var writer = new TestSpanBatchWriter();

        // Act & Assert
        writer.EndBatch();
        Assert.True(writer.EndBatchCalled);
    }

    [Fact]
    public void BeginInstrumentationScope_WithScope_DoesNotThrow()
    {
        // Arrange
        var writer = new TestSpanBatchWriter();
        var scope = new InstrumentationScope("test-scope");

        // Act & Assert
        writer.BeginInstrumentationScope(scope);
        Assert.True(writer.BeginInstrumentationScopeCalled);
        Assert.Equal(scope, writer.LastInstrumentationScope);
    }

    [Fact]
    public void EndInstrumentationScope_CallSucceeds()
    {
        // Arrange
        var writer = new TestSpanBatchWriter();

        // Act & Assert
        writer.EndInstrumentationScope();
        Assert.True(writer.EndInstrumentationScopeCalled);
    }

    [Fact]
    public void WriteSpan_WithValidSpan_DoesNotThrow()
    {
        // Arrange
        var writer = new TestSpanBatchWriter();
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

        // Act & Assert
        writer.WriteSpan(in span);
        Assert.True(writer.WriteSpanCalled);
        Assert.Equal("test-span", writer.LastSpanName);
    }

    [Fact]
    public void FullWorkflow_WithMultipleSpans_ExecutesCorrectly()
    {
        // Arrange
        var writer = new TestSpanBatchWriter();
        var resource = new Resource(new Dictionary<string, object> { ["service.name"] = "test-service" });
        var scope = new InstrumentationScope("my-instrumentation");

        var spanInfo1 = new SpanInfo(scope, "span1")
        {
            TraceId = ActivityTraceId.CreateRandom(),
            SpanId = ActivitySpanId.CreateRandom(),
            TraceFlags = ActivityTraceFlags.Recorded,
            StartTimestampUtc = DateTime.UtcNow,
            EndTimestampUtc = DateTime.UtcNow.AddMilliseconds(50)
        };

        var spanInfo2 = new SpanInfo(scope, "span2")
        {
            TraceId = ActivityTraceId.CreateRandom(),
            SpanId = ActivitySpanId.CreateRandom(),
            TraceFlags = ActivityTraceFlags.None,
            StartTimestampUtc = DateTime.UtcNow,
            EndTimestampUtc = DateTime.UtcNow.AddMilliseconds(75)
        };

        var span1 = new Span(in spanInfo1);
        var span2 = new Span(in spanInfo2);

        // Act
        writer.BeginBatch(resource);
        writer.BeginInstrumentationScope(scope);
        writer.WriteSpan(in span1);
        writer.WriteSpan(in span2);
        writer.EndInstrumentationScope();
        writer.EndBatch();

        // Assert
        Assert.True(writer.BeginBatchCalled);
        Assert.True(writer.BeginInstrumentationScopeCalled);
        Assert.True(writer.WriteSpanCalled);
        Assert.True(writer.EndInstrumentationScopeCalled);
        Assert.True(writer.EndBatchCalled);
        Assert.Equal(2, writer.SpanCount);
    }

    [Fact]
    public void BaseImplementations_AreVirtual()
    {
        // This test verifies that all methods can be overridden
        var writer = new DerivedSpanBatchWriter();
        var resource = new Resource(new Dictionary<string, object> { ["service.name"] = "test-service" });
        var scope = new InstrumentationScope("test");
        var spanInfo = new SpanInfo(scope, "test")
        {
            TraceId = ActivityTraceId.CreateRandom(),
            SpanId = ActivitySpanId.CreateRandom(),
            TraceFlags = ActivityTraceFlags.None,
            StartTimestampUtc = DateTime.UtcNow,
            EndTimestampUtc = DateTime.UtcNow.AddMilliseconds(100)
        };
        var span = new Span(in spanInfo);

        // Act & Assert - All methods should call the derived implementations
        writer.BeginBatch(resource);
        Assert.True(writer.BeginBatchOverridden);

        writer.EndBatch();
        Assert.True(writer.EndBatchOverridden);

        writer.BeginInstrumentationScope(scope);
        Assert.True(writer.BeginInstrumentationScopeOverridden);

        writer.EndInstrumentationScope();
        Assert.True(writer.EndInstrumentationScopeOverridden);

        writer.WriteSpan(in span);
        Assert.True(writer.WriteSpanOverridden);
    }

    [Fact]
    public void SpanBatchWriter_ImplementsIBatchWriter()
    {
        // Arrange & Act
        var writer = new TestSpanBatchWriter();

        // Assert
        Assert.IsAssignableFrom<IBatchWriter>(writer);
    }

    [Fact]
    public void DefaultConstructor_CreatesValidInstance()
    {
        // Act
        var writer = new TestSpanBatchWriter();

        // Assert
        Assert.NotNull(writer);
        Assert.False(writer.BeginBatchCalled);
        Assert.False(writer.EndBatchCalled);
        Assert.False(writer.BeginInstrumentationScopeCalled);
        Assert.False(writer.EndInstrumentationScopeCalled);
        Assert.False(writer.WriteSpanCalled);
    }

    private sealed class TestSpanBatchWriter : SpanBatchWriter
    {
        public bool BeginBatchCalled { get; private set; }

        public bool EndBatchCalled { get; private set; }

        public bool BeginInstrumentationScopeCalled { get; private set; }

        public bool EndInstrumentationScopeCalled { get; private set; }

        public bool WriteSpanCalled { get; private set; }

        public Resource? LastResource { get; private set; }

        public InstrumentationScope? LastInstrumentationScope { get; private set; }

        public string? LastSpanName { get; private set; }

        public int SpanCount { get; private set; }

        public override void BeginBatch(Resource resource)
        {
            BeginBatchCalled = true;
            LastResource = resource;
        }

        public override void EndBatch()
        {
            EndBatchCalled = true;
        }

        public override void BeginInstrumentationScope(InstrumentationScope instrumentationScope)
        {
            BeginInstrumentationScopeCalled = true;
            LastInstrumentationScope = instrumentationScope;
        }

        public override void EndInstrumentationScope()
        {
            EndInstrumentationScopeCalled = true;
        }

        public override void WriteSpan(in Span span)
        {
            WriteSpanCalled = true;
            LastSpanName = span.Info.Name;
            SpanCount++;
        }
    }

    private sealed class DerivedSpanBatchWriter : SpanBatchWriter
    {
        public bool BeginBatchOverridden { get; private set; }

        public bool EndBatchOverridden { get; private set; }

        public bool BeginInstrumentationScopeOverridden { get; private set; }

        public bool EndInstrumentationScopeOverridden { get; private set; }

        public bool WriteSpanOverridden { get; private set; }

        public override void BeginBatch(Resource resource)
        {
            BeginBatchOverridden = true;
        }

        public override void EndBatch()
        {
            EndBatchOverridden = true;
        }

        public override void BeginInstrumentationScope(InstrumentationScope instrumentationScope)
        {
            BeginInstrumentationScopeOverridden = true;
        }

        public override void EndInstrumentationScope()
        {
            EndInstrumentationScopeOverridden = true;
        }

        public override void WriteSpan(in Span span)
        {
            WriteSpanOverridden = true;
        }
    }
}
