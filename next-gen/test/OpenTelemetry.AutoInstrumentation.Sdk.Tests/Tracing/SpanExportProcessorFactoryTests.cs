// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Logging;

using OpenTelemetry.Resources;
using OpenTelemetry.Tracing;

using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Sdk.Tests.Tracing;

public sealed class SpanExportProcessorFactoryTests
{
    [Fact]
    public void CreateBatchExportProcessorAsync_WithValidParameters_ReturnsProcessor()
    {
        // Arrange
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var resource = new Resource(new Dictionary<string, object> { ["service.name"] = "test" });
        using var exporter = new TestSpanExporter();
        var options = new BatchExportProcessorOptions();

        // Act
        // Act
        using var processor = SpanExportProcessorFactory.CreateBatchExportProcessorAsync(
            loggerFactory,
            resource,
            exporter,
            options);

        // Assert
        Assert.NotNull(processor);
        Assert.IsAssignableFrom<ISpanProcessor>(processor);
        Assert.IsAssignableFrom<IProcessor>(processor);
    }

    [Fact]
    public void CreateBatchExportProcessorAsync_WithNullLoggerFactory_ThrowsArgumentNullException()
    {
        // Arrange
        var resource = new Resource(new Dictionary<string, object> { ["service.name"] = "test-service" });
        using var exporter = new TestSpanExporter();
        var options = new BatchExportProcessorOptions();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            SpanExportProcessorFactory.CreateBatchExportProcessorAsync(
                null!,
                resource,
                exporter,
                options));
    }

    [Fact]
    public void CreateBatchExportProcessorAsync_WithNullExporter_ThrowsArgumentNullException()
    {
        // Arrange
        using var loggerFactory = LoggerFactory.Create(builder => { });
        var resource = new Resource(new Dictionary<string, object> { ["service.name"] = "test-service" });
        var options = new BatchExportProcessorOptions();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            SpanExportProcessorFactory.CreateBatchExportProcessorAsync(
                loggerFactory,
                resource,
                null!,
                options));
    }

    [Fact]
    public void CreateBatchExportProcessorAsync_WithNullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        using var loggerFactory = LoggerFactory.Create(builder => { });
        var resource = new Resource(new Dictionary<string, object> { ["service.name"] = "test-service" });
        using var exporter = new TestSpanExporter();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            SpanExportProcessorFactory.CreateBatchExportProcessorAsync(
                loggerFactory,
                resource,
                exporter,
                null!));
    }

    [Fact]
    public void CreateBatchExportProcessorAsync_WithCustomOptions_UsesOptions()
    {
        // Arrange
        using var loggerFactory = LoggerFactory.Create(builder => { });
        var resource = new Resource(new Dictionary<string, object> { ["service.name"] = "test-service" });
        using var exporter = new TestSpanExporter();
        var customOptions = new BatchExportProcessorOptions(
            maxQueueSize: 1000,
            maxExportBatchSize: 100,
            exportIntervalMilliseconds: 2000,
            exportTimeoutMilliseconds: 10000);

        // Act
        using var processor = SpanExportProcessorFactory.CreateBatchExportProcessorAsync(
            loggerFactory,
            resource,
            exporter,
            customOptions);

        // Assert
        Assert.NotNull(processor);
        Assert.IsType<SpanBatchExportProcessorAsync>(processor);
    }

    [Fact]
    public void CreateBatchExportProcessorAsync_WithDifferentResources_CreatesCorrectly()
    {
        // Arrange
        using var loggerFactory = LoggerFactory.Create(builder => { });
        using var exporter = new TestSpanExporter();
        var options = new BatchExportProcessorOptions();

        var emptyResource = new Resource(new Dictionary<string, object> { ["service.name"] = "test-service" });
        var customResource = new Resource(new Dictionary<string, object>
        {
            ["service.name"] = "my-service",
            ["service.version"] = "1.0.0",
            ["deployment.environment"] = "test"
        });

        // Act
        using var processor1 = SpanExportProcessorFactory.CreateBatchExportProcessorAsync(
            loggerFactory, emptyResource, exporter, options);

        using var processor2 = SpanExportProcessorFactory.CreateBatchExportProcessorAsync(
            loggerFactory, customResource, exporter, options);

        // Assert
        Assert.NotNull(processor1);
        Assert.NotNull(processor2);
        Assert.IsType<SpanBatchExportProcessorAsync>(processor1);
        Assert.IsType<SpanBatchExportProcessorAsync>(processor2);
    }

    [Fact]
    public void CreateBatchExportProcessorAsync_ProcessorsAreIndependent()
    {
        // Arrange
        using var loggerFactory = LoggerFactory.Create(builder => { });
        var resource = new Resource(new Dictionary<string, object> { ["service.name"] = "test-service" });
        using var exporter1 = new TestSpanExporter();
        using var exporter2 = new TestSpanExporter();
        var options = new BatchExportProcessorOptions();

        // Act
        using var processor1 = SpanExportProcessorFactory.CreateBatchExportProcessorAsync(
            loggerFactory, resource, exporter1, options);

        using var processor2 = SpanExportProcessorFactory.CreateBatchExportProcessorAsync(
            loggerFactory, resource, exporter2, options);

        // Assert
        Assert.NotNull(processor1);
        Assert.NotNull(processor2);
        Assert.NotSame(processor1, processor2);
    }

    [Fact]
    public void CreateBatchExportProcessorAsync_CanProcessSpans()
    {
        // Arrange
        using var loggerFactory = LoggerFactory.Create(builder => { });
        var resource = new Resource(new Dictionary<string, object> { ["service.name"] = "test-service" });
        using var exporter = new TestSpanExporter();
        var options = new BatchExportProcessorOptions();

        using var processor = SpanExportProcessorFactory.CreateBatchExportProcessorAsync(
            loggerFactory, resource, exporter, options);

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
        Exception? exception = null;
        try
        {
            processor.ProcessEndedSpan(in span);
        }
        catch (Exception ex)
        {
            exception = ex;
        }

        // Assert
        Assert.Null(exception); // Should not throw
        Assert.NotNull(processor);
    }

    [Fact]
    public void CreateBatchExportProcessorAsync_WithMinimalOptions_Works()
    {
        // Arrange
        using var loggerFactory = LoggerFactory.Create(builder => { });
        var resource = new Resource(new Dictionary<string, object> { ["service.name"] = "test-service" });
        using var exporter = new TestSpanExporter();
        var minimalOptions = new BatchExportProcessorOptions(
            maxQueueSize: 1,
            maxExportBatchSize: 1,
            exportIntervalMilliseconds: 1,
            exportTimeoutMilliseconds: 1000);

        // Act
        using var processor = SpanExportProcessorFactory.CreateBatchExportProcessorAsync(
            loggerFactory, resource, exporter, minimalOptions);

        // Assert
        Assert.NotNull(processor);
    }

    private sealed class TestSpanExporter : ISpanExporterAsync
    {
        public Task<bool> ExportAsync<TBatch>(in TBatch batch, CancellationToken cancellationToken)
            where TBatch : IBatch<SpanBatchWriter>, allows ref struct
        {
            return Task.FromResult(true);
        }

        public void Dispose()
        {
            // No-op for test
        }
    }
}
