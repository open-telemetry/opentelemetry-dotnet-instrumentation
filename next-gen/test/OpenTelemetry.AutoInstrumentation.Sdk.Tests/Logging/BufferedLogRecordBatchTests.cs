// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.Logging;
using OpenTelemetry.Resources;
using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Sdk.Tests.Logging;

public sealed class BufferedLogRecordBatchTests
{
    [Fact]
    public void Constructor_WithValidBufferedBatch_DoesNotThrow()
    {
        // Arrange
        var resource = new Resource(new List<KeyValuePair<string, object>>());
        var bufferedBatch = new BufferedTelemetryBatch<BufferedLogRecord>(resource);

        // Act & Assert - Should not throw
        _ = new BufferedLogRecordBatch(bufferedBatch);

        // Assert that construction succeeds
        Assert.True(true);
    }

    [Fact]
    public void WriteTo_WithEmptyBatch_WritesNothing()
    {
        // Arrange
        var resource = new Resources.Resource(new List<KeyValuePair<string, object>>());
        var bufferedBatch = new BufferedTelemetryBatch<BufferedLogRecord>(resource);
        var batch = new BufferedLogRecordBatch(bufferedBatch);
        var writer = new TestLogRecordBatchWriter();

        // Act
        bool result = batch.WriteTo(writer);

        // Assert
        Assert.True(result);
        Assert.Equal(0, writer.LogRecordCount);
    }

    [Fact]
    public void WriteTo_WithSingleLogRecord_WritesCorrectly()
    {
        // Arrange
        var resource = new Resource(new List<KeyValuePair<string, object>>());
        var bufferedBatch = new BufferedTelemetryBatch<BufferedLogRecord>(resource);

        var activityContext = new ActivityContext(
            ActivityTraceId.CreateRandom(),
            ActivitySpanId.CreateRandom(),
            ActivityTraceFlags.Recorded);

        var scope = new InstrumentationScope("test.scope") { Version = "1.0.0" };
        var logRecordInfo = new LogRecordInfo(scope)
        {
            TimestampUtc = DateTime.UtcNow,
            Severity = LogRecordSeverity.Info,
            Body = "Test message"
        };

        var attributes = new List<KeyValuePair<string, object?>>
        {
            new("key1", "value1"),
            new("key2", 42)
        };

        var logRecord = new LogRecord(in activityContext, in logRecordInfo)
        {
            Attributes = attributes.ToArray()
        };

        var bufferedLogRecord = new BufferedLogRecord(in logRecord);
        bufferedBatch.Add(bufferedLogRecord);

        var batch = new BufferedLogRecordBatch(bufferedBatch);
        var writer = new TestLogRecordBatchWriter();

        // Act
        bool result = batch.WriteTo(writer);

        // Assert
        Assert.True(result);
        Assert.Equal(1, writer.LogRecordCount);
        Assert.NotNull(writer.LastLogRecordInfo);
        Assert.Equal("Test message", writer.LastLogRecordInfo.Value.Body);
        Assert.Equal(LogRecordSeverity.Info, writer.LastLogRecordInfo.Value.Severity);
    }

    [Fact]
    public void WriteTo_WithMultipleLogRecords_WritesAllCorrectly()
    {
        // Arrange
        var resource = new Resource(new List<KeyValuePair<string, object>>());
        var bufferedBatch = new BufferedTelemetryBatch<BufferedLogRecord>(resource);

        var scope = new InstrumentationScope("test.scope") { Version = "1.0.0" };

        // Create multiple log records
        const int recordCount = 3;
        for (int i = 0; i < recordCount; i++)
        {
            var activityContext = new ActivityContext(
                ActivityTraceId.CreateRandom(),
                ActivitySpanId.CreateRandom(),
                ActivityTraceFlags.Recorded);

            var logRecordInfo = new LogRecordInfo(scope)
            {
                TimestampUtc = DateTime.UtcNow.AddSeconds(i),
                Severity = LogRecordSeverity.Info,
                Body = $"Test message {i}"
            };

            var logRecord = new LogRecord(in activityContext, in logRecordInfo);

            var bufferedLogRecord = new BufferedLogRecord(in logRecord);
            bufferedBatch.Add(bufferedLogRecord);
        }

        var batch = new BufferedLogRecordBatch(bufferedBatch);
        var writer = new TestLogRecordBatchWriter();

        // Act
        bool result = batch.WriteTo(writer);

        // Assert
        Assert.True(result);
        Assert.Equal(recordCount, writer.LogRecordCount);
    }

    [Fact]
    public void WriteTo_WithDifferentSeverities_WritesAllCorrectly()
    {
        // Arrange
        var resource = new Resource(new List<KeyValuePair<string, object>>());
        var bufferedBatch = new BufferedTelemetryBatch<BufferedLogRecord>(resource);

        var scope = new InstrumentationScope("test.scope") { Version = "1.0.0" };
        var severities = new[]
        {
            LogRecordSeverity.Debug,
            LogRecordSeverity.Info,
            LogRecordSeverity.Warn,
            LogRecordSeverity.Error
        };

        foreach (var severity in severities)
        {
            var activityContext = default(ActivityContext);
            var logRecordInfo = new LogRecordInfo(scope)
            {
                Severity = severity,
                Body = $"Message with {severity} severity"
            };

            var logRecord = new LogRecord(in activityContext, in logRecordInfo);
            var bufferedLogRecord = new BufferedLogRecord(in logRecord);
            bufferedBatch.Add(bufferedLogRecord);
        }

        var batch = new BufferedLogRecordBatch(bufferedBatch);
        var writer = new TestLogRecordBatchWriter();

        // Act
        bool result = batch.WriteTo(writer);

        // Assert
        Assert.True(result);
        Assert.Equal(severities.Length, writer.LogRecordCount);
    }

    private sealed class TestLogRecordBatchWriter : LogRecordBatchWriter
    {
        public int LogRecordCount { get; private set; }

        public LogRecordInfo? LastLogRecordInfo { get; private set; }

        public bool BeginBatchCalled { get; private set; }

        public bool EndBatchCalled { get; private set; }

        public bool BeginInstrumentationScopeCalled { get; private set; }

        public bool EndInstrumentationScopeCalled { get; private set; }

        public override void BeginBatch(Resource resource)
        {
            BeginBatchCalled = true;
        }

        public override void EndBatch()
        {
            EndBatchCalled = true;
        }

        public override void BeginInstrumentationScope(InstrumentationScope instrumentationScope)
        {
            BeginInstrumentationScopeCalled = true;
        }

        public override void EndInstrumentationScope()
        {
            EndInstrumentationScopeCalled = true;
        }

        public override void WriteLogRecord(in LogRecord logRecord)
        {
            LogRecordCount++;
            LastLogRecordInfo = logRecord.Info;
        }
    }
}
