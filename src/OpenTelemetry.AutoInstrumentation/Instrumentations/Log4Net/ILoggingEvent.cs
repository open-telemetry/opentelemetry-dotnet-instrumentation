// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections;
using OpenTelemetry.AutoInstrumentation.DuckTyping;
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.Log4Net;

// Wraps https://github.com/apache/logging-log4net/blob/2d68abc25dd77a69926b16234510377c9b63acad/src/log4net/Core/LoggingEvent.cs#L168
internal interface ILoggingEvent
{
    public LoggingLevel Level { get; }

    public string? LoggerName { get; }

    public string? RenderedMessage { get; }

    public Exception? ExceptionObject { get; }

    public DateTime TimeStampUtc { get; }

    public object MessageObject { get; }

    // used for injecting trace context
    public IDictionary? Properties { get; }

    public IDictionary? GetProperties();
}

internal interface IStringFormatNew : IDuckType
{
    public string Format { get; set; }

    public object?[]? Args { get; set; }
}

internal interface IStringFormatOld : IDuckType
{
    [DuckField(Name = "m_args")]
    public object[] Args { get; }

    [DuckField(Name = "m_format")]
    public string Format { get; }
}

// Wraps https://github.com/apache/logging-log4net/blob/2d68abc25dd77a69926b16234510377c9b63acad/src/log4net/Core/Level.cs#L86
[DuckCopy]
internal struct LoggingLevel
{
    public int Value;

    public string Name;
}
