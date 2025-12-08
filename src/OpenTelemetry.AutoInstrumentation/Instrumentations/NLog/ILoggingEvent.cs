// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.DuckTyping;
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
#pragma warning disable SA1201 // Elements should appear in the correct order

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.NLog;

/// <summary>
/// Duck typing struct that wraps NLog's LogEventInfo struct.
/// This struct maps to NLog's LogEventInfo structure to extract logging information
/// for conversion to OpenTelemetry log records.
///
/// Based on: https://github.com/NLog/NLog/blob/master/src/NLog/LogEventInfo.cs
/// </summary>
[DuckCopy]
internal struct ILoggingEvent
{
    /// <summary>
    /// Gets the logging level of the log event.
    /// Maps to NLog's LogLevel property.
    /// </summary>
    public LoggingLevel Level;

    /// <summary>
    /// Gets the name of the logger that created the log event.
    /// Maps to NLog's LoggerName property.
    /// </summary>
    public string? LoggerName;

    /// <summary>
    /// Gets the formatted log message.
    /// Maps to NLog's FormattedMessage property.
    /// </summary>
    public string? FormattedMessage;

    /// <summary>
    /// Gets the exception associated with the log event, if any.
    /// Maps to NLog's Exception property.
    /// </summary>
    public Exception? Exception;

    /// <summary>
    /// Gets the timestamp when the log event was created.
    /// Maps to NLog's TimeStamp property.
    /// </summary>
    public DateTime TimeStamp;

    /// <summary>
    /// Gets the message object before formatting.
    /// Maps to NLog's Message property.
    /// </summary>
    public object? Message;

    /// <summary>
    /// Gets the parameters for the log message.
    /// Maps to NLog's Parameters property.
    /// </summary>
    public object?[]? Parameters;

    /// <summary>
    /// Gets the properties collection for custom properties.
    /// Used for injecting trace context and storing additional metadata.
    /// Maps to NLog's Properties property.
    /// </summary>
    public object? Properties;
}

/// <summary>
/// Duck typing structure that wraps NLog's LogLevel.
/// This provides access to NLog's log level information for mapping to OpenTelemetry severity levels.
///
/// Based on: https://github.com/NLog/NLog/blob/master/src/NLog/LogLevel.cs
/// </summary>
[DuckCopy]
internal struct LoggingLevel
{
    /// <summary>
    /// Gets the numeric value of the log level.
    /// NLog uses ordinal values: Trace=0, Debug=1, Info=2, Warn=3, Error=4, Fatal=5
    /// </summary>
    public int Ordinal;

    /// <summary>
    /// Gets the string name of the log level.
    /// </summary>
    public string Name;
}

