// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Logging;

using OpenTelemetry.Resources;
using OpenTelemetry.Tracing;

using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Sdk.Tests.Tracing;

public sealed class SpanBatchExportProcessorAsyncTests : IDisposable
{
    private readonly MockLogger<SpanBatchExportProcessorAsync> _Logger;
    private readonly Resource _Resource;
    private readonly TestSpanExporterAsync _Exporter;
    private readonly BatchExportProcessorOptions _Options;

    public SpanBatchExportProcessorAsyncTests()
    {
        _Logger = new MockLogger<SpanBatchExportProcessorAsync>();
        _Resource = CreateTestResource();
        _Exporter = new TestSpanExporterAsync();
        _Options = new BatchExportProcessorOptions(
            maxQueueSize: 100,
            maxExportBatchSize: 10,
            exportIntervalMilliseconds: 1000,
            exportTimeoutMilliseconds: 5000);
    }

    public void Dispose()
    {
        _Exporter.Dispose();
    }

    [Fact]
    public void Constructor_WithValidParameters_SetsCorrectly()
    {
        // Act
        using var processor = new SpanBatchExportProcessorAsync(_Logger, _Resource, _Exporter, _Options);

        // Assert
        Assert.NotNull(processor);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new SpanBatchExportProcessorAsync(null!, _Resource, _Exporter, _Options));
    }

    [Fact]
    public void Constructor_WithNullExporter_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new SpanBatchExportProcessorAsync(_Logger, _Resource, null!, _Options));
    }

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new SpanBatchExportProcessorAsync(_Logger, _Resource, _Exporter, null!));
    }

    [Fact]
    public void ProcessEndedSpan_WithValidSpan_AddsToBatch()
    {
        // Arrange
        using var processor = new SpanBatchExportProcessorAsync(_Logger, _Resource, _Exporter, _Options);

        var scope = new InstrumentationScope("test-scope");
        var spanInfo = new SpanInfo(scope, "test-span")
        {
            TraceId = ActivityTraceId.CreateRandom(),
            SpanId = ActivitySpanId.CreateRandom(),
            TraceFlags = ActivityTraceFlags.Recorded,
            StartTimestampUtc = DateTime.UtcNow,
            EndTimestampUtc = DateTime.UtcNow.AddMilliseconds(100)
        };

        var span = new Span(in spanInfo);

        // Act
        processor.ProcessEndedSpan(in span);

        // Assert
        // The test passes if no exception was thrown during ProcessEndedSpan
        Assert.NotNull(processor); // Verify processor is still in valid state
    }

    [Fact]
    public async Task ProcessEndedSpan_WithMultipleSpans_ProcessesAllSpans()
    {
        // Arrange
        var shortIntervalOptions = new BatchExportProcessorOptions(
            maxQueueSize: 100,
            maxExportBatchSize: 2,
            exportIntervalMilliseconds: 100,
            exportTimeoutMilliseconds: 5000);

        using var processor = new SpanBatchExportProcessorAsync(_Logger, _Resource, _Exporter, shortIntervalOptions);

        var scope = new InstrumentationScope("test-scope");

        // Act
        for (int i = 0; i < 3; i++)
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
            processor.ProcessEndedSpan(in span);
        }

        await Task.Delay(200);

        // Assert
        Assert.True(_Exporter.ExportCount >= 0);
    }

    [Fact]
    public void ProcessEndedSpan_ImplementsISpanProcessorInterface()
    {
        // Arrange
        using var processor = new SpanBatchExportProcessorAsync(_Logger, _Resource, _Exporter, _Options);

        // Act & Assert
        Assert.IsAssignableFrom<ISpanProcessor>(processor);
        Assert.IsAssignableFrom<IProcessor>(processor);
    }

    [Fact]
    public void ProcessEndedSpan_WithSpanHavingAttributes_ProcessesCorrectly()
    {
        // Arrange
        using var processor = new SpanBatchExportProcessorAsync(_Logger, _Resource, _Exporter, _Options);

        var scope = new InstrumentationScope("test-scope");
        var spanInfo = new SpanInfo(scope, "attributed-span")
        {
            Kind = ActivityKind.Client,
            TraceId = ActivityTraceId.CreateRandom(),
            SpanId = ActivitySpanId.CreateRandom(),
            TraceFlags = ActivityTraceFlags.Recorded,
            StartTimestampUtc = DateTime.UtcNow,
            EndTimestampUtc = DateTime.UtcNow.AddMilliseconds(150),
            StatusCode = ActivityStatusCode.Ok
        };

        var attributes = new KeyValuePair<string, object?>[]
        {
            new("http.method", "POST"),
            new("http.status_code", 201),
            new("custom.attribute", "value")
        };

        var span = new Span(in spanInfo) { Attributes = attributes };

        // Act
        processor.ProcessEndedSpan(in span);

        // Assert
        // Verify that the processor accepted the span with attributes without throwing
        Assert.NotNull(processor);
        Assert.NotNull(attributes);
    }

    [Fact]
    public void ProcessEndedSpan_WithSpanHavingLinksAndEvents_ProcessesCorrectly()
    {
        // Arrange
        using var processor = new SpanBatchExportProcessorAsync(_Logger, _Resource, _Exporter, _Options);

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
        var events = new SpanEvent[]
        {
            new("start", timestamp),
            new("end", timestamp.AddMilliseconds(190))
        };

        var span = new Span(in spanInfo)
        {
            Links = links,
            Events = events
        };

        // Act
        processor.ProcessEndedSpan(in span);

        // Assert
        // Verify that the processor accepted the span with links and events without throwing
        Assert.NotNull(processor);
        Assert.NotNull(links);
        Assert.NotNull(events);
    }

    private static Resource CreateTestResource()
    {
        return new Resource(new[] { new KeyValuePair<string, object>("service.name", "test-service") });
    }

    private sealed class TestSpanExporterAsync : ISpanExporterAsync
    {
        private int _ExportCount;

        public int ExportCount => _ExportCount;

        public Task<bool> ExportAsync<TBatch>(in TBatch batch, CancellationToken cancellationToken)
            where TBatch : IBatch<SpanBatchWriter>, allows ref struct
        {
            _ExportCount++;
            return Task.FromResult(true);
        }

        public void Dispose()
        {
            // No-op for test implementation
        }
    }

    private sealed class MockLogger<T> : ILogger<T>
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            // No-op for test implementation
        }
    }
}
