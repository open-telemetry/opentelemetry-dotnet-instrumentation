// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.Logging;

using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Sdk.Tests.Logging;

public sealed class LogRecordInfoTests
{
    [Fact]
    public void Constructor_WithValidScope_SetsPropertiesCorrectly()
    {
        // Arrange
        var scope = new InstrumentationScope("test.scope") { Version = "1.0.0" };

        // Act
        var logRecordInfo = new LogRecordInfo(scope);

        // Assert
        Assert.Equal(scope, logRecordInfo.Scope);
        Assert.Equal(LogRecordSeverity.Unspecified, logRecordInfo.Severity);
        Assert.Null(logRecordInfo.SeverityText);
        Assert.Null(logRecordInfo.Body);
    }

    [Fact]
    public void Constructor_WithNullScope_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => new LogRecordInfo(null!));
        Assert.Equal("scope", exception.ParamName);
    }

    [Fact]
    public void TimestampUtc_DefaultValue_IsCloseToNow()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow;
        var scope = new InstrumentationScope("test.scope");

        // Act
        var logRecordInfo = new LogRecordInfo(scope);
        var afterCreation = DateTime.UtcNow;

        // Assert
        Assert.True(logRecordInfo.TimestampUtc >= beforeCreation);
        Assert.True(logRecordInfo.TimestampUtc <= afterCreation);
    }

    [Fact]
    public void ObservedTimestampUtc_DefaultValue_IsCloseToNow()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow;
        var scope = new InstrumentationScope("test.scope");

        // Act
        var logRecordInfo = new LogRecordInfo(scope);
        var afterCreation = DateTime.UtcNow;

        // Assert
        Assert.True(logRecordInfo.ObservedTimestampUtc >= beforeCreation);
        Assert.True(logRecordInfo.ObservedTimestampUtc <= afterCreation);
    }

    [Fact]
    public void TimestampUtc_SetWithLocalTime_ConvertsToUtc()
    {
        // Arrange
        var scope = new InstrumentationScope("test.scope");
        var localTime = new DateTime(2023, 12, 25, 10, 30, 45, DateTimeKind.Local);

        // Act
        var logRecordInfo = new LogRecordInfo(scope)
        {
            TimestampUtc = localTime
        };

        // Assert
        Assert.Equal(DateTimeKind.Utc, logRecordInfo.TimestampUtc.Kind);
        Assert.Equal(localTime.ToUniversalTime(), logRecordInfo.TimestampUtc);
    }

    [Fact]
    public void ObservedTimestampUtc_SetWithLocalTime_ConvertsToUtc()
    {
        // Arrange
        var scope = new InstrumentationScope("test.scope");
        var localTime = new DateTime(2023, 12, 25, 11, 15, 30, DateTimeKind.Local);

        // Act
        var logRecordInfo = new LogRecordInfo(scope)
        {
            ObservedTimestampUtc = localTime
        };

        // Assert
        Assert.Equal(DateTimeKind.Utc, logRecordInfo.ObservedTimestampUtc.Kind);
        Assert.Equal(localTime.ToUniversalTime(), logRecordInfo.ObservedTimestampUtc);
    }

    [Fact]
    public void TimestampUtc_SetWithUtcTime_KeepsUtcTime()
    {
        // Arrange
        var scope = new InstrumentationScope("test.scope");
        var utcTime = new DateTime(2023, 12, 25, 10, 30, 45, DateTimeKind.Utc);

        // Act
        var logRecordInfo = new LogRecordInfo(scope)
        {
            TimestampUtc = utcTime
        };

        // Assert
        Assert.Equal(DateTimeKind.Utc, logRecordInfo.TimestampUtc.Kind);
        Assert.Equal(utcTime, logRecordInfo.TimestampUtc);
    }

    [Fact]
    public void Severity_CanBeSetToAllValues()
    {
        // Arrange
        var scope = new InstrumentationScope("test.scope");
        var severities = Enum.GetValues<LogRecordSeverity>();

        foreach (var severity in severities)
        {
            // Act
            var logRecordInfo = new LogRecordInfo(scope)
            {
                Severity = severity
            };

            // Assert
            Assert.Equal(severity, logRecordInfo.Severity);
        }
    }

    [Fact]
    public void SeverityText_CanBeSetAndRetrieved()
    {
        // Arrange
        var scope = new InstrumentationScope("test.scope");
        const string severityText = "Custom Severity";

        // Act
        var logRecordInfo = new LogRecordInfo(scope)
        {
            SeverityText = severityText
        };

        // Assert
        Assert.Equal(severityText, logRecordInfo.SeverityText);
    }

    [Fact]
    public void Body_CanBeSetAndRetrieved()
    {
        // Arrange
        var scope = new InstrumentationScope("test.scope");
        const string body = "This is a test log message with details.";

        // Act
        var logRecordInfo = new LogRecordInfo(scope)
        {
            Body = body
        };

        // Assert
        Assert.Equal(body, logRecordInfo.Body);
    }

    [Fact]
    public void AllProperties_CanBeSetTogether()
    {
        // Arrange
        var scope = new InstrumentationScope("test.scope") { Version = "2.0.0" };
        var timestamp = new DateTime(2023, 12, 25, 10, 30, 45, DateTimeKind.Utc);
        var observedTimestamp = new DateTime(2023, 12, 25, 10, 30, 50, DateTimeKind.Utc);
        const LogRecordSeverity severity = LogRecordSeverity.Error;
        const string severityText = "ERROR";
        const string body = "An error occurred during processing";

        // Act
        var logRecordInfo = new LogRecordInfo(scope)
        {
            TimestampUtc = timestamp,
            ObservedTimestampUtc = observedTimestamp,
            Severity = severity,
            SeverityText = severityText,
            Body = body
        };

        // Assert
        Assert.Equal(scope, logRecordInfo.Scope);
        Assert.Equal(timestamp, logRecordInfo.TimestampUtc);
        Assert.Equal(observedTimestamp, logRecordInfo.ObservedTimestampUtc);
        Assert.Equal(severity, logRecordInfo.Severity);
        Assert.Equal(severityText, logRecordInfo.SeverityText);
        Assert.Equal(body, logRecordInfo.Body);
    }

    [Fact]
    public void Equality_SameValues_ReturnsTrue()
    {
        // Arrange
        var scope = new InstrumentationScope("test.scope");
        var timestamp = new DateTime(2023, 12, 25, 10, 30, 45, DateTimeKind.Utc);
        var observedTimestamp = new DateTime(2023, 12, 25, 10, 30, 46, DateTimeKind.Utc);

        var logRecordInfo1 = new LogRecordInfo(scope)
        {
            TimestampUtc = timestamp,
            ObservedTimestampUtc = observedTimestamp,
            Severity = LogRecordSeverity.Info,
            Body = "Test message"
        };

        var logRecordInfo2 = new LogRecordInfo(scope)
        {
            TimestampUtc = timestamp,
            ObservedTimestampUtc = observedTimestamp,
            Severity = LogRecordSeverity.Info,
            Body = "Test message"
        };

        // Act & Assert
        Assert.Equal(logRecordInfo1, logRecordInfo2);
        Assert.True(logRecordInfo1.Equals(logRecordInfo2));
        Assert.Equal(logRecordInfo1.GetHashCode(), logRecordInfo2.GetHashCode());
    }

    [Fact]
    public void Equality_DifferentValues_ReturnsFalse()
    {
        // Arrange
        var scope = new InstrumentationScope("test.scope");

        var logRecordInfo1 = new LogRecordInfo(scope)
        {
            Severity = LogRecordSeverity.Info,
            Body = "Test message 1"
        };

        var logRecordInfo2 = new LogRecordInfo(scope)
        {
            Severity = LogRecordSeverity.Error,
            Body = "Test message 2"
        };

        // Act & Assert
        Assert.NotEqual(logRecordInfo1, logRecordInfo2);
        Assert.False(logRecordInfo1.Equals(logRecordInfo2));
    }
}
