// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.Logging;
using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Sdk.Tests.Logging;

public sealed class BufferedLogRecordTests
{
    [Fact]
    public void Constructor_WithValidLogRecord_SetsPropertiesCorrectly()
    {
        // Arrange
        var activityContext = new ActivityContext(
            ActivityTraceId.CreateRandom(),
            ActivitySpanId.CreateRandom(),
            ActivityTraceFlags.Recorded);

        var scope = new InstrumentationScope("test.scope") { Version = "1.0.0" };
        var logRecordInfo = new LogRecordInfo(scope)
        {
            TimestampUtc = DateTime.UtcNow,
            ObservedTimestampUtc = DateTime.UtcNow,
            Severity = LogRecordSeverity.Info,
            SeverityText = "Info",
            Body = "Test log message"
        };

        var attributes = new List<KeyValuePair<string, object?>>
        {
            new("key1", "value1"),
            new("key2", 42),
            new("key3", true)
        };

        var logRecord = new LogRecord(in activityContext, in logRecordInfo)
        {
            Attributes = attributes.ToArray()
        };

        // Act
        var bufferedLogRecord = new BufferedLogRecord(in logRecord);

        // Assert
        Assert.Equal(activityContext, bufferedLogRecord.SpanContext);
        Assert.Equal(logRecordInfo, bufferedLogRecord.Info);
        Assert.Equal(scope, bufferedLogRecord.Scope);
        Assert.Null(bufferedLogRecord.Next);
    }

    [Fact]
    public void Next_Property_CanBeSetAndGet()
    {
        // Arrange
        var activityContext = default(ActivityContext);
        var scope = new InstrumentationScope("test.scope") { Version = "1.0.0" };
        var logRecordInfo = new LogRecordInfo(scope);
        var logRecord = new LogRecord(in activityContext, in logRecordInfo);

        var bufferedLogRecord1 = new BufferedLogRecord(in logRecord);
        var bufferedLogRecord2 = new BufferedLogRecord(in logRecord);

        // Act
        bufferedLogRecord1.Next = bufferedLogRecord2;

        // Assert
        Assert.Same(bufferedLogRecord2, bufferedLogRecord1.Next);
    }

    [Fact]
    public void ToLogRecord_ReconstructsOriginalLogRecord()
    {
        // Arrange
        var activityContext = new ActivityContext(
            ActivityTraceId.CreateRandom(),
            ActivitySpanId.CreateRandom(),
            ActivityTraceFlags.Recorded);

        var scope = new InstrumentationScope("test.scope") { Version = "1.0.0" };
        var originalTimestamp = DateTime.UtcNow;
        var originalObservedTimestamp = DateTime.UtcNow.AddMilliseconds(100);

        var logRecordInfo = new LogRecordInfo(scope)
        {
            TimestampUtc = originalTimestamp,
            ObservedTimestampUtc = originalObservedTimestamp,
            Severity = LogRecordSeverity.Warn,
            SeverityText = "Warning",
            Body = "Test warning message"
        };

        var attributes = new List<KeyValuePair<string, object?>>
        {
            new("service.name", "test-service"),
            new("level", "warn"),
            new("count", 10)
        };

        var originalLogRecord = new LogRecord(in activityContext, in logRecordInfo)
        {
            Attributes = attributes.ToArray()
        };

        var bufferedLogRecord = new BufferedLogRecord(in originalLogRecord);

        // Act
        bufferedLogRecord.ToLogRecord(out LogRecord reconstructedLogRecord);

        // Assert
        Assert.Equal(originalLogRecord.SpanContext, reconstructedLogRecord.SpanContext);
        Assert.Equal(originalLogRecord.Info, reconstructedLogRecord.Info);

        // Verify attributes are correctly reconstructed
        var originalAttributesList = originalLogRecord.Attributes.ToArray();
        var reconstructedAttributesList = reconstructedLogRecord.Attributes.ToArray();

        Assert.Equal(originalAttributesList.Length, reconstructedAttributesList.Length);

        for (int i = 0; i < originalAttributesList.Length; i++)
        {
            Assert.Equal(originalAttributesList[i].Key, reconstructedAttributesList[i].Key);
            Assert.Equal(originalAttributesList[i].Value, reconstructedAttributesList[i].Value);
        }
    }

    [Fact]
    public void ToLogRecord_WithEmptyAttributes_Works()
    {
        // Arrange
        var activityContext = default(ActivityContext);
        var scope = new InstrumentationScope("test.scope") { Version = "1.0.0" };
        var logRecordInfo = new LogRecordInfo(scope);
        var logRecord = new LogRecord(in activityContext, in logRecordInfo);

        var bufferedLogRecord = new BufferedLogRecord(in logRecord);

        // Act
        bufferedLogRecord.ToLogRecord(out LogRecord reconstructedLogRecord);

        // Assert
        Assert.Equal(activityContext, reconstructedLogRecord.SpanContext);
        Assert.Equal(logRecordInfo, reconstructedLogRecord.Info);
        Assert.True(reconstructedLogRecord.Attributes.IsEmpty);
    }

    [Fact]
    public void Scope_ReturnsCorrectInstrumentationScope()
    {
        // Arrange
        var activityContext = default(ActivityContext);
        var scope = new InstrumentationScope("custom.scope") { Version = "2.1.0" };
        var logRecordInfo = new LogRecordInfo(scope);
        var logRecord = new LogRecord(in activityContext, in logRecordInfo);

        // Act
        var bufferedLogRecord = new BufferedLogRecord(in logRecord);

        // Assert
        Assert.Same(scope, bufferedLogRecord.Scope);
        Assert.Equal("custom.scope", bufferedLogRecord.Scope.Name);
        Assert.Equal("2.1.0", bufferedLogRecord.Scope.Version);
    }
}
