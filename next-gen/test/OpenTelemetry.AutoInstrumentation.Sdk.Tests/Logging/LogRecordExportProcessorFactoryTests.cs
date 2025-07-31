// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using OpenTelemetry.Logging;

using Xunit;

using Resource = OpenTelemetry.Resources.Resource;

namespace OpenTelemetry.AutoInstrumentation.Sdk.Tests.Logging;

public sealed class LogRecordExportProcessorFactoryTests
{
    [Fact]
    public void CreateBatchExportProcessorAsync_WithValidParameters_ReturnsProcessor()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        using var serviceProvider = services.BuildServiceProvider();
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

        var resource = new Resource(new List<KeyValuePair<string, object>>
        {
            new("service.name", "test-service")
        });

        using var exporter = new TestLogRecordExporter();
        var options = new BatchExportProcessorOptions();

        // Act
        using var processor = LogRecordExportProcessorFactory.CreateBatchExportProcessorAsync(
            loggerFactory,
            resource,
            exporter,
            options);

        // Assert
        Assert.NotNull(processor);
        Assert.IsAssignableFrom<ILogRecordProcessor>(processor);
    }

    [Fact]
    public void CreateBatchExportProcessorAsync_WithNullLoggerFactory_ThrowsArgumentNullException()
    {
        // Arrange
        var resource = new Resource(new List<KeyValuePair<string, object>>());
        using var exporter = new TestLogRecordExporter();
        var options = new BatchExportProcessorOptions();

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            LogRecordExportProcessorFactory.CreateBatchExportProcessorAsync(
                null!,
                resource,
                exporter,
                options));

        Assert.Equal("loggerFactory", exception.ParamName);
    }

    [Fact]
    public void CreateBatchExportProcessorAsync_WithNullResource_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        using var serviceProvider = services.BuildServiceProvider();
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

        using var exporter = new TestLogRecordExporter();
        var options = new BatchExportProcessorOptions();

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            LogRecordExportProcessorFactory.CreateBatchExportProcessorAsync(
                loggerFactory,
                null!,
                exporter,
                options));

        Assert.Equal("resource", exception.ParamName);
    }

    [Fact]
    public void CreateBatchExportProcessorAsync_WithNullExporter_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        using var serviceProvider = services.BuildServiceProvider();
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

        var resource = new Resource(new List<KeyValuePair<string, object>>());
        var options = new BatchExportProcessorOptions();

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            LogRecordExportProcessorFactory.CreateBatchExportProcessorAsync(
                loggerFactory,
                resource,
                null!,
                options));

        Assert.Equal("exporter", exception.ParamName);
    }

    [Fact]
    public void CreateBatchExportProcessorAsync_WithNullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        using var serviceProvider = services.BuildServiceProvider();
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

        var resource = new Resource(new List<KeyValuePair<string, object>>());
        using var exporter = new TestLogRecordExporter();

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            LogRecordExportProcessorFactory.CreateBatchExportProcessorAsync(
                loggerFactory,
                resource,
                exporter,
                null!));

        Assert.Equal("options", exception.ParamName);
    }

    [Fact]
    public void CreateBatchExportProcessorAsync_WithCustomOptions_CreatesProcessor()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        using var serviceProvider = services.BuildServiceProvider();
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

        var resource = new Resource(new List<KeyValuePair<string, object>>
        {
            new("service.name", "custom-service"),
            new("service.version", "1.2.3")
        });

        using var exporter = new TestLogRecordExporter();
        var options = new BatchExportProcessorOptions(
            maxQueueSize: 1000,
            maxExportBatchSize: 100,
            exportIntervalMilliseconds: 2000,
            exportTimeoutMilliseconds: 10000);

        // Act
        using var processor = LogRecordExportProcessorFactory.CreateBatchExportProcessorAsync(
            loggerFactory,
            resource,
            exporter,
            options);

        // Assert
        Assert.NotNull(processor);
        Assert.IsAssignableFrom<ILogRecordProcessor>(processor);
    }

    [Fact]
    public void CreateBatchExportProcessorAsync_ReturnsLogRecordBatchExportProcessorAsync()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        using var serviceProvider = services.BuildServiceProvider();
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

        var resource = new Resource(new List<KeyValuePair<string, object>>());
        using var exporter = new TestLogRecordExporter();
        var options = new BatchExportProcessorOptions();

        // Act
        using var processor = LogRecordExportProcessorFactory.CreateBatchExportProcessorAsync(
            loggerFactory,
            resource,
            exporter,
            options);

        // Assert
        Assert.NotNull(processor);
        Assert.IsType<LogRecordBatchExportProcessorAsync>(processor);
    }

    [Fact]
    public void CreateBatchExportProcessorAsync_ProcessorCanProcessLogRecord()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        using var serviceProvider = services.BuildServiceProvider();
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

        var resource = new Resource(new List<KeyValuePair<string, object>>());
        using var exporter = new TestLogRecordExporter();
        var options = new BatchExportProcessorOptions();

        using var processor = LogRecordExportProcessorFactory.CreateBatchExportProcessorAsync(
            loggerFactory,
            resource,
            exporter,
            options);

        var activityContext = default(System.Diagnostics.ActivityContext);
        var scope = new InstrumentationScope("test.scope");
        var logRecordInfo = new LogRecordInfo(scope)
        {
            Severity = LogRecordSeverity.Info,
            Body = "Test message"
        };
        var logRecord = new LogRecord(in activityContext, in logRecordInfo);

        // Act & Assert - Should not throw and processor should be valid
        processor.ProcessEmittedLogRecord(in logRecord);
        Assert.IsAssignableFrom<ILogRecordProcessor>(processor);
    }

    [Fact]
    public void CreateBatchExportProcessorAsync_WithMultipleProcessors_AllWorkIndependently()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        using var serviceProvider = services.BuildServiceProvider();
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

        var resource1 = new Resource(new List<KeyValuePair<string, object>>
        {
            new("service.name", "service1")
        });
        var resource2 = new Resource(new List<KeyValuePair<string, object>>
        {
            new("service.name", "service2")
        });

        using var exporter1 = new TestLogRecordExporter();
        using var exporter2 = new TestLogRecordExporter();
        var options = new BatchExportProcessorOptions();

        // Act
        using var processor1 = LogRecordExportProcessorFactory.CreateBatchExportProcessorAsync(
            loggerFactory,
            resource1,
            exporter1,
            options);

        using var processor2 = LogRecordExportProcessorFactory.CreateBatchExportProcessorAsync(
            loggerFactory,
            resource2,
            exporter2,
            options);

        // Assert
        Assert.NotNull(processor1);
        Assert.NotNull(processor2);
        Assert.NotSame(processor1, processor2);
        Assert.IsAssignableFrom<ILogRecordProcessor>(processor1);
        Assert.IsAssignableFrom<ILogRecordProcessor>(processor2);
    }

    private sealed class TestLogRecordExporter : ILogRecordExporterAsync
    {
        public bool ExportAsyncCalled { get; private set; }

        public bool DisposeCalled { get; private set; }

        public Task<bool> ExportAsync<TBatch>(in TBatch batch, CancellationToken cancellationToken)
            where TBatch : IBatch<LogRecordBatchWriter>, allows ref struct
        {
            ExportAsyncCalled = true;
            _ = batch;
            _ = cancellationToken;
            return Task.FromResult(true);
        }

        public void Dispose()
        {
            DisposeCalled = true;
        }
    }
}
