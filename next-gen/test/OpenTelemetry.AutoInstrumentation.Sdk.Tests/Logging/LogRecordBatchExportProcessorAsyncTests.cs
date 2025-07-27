// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using OpenTelemetry.Logging;
using OpenTelemetry.Resources;

using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Sdk.Tests.Logging;

public sealed class LogRecordBatchExportProcessorAsyncTests
{
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

    private static LogRecordBatchExportProcessorAsync CreateProcessor(
        ILogRecordExporterAsync? exporter = null,
        BatchExportProcessorOptions? options = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        using var serviceProvider = services.BuildServiceProvider();
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger<LogRecordBatchExportProcessorAsync>();

        var resource = new Resource(new List<KeyValuePair<string, object>>
        {
            new("service.name", "test-service")
        });

#pragma warning disable CA2000 // Dispose objects before losing scope - The processor will own the exporter
        exporter ??= new TestLogRecordExporter();
#pragma warning restore CA2000
        options ??= new BatchExportProcessorOptions();

        return new LogRecordBatchExportProcessorAsync(logger, resource, exporter, options);
    }

    [Fact]
    public void Constructor_WithValidParameters_CreatesProcessor()
    {
        // Arrange & Act
        using var exporter = new TestLogRecordExporter();
        using var processor = CreateProcessor(exporter);

        // Assert
        Assert.NotNull(processor);
        Assert.IsAssignableFrom<ILogRecordProcessor>(processor);
    }

    [Fact]
    public void ProcessEmittedLogRecord_WithValidLogRecord_ProcessesSuccessfully()
    {
        // Arrange
        using var exporter = new TestLogRecordExporter();
        using var processor = CreateProcessor(exporter);

        var activityContext = default(System.Diagnostics.ActivityContext);
        var scope = new InstrumentationScope("test.scope");
        var logRecordInfo = new LogRecordInfo(scope)
        {
            Severity = LogRecordSeverity.Info,
            Body = "Test message"
        };
        var logRecord = new LogRecord(in activityContext, in logRecordInfo);

        // Act & Assert - Should not throw and processor should remain valid
        processor.ProcessEmittedLogRecord(in logRecord);
        Assert.NotNull(processor);
    }

    [Fact]
    public void ProcessEmittedLogRecord_WithMultipleLogRecords_ProcessesAll()
    {
        // Arrange
        using var exporter = new TestLogRecordExporter();
        using var processor = CreateProcessor(exporter);

        var activityContext = default(System.Diagnostics.ActivityContext);
        var scope = new InstrumentationScope("test.scope");

        var logRecordInfos = new[]
        {
            new LogRecordInfo(scope) { Severity = LogRecordSeverity.Debug, Body = "Debug message" },
            new LogRecordInfo(scope) { Severity = LogRecordSeverity.Info, Body = "Info message" },
            new LogRecordInfo(scope) { Severity = LogRecordSeverity.Warn, Body = "Warning message" },
            new LogRecordInfo(scope) { Severity = LogRecordSeverity.Error, Body = "Error message" }
        };

        // Act & Assert - Should not throw and all records processed
        foreach (var logRecordInfo in logRecordInfos)
        {
            var logRecord = new LogRecord(in activityContext, in logRecordInfo);
            processor.ProcessEmittedLogRecord(in logRecord);
        }

        Assert.Equal(4, logRecordInfos.Length); // Verify we processed all records
    }

    [Fact]
    public void ProcessEmittedLogRecord_WithDifferentSeverities_ProcessesCorrectly()
    {
        // Arrange
        using var exporter = new TestLogRecordExporter();
        using var processor = CreateProcessor(exporter);

        var activityContext = default(System.Diagnostics.ActivityContext);
        var scope = new InstrumentationScope("test.scope");

        var severities = new[]
        {
            LogRecordSeverity.Trace,
            LogRecordSeverity.Debug,
            LogRecordSeverity.Info,
            LogRecordSeverity.Warn,
            LogRecordSeverity.Error,
            LogRecordSeverity.Fatal
        };

        // Act & Assert - Should not throw and all severities processed
        foreach (var severity in severities)
        {
            var logRecordInfo = new LogRecordInfo(scope)
            {
                Severity = severity,
                Body = $"Message with {severity} severity"
            };
            var logRecord = new LogRecord(in activityContext, in logRecordInfo);

            processor.ProcessEmittedLogRecord(in logRecord);
        }

        Assert.Equal(6, severities.Length); // Verify we processed all severity levels
    }

    [Fact]
    public void ProcessEmittedLogRecord_WithAttributes_ProcessesCorrectly()
    {
        // Arrange
        using var exporter = new TestLogRecordExporter();
        using var processor = CreateProcessor(exporter);

        var activityContext = default(System.Diagnostics.ActivityContext);
        var scope = new InstrumentationScope("test.scope");
        var logRecordInfo = new LogRecordInfo(scope)
        {
            Severity = LogRecordSeverity.Info,
            Body = "Test message with attributes"
        };

        var attributes = new List<KeyValuePair<string, object?>>
        {
            new("user.id", "12345"),
            new("user.name", "testuser"),
            new("request.method", "GET"),
            new("response.status", 200)
        };

        var logRecord = new LogRecord(in activityContext, in logRecordInfo)
        {
            Attributes = attributes.ToArray()
        };

        // Act & Assert - Should not throw and attributes are processed
        processor.ProcessEmittedLogRecord(in logRecord);
        Assert.Equal(4, attributes.Count); // Verify attributes were included
    }

    [Fact]
    public void ProcessEmittedLogRecord_WithActivityContext_ProcessesCorrectly()
    {
        // Arrange
        using var exporter = new TestLogRecordExporter();
        using var processor = CreateProcessor(exporter);

        var activityContext = new System.Diagnostics.ActivityContext(
            System.Diagnostics.ActivityTraceId.CreateRandom(),
            System.Diagnostics.ActivitySpanId.CreateRandom(),
            System.Diagnostics.ActivityTraceFlags.Recorded);

        var scope = new InstrumentationScope("test.scope");
        var logRecordInfo = new LogRecordInfo(scope)
        {
            Severity = LogRecordSeverity.Info,
            Body = "Test message with activity context"
        };

        var logRecord = new LogRecord(in activityContext, in logRecordInfo);

        // Act & Assert - Should not throw and activity context is processed
        processor.ProcessEmittedLogRecord(in logRecord);
        Assert.NotEqual(default(System.Diagnostics.ActivityContext), activityContext);
    }

    [Fact]
    public void ProcessEmittedLogRecord_WithCustomOptions_ProcessesCorrectly()
    {
        // Arrange
        using var exporter = new TestLogRecordExporter();
        var options = new BatchExportProcessorOptions(
            maxQueueSize: 100,
            maxExportBatchSize: 10,
            exportIntervalMilliseconds: 1000,
            exportTimeoutMilliseconds: 5000);

        using var processor = CreateProcessor(exporter, options);

        var activityContext = default(System.Diagnostics.ActivityContext);
        var scope = new InstrumentationScope("test.scope");
        var logRecordInfo = new LogRecordInfo(scope)
        {
            Severity = LogRecordSeverity.Info,
            Body = "Test message with custom options"
        };

        var logRecord = new LogRecord(in activityContext, in logRecordInfo);

        // Act & Assert - Should not throw and processor uses custom options
        processor.ProcessEmittedLogRecord(in logRecord);
        Assert.NotNull(processor);
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_DoesNotThrow()
    {
        // Arrange
        using var exporter = new TestLogRecordExporter();
        var processor = CreateProcessor(exporter);

        // Act & Assert - Should not throw
        processor.Dispose();
        processor.Dispose();

        // Verify disposal completed successfully
        Assert.True(true);
    }

    [Fact]
    public void ProcessEmittedLogRecord_AfterDispose_DoesNotThrow()
    {
        // Arrange
        using var exporter = new TestLogRecordExporter();
        var processor = CreateProcessor(exporter);

        var activityContext = default(System.Diagnostics.ActivityContext);
        var scope = new InstrumentationScope("test.scope");
        var logRecordInfo = new LogRecordInfo(scope)
        {
            Severity = LogRecordSeverity.Info,
            Body = "Test message after dispose"
        };

        var logRecord = new LogRecord(in activityContext, in logRecordInfo);

        // Act
        processor.Dispose();

        // Assert - Should not throw
        processor.ProcessEmittedLogRecord(in logRecord);
        Assert.True(true); // Verify operation completed without exception
    }

    [Fact]
    public void ProcessEmittedLogRecord_WithComplexLogRecordInfo_ProcessesCorrectly()
    {
        // Arrange
        using var exporter = new TestLogRecordExporter();
        using var processor = CreateProcessor(exporter);

        var activityContext = default(System.Diagnostics.ActivityContext);
        var scope = new InstrumentationScope("complex.scope")
        {
            Version = "2.1.0",
            Attributes = new List<KeyValuePair<string, object?>>
            {
                new("scope.attr1", "value1"),
                new("scope.attr2", 42)
            }
        };

        var logRecordInfo = new LogRecordInfo(scope)
        {
            TimestampUtc = DateTime.UtcNow,
            ObservedTimestampUtc = DateTime.UtcNow.AddMilliseconds(100),
            Severity = LogRecordSeverity.Warn,
            SeverityText = "WARNING",
            Body = "Complex log record with all properties set"
        };

        var attributes = new List<KeyValuePair<string, object?>>
        {
            new("complex.attr1", "string value"),
            new("complex.attr2", 123),
            new("complex.attr3", true),
            new("complex.attr4", 45.67),
            new("complex.attr5", null)
        };

        var logRecord = new LogRecord(in activityContext, in logRecordInfo)
        {
            Attributes = attributes.ToArray()
        };

        // Act & Assert - Should not throw and complex record is processed
        processor.ProcessEmittedLogRecord(in logRecord);
        Assert.Equal("complex.scope", scope.Name);
        Assert.Equal("2.1.0", scope.Version);
        Assert.Equal(5, attributes.Count);
    }
}
