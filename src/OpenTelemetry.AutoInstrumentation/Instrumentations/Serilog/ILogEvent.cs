// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections;
using OpenTelemetry.AutoInstrumentation.DuckTyping;
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.Serilog;

// Wraps https://github.com/serilog/serilog/blob/v4.4.0/src/Serilog/Events/LogEvent.cs
internal interface ILogEvent
{
    public DateTimeOffset Timestamp { get; }

    public int Level { get; }

    public IMessageTemplate MessageTemplate { get; }

    public IEnumerable Properties { get; }

    public Exception? Exception { get; }

    public string RenderMessage(IFormatProvider? formatProvider);
}

internal interface IMessageTemplate
{
    public string Text { get; }
}

internal interface IScalarValue
{
    public object? Value { get; }
}

internal interface IStructureValue
{
    public IEnumerable Properties { get; }
}

internal interface IStructureProperty
{
    public string Name { get; }

    public object? Value { get; }
}

// Wraps DictionaryValue and SequenceValue
internal interface IElementsValue
{
    public IEnumerable Elements { get; }
}

internal interface ILoggerConfiguration
{
    [DuckField(Name = "_logEventSinks")]
    public IList LogEventSinks { get; }
}

// Wraps https://github.com/serilog/serilog-extensions-logging/blob/v10.0.0/src/Serilog.Extensions.Logging/Extensions/Logging/LoggerProviderCollectionSink.cs
internal interface ILoggerProviderCollectionSink
{
    [DuckField(Name = "_providers")]
    public ILoggerProviderCollection Providers { get; }
}

internal interface ILoggerProviderCollection
{
    public IEnumerable Providers { get; }
}

// Wraps KeyValuePair<string, LogEventPropertyValue>
[DuckCopy]
internal struct LogEventProperty
{
    public string Key;

    public object? Value;
}

// Wraps KeyValuePair<ScalarValue, LogEventPropertyValue>
[DuckCopy]
internal struct DictionaryElement
{
    public object Key;

    public object? Value;
}
