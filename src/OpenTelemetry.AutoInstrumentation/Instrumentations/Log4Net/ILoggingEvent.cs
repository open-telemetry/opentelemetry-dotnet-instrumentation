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

    public IDictionary? GetProperties();
}

// Wraps https://github.com/apache/logging-log4net/blob/2d68abc25dd77a69926b16234510377c9b63acad/src/log4net/Core/Level.cs#L86
[DuckCopy]
internal struct LoggingLevel
{
    public int Value;

    public string Name;
}
