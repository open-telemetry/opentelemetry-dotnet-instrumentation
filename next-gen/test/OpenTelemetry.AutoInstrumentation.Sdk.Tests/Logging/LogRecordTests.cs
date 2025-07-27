// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.Logging;

using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Sdk.Tests.Logging;

public sealed class LogRecordTests
{
    [Fact]
    public void Constructor_WithValidParameters_SetsPropertiesCorrectly()
    {
        // Arrange
        var activityContext = new System.Diagnostics.ActivityContext(
            System.Diagnostics.ActivityTraceId.CreateRandom(),
            System.Diagnostics.ActivitySpanId.CreateRandom(),
            System.Diagnostics.ActivityTraceFlags.Recorded);

        var scope = new InstrumentationScope("test.scope") { Version = "1.0.0" };
        var logRecordInfo = new LogRecordInfo(scope)
        {
            TimestampUtc = DateTime.UtcNow,
            ObservedTimestampUtc = DateTime.UtcNow.AddMilliseconds(100),
            Severity = LogRecordSeverity.Error,
            SeverityText = "Error",
            Body = "Test error message"
        };

        // Act
        var logRecord = new LogRecord(in activityContext, in logRecordInfo);

        // Assert
        Assert.Equal(activityContext, logRecord.SpanContext);
        Assert.Equal(logRecordInfo, logRecord.Info);
        Assert.True(logRecord.Attributes.IsEmpty);
    }

    [Fact]
    public void Constructor_WithDefaultActivityContext_Works()
    {
        // Arrange
        var activityContext = default(System.Diagnostics.ActivityContext);
        var scope = new InstrumentationScope("test.scope");
        var logRecordInfo = new LogRecordInfo(scope);

        // Act
        var logRecord = new LogRecord(in activityContext, in logRecordInfo);

        // Assert
        Assert.Equal(default(System.Diagnostics.ActivityContext), logRecord.SpanContext);
        Assert.Equal(logRecordInfo, logRecord.Info);
    }

    [Fact]
    public void Attributes_Property_CanBeSetAndRead()
    {
        // Arrange
        var activityContext = default(System.Diagnostics.ActivityContext);
        var scope = new InstrumentationScope("test.scope");
        var logRecordInfo = new LogRecordInfo(scope);

        var attributes = new List<KeyValuePair<string, object?>>
        {
            new("key1", "value1"),
            new("key2", 42),
            new("key3", true),
            new("key4", 3.14)
        };

        // Act
        var logRecord = new LogRecord(in activityContext, in logRecordInfo)
        {
            Attributes = attributes.ToArray()
        };

        // Assert
        Assert.Equal(4, logRecord.Attributes.Length);
        Assert.Equal("key1", logRecord.Attributes[0].Key);
        Assert.Equal("value1", logRecord.Attributes[0].Value);
        Assert.Equal("key2", logRecord.Attributes[1].Key);
        Assert.Equal(42, logRecord.Attributes[1].Value);
        Assert.Equal("key3", logRecord.Attributes[2].Key);
        Assert.True((bool)logRecord.Attributes[2].Value!);
        Assert.Equal("key4", logRecord.Attributes[3].Key);
        Assert.Equal(3.14, logRecord.Attributes[3].Value);
    }

    [Fact]
    public void Attributes_WithEmptyArray_ReturnsEmptySpan()
    {
        // Arrange
        var activityContext = default(System.Diagnostics.ActivityContext);
        var scope = new InstrumentationScope("test.scope");
        var logRecordInfo = new LogRecordInfo(scope);

        // Act
        var logRecord = new LogRecord(in activityContext, in logRecordInfo)
        {
            Attributes = Array.Empty<KeyValuePair<string, object?>>()
        };

        // Assert
        Assert.True(logRecord.Attributes.IsEmpty);
    }

    [Fact]
    public void Attributes_WithNullValues_HandlesCorrectly()
    {
        // Arrange
        var activityContext = default(System.Diagnostics.ActivityContext);
        var scope = new InstrumentationScope("test.scope");
        var logRecordInfo = new LogRecordInfo(scope);

        var attributes = new List<KeyValuePair<string, object?>>
        {
            new("key1", "value1"),
            new("key2", null),
            new("key3", "value3")
        };

        // Act
        var logRecord = new LogRecord(in activityContext, in logRecordInfo)
        {
            Attributes = attributes.ToArray()
        };

        // Assert
        Assert.Equal(3, logRecord.Attributes.Length);
        Assert.Equal("key1", logRecord.Attributes[0].Key);
        Assert.Equal("value1", logRecord.Attributes[0].Value);
        Assert.Equal("key2", logRecord.Attributes[1].Key);
        Assert.Null(logRecord.Attributes[1].Value);
        Assert.Equal("key3", logRecord.Attributes[2].Key);
        Assert.Equal("value3", logRecord.Attributes[2].Value);
    }

    [Fact]
    public void SpanContext_ReferenceEquality_Works()
    {
        // Arrange
        var traceId = System.Diagnostics.ActivityTraceId.CreateRandom();
        var spanId = System.Diagnostics.ActivitySpanId.CreateRandom();
        var activityContext = new System.Diagnostics.ActivityContext(traceId, spanId, System.Diagnostics.ActivityTraceFlags.Recorded);

        var scope = new InstrumentationScope("test.scope");
        var logRecordInfo = new LogRecordInfo(scope);

        // Act
        var logRecord = new LogRecord(in activityContext, in logRecordInfo);

        // Assert
        Assert.Equal(traceId, logRecord.SpanContext.TraceId);
        Assert.Equal(spanId, logRecord.SpanContext.SpanId);
        Assert.Equal(System.Diagnostics.ActivityTraceFlags.Recorded, logRecord.SpanContext.TraceFlags);
    }

    [Fact]
    public void Info_ReferenceEquality_Works()
    {
        // Arrange
        var activityContext = default(System.Diagnostics.ActivityContext);
        var scope = new InstrumentationScope("test.scope");
        var timestamp = DateTime.UtcNow;
        var observedTimestamp = DateTime.UtcNow.AddMilliseconds(50);

        var logRecordInfo = new LogRecordInfo(scope)
        {
            TimestampUtc = timestamp,
            ObservedTimestampUtc = observedTimestamp,
            Severity = LogRecordSeverity.Warn,
            SeverityText = "Warning",
            Body = "Test warning"
        };

        // Act
        var logRecord = new LogRecord(in activityContext, in logRecordInfo);

        // Assert
        Assert.Equal(scope, logRecord.Info.Scope);
        Assert.Equal(timestamp, logRecord.Info.TimestampUtc);
        Assert.Equal(observedTimestamp, logRecord.Info.ObservedTimestampUtc);
        Assert.Equal(LogRecordSeverity.Warn, logRecord.Info.Severity);
        Assert.Equal("Warning", logRecord.Info.SeverityText);
        Assert.Equal("Test warning", logRecord.Info.Body);
    }
}
