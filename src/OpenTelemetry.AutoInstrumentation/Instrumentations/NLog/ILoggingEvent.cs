// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using OpenTelemetry.AutoInstrumentation.DuckTyping;
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.NLog;

/// <summary>
/// Duck typing interface that wraps NLog's LogEventInfo class.
/// This interface maps to NLog's LogEventInfo structure to extract logging information
/// for conversion to OpenTelemetry log records.
///
/// Based on: https://github.com/NLog/NLog/blob/master/src/NLog/LogEventInfo.cs
/// </summary>
internal interface ILoggingEvent
{
    /// <summary>
    /// Gets the logging level of the log event.
    /// Maps to NLog's LogLevel property.
    /// </summary>
    public LoggingLevel Level { get; }

    /// <summary>
    /// Gets the name of the logger that created the log event.
    /// Maps to NLog's LoggerName property.
    /// </summary>
    public string? LoggerName { get; }

    /// <summary>
    /// Gets the formatted log message.
    /// Maps to NLog's FormattedMessage property.
    /// </summary>
    public string? FormattedMessage { get; }

    /// <summary>
    /// Gets the exception associated with the log event, if any.
    /// Maps to NLog's Exception property.
    /// </summary>
    public Exception? Exception { get; }

    /// <summary>
    /// Gets the timestamp when the log event was created.
    /// Maps to NLog's TimeStamp property.
    /// </summary>
    public DateTime TimeStamp { get; }

    /// <summary>
    /// Gets the message object before formatting.
    /// Maps to NLog's Message property.
    /// </summary>
    public object? Message { get; }

    /// <summary>
    /// Gets the parameters for the log message.
    /// Maps to NLog's Parameters property.
    /// </summary>
    public object?[]? Parameters { get; }

    /// <summary>
    /// Gets the properties collection for custom properties.
    /// Used for injecting trace context and storing additional metadata.
    /// Maps to NLog's Properties property.
    /// </summary>
    public IDictionary? Properties { get; }

    /// <summary>
    /// Gets the context properties dictionary.
    /// Maps to NLog's Properties property with read access.
    /// </summary>
    public IDictionary? GetProperties();
}

/// <summary>
/// Duck typing interface for NLog's message template structure.
/// This represents structured logging information when using message templates.
/// </summary>
internal interface IMessageTemplateParameters : IDuckType
{
    /// <summary>
    /// Gets the message template format string.
    /// </summary>
    public string? MessageTemplate { get; }

    /// <summary>
    /// Gets the parameters for the message template.
    /// </summary>
    public object?[]? Parameters { get; }
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
