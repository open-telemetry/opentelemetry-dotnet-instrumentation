// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.Logging;
using OpenTelemetry.Resources;

using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Sdk.Tests.Logging;

public sealed class LogRecordBatchWriterTests
{
    [Fact]
    public void Constructor_CanBeCreated()
    {
        // Act & Assert - Should not throw
        var writer = new TestLogRecordBatchWriter();
        Assert.NotNull(writer);
    }

    [Fact]
    public void BeginBatch_DefaultImplementation_DoesNotThrow()
    {
        // Arrange
        var writer = new TestLogRecordBatchWriter();
        var resource = new Resource(new List<KeyValuePair<string, object>>
        {
            new("service.name", "test-service")
        });

        // Act & Assert - Should not throw
        writer.BeginBatch(resource);
        Assert.True(writer.BeginBatchCalled);
        Assert.Equal(resource, writer.LastResource);
    }

    [Fact]
    public void EndBatch_DefaultImplementation_DoesNotThrow()
    {
        // Arrange
        var writer = new TestLogRecordBatchWriter();

        // Act & Assert - Should not throw
        writer.EndBatch();
        Assert.True(writer.EndBatchCalled);
    }

    [Fact]
    public void BeginInstrumentationScope_DefaultImplementation_DoesNotThrow()
    {
        // Arrange
        var writer = new TestLogRecordBatchWriter();
        var scope = new InstrumentationScope("test.scope") { Version = "1.0.0" };

        // Act & Assert - Should not throw
        writer.BeginInstrumentationScope(scope);
        Assert.True(writer.BeginInstrumentationScopeCalled);
        Assert.Equal(scope, writer.LastInstrumentationScope);
    }

    [Fact]
    public void EndInstrumentationScope_DefaultImplementation_DoesNotThrow()
    {
        // Arrange
        var writer = new TestLogRecordBatchWriter();

        // Act & Assert - Should not throw
        writer.EndInstrumentationScope();
        Assert.True(writer.EndInstrumentationScopeCalled);
    }

    [Fact]
    public void WriteLogRecord_DefaultImplementation_DoesNotThrow()
    {
        // Arrange
        var writer = new TestLogRecordBatchWriter();
        var activityContext = default(ActivityContext);
        var scope = new InstrumentationScope("test.scope");
        var logRecordInfo = new LogRecordInfo(scope)
        {
            Severity = LogRecordSeverity.Info,
            Body = "Test message"
        };
        var logRecord = new LogRecord(in activityContext, in logRecordInfo);

        // Act & Assert - Should not throw
        writer.WriteLogRecord(in logRecord);
        Assert.True(writer.WriteLogRecordCalled);
        Assert.Equal(logRecordInfo, writer.LastLogRecordInfo);
    }

    [Fact]
    public void BatchLifecycle_CanBeCalledInSequence()
    {
        // Arrange
        var writer = new TestLogRecordBatchWriter();
        var resource = new Resource(new List<KeyValuePair<string, object>>
        {
            new("service.name", "test-service"),
            new("service.version", "1.0.0")
        });
        var scope = new InstrumentationScope("test.scope") { Version = "2.0.0" };

        // Act
        writer.BeginBatch(resource);
        writer.BeginInstrumentationScope(scope);

        var activityContext = default(ActivityContext);
        var logRecordInfo = new LogRecordInfo(scope)
        {
            Severity = LogRecordSeverity.Warn,
            Body = "Warning message"
        };
        var logRecord = new LogRecord(in activityContext, in logRecordInfo);
        writer.WriteLogRecord(in logRecord);

        writer.EndInstrumentationScope();
        writer.EndBatch();

        // Assert
        Assert.True(writer.BeginBatchCalled);
        Assert.True(writer.BeginInstrumentationScopeCalled);
        Assert.True(writer.WriteLogRecordCalled);
        Assert.True(writer.EndInstrumentationScopeCalled);
        Assert.True(writer.EndBatchCalled);
        Assert.Equal(resource, writer.LastResource);
        Assert.Equal(scope, writer.LastInstrumentationScope);
        Assert.Equal(logRecordInfo, writer.LastLogRecordInfo);
    }

    [Fact]
    public void MultipleLogRecords_CanBeWritten()
    {
        // Arrange
        var writer = new TestLogRecordBatchWriter();
        var activityContext = default(System.Diagnostics.ActivityContext);
        var scope = new InstrumentationScope("test.scope");

        // Act & Assert - Should not throw
        var logRecord1 = new LogRecord(in activityContext, new LogRecordInfo(scope) { Severity = LogRecordSeverity.Debug, Body = "Debug message" });
        writer.WriteLogRecord(in logRecord1);

        var logRecord2 = new LogRecord(in activityContext, new LogRecordInfo(scope) { Severity = LogRecordSeverity.Info, Body = "Info message" });
        writer.WriteLogRecord(in logRecord2);

        var logRecord3 = new LogRecord(in activityContext, new LogRecordInfo(scope) { Severity = LogRecordSeverity.Error, Body = "Error message" });
        writer.WriteLogRecord(in logRecord3);

        Assert.True(writer.WriteLogRecordCalled);
        Assert.Equal("Error message", writer.LastLogRecordInfo?.Body); // Last one written
    }

    [Fact]
    public void Resource_WithEmptyAttributes_HandledCorrectly()
    {
        // Arrange
        var writer = new TestLogRecordBatchWriter();
        var resource = new Resource(new List<KeyValuePair<string, object>>());

        // Act & Assert - Should not throw
        writer.BeginBatch(resource);
        Assert.True(writer.BeginBatchCalled);
        Assert.Equal(resource, writer.LastResource);
        Assert.NotNull(writer.LastResource);
        Assert.True(writer.LastResource.Attributes.IsEmpty);
    }

    [Fact]
    public void InstrumentationScope_WithAllProperties_HandledCorrectly()
    {
        // Arrange
        var writer = new TestLogRecordBatchWriter();
        var attributes = new List<KeyValuePair<string, object?>>
        {
            new("attr1", "value1"),
            new("attr2", 42)
        };

        var scope = new InstrumentationScope("complex.scope")
        {
            Version = "3.1.4",
            Attributes = attributes
        };

        // Act
        writer.BeginInstrumentationScope(scope);

        // Assert
        Assert.True(writer.BeginInstrumentationScopeCalled);
        Assert.Equal(scope, writer.LastInstrumentationScope);
        Assert.Equal("complex.scope", writer.LastInstrumentationScope!.Name);
        Assert.Equal("3.1.4", writer.LastInstrumentationScope.Version);
        Assert.NotNull(writer.LastInstrumentationScope.Attributes);
        Assert.Equal(2, writer.LastInstrumentationScope.Attributes!.Count);
    }

    private sealed class TestLogRecordBatchWriter : LogRecordBatchWriter
    {
        public bool BeginBatchCalled { get; private set; }

        public bool EndBatchCalled { get; private set; }

        public bool BeginInstrumentationScopeCalled { get; private set; }

        public bool EndInstrumentationScopeCalled { get; private set; }

        public bool WriteLogRecordCalled { get; private set; }

        public Resource? LastResource { get; private set; }

        public InstrumentationScope? LastInstrumentationScope { get; private set; }

        public LogRecordInfo? LastLogRecordInfo { get; private set; }

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

        public override void WriteLogRecord(in LogRecord logRecord)
        {
            WriteLogRecordCalled = true;
            LastLogRecordInfo = logRecord.Info;
        }
    }
}
