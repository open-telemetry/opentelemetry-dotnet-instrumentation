// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics.Tracing;

namespace OpenTelemetry.AutoInstrumentation;

/// <summary>
/// EventSource implementation for OpenTelemetry AutoInstrumentation implementation.
/// </summary>
internal partial class AutoInstrumentationEventSource : EventSource
{
    private AutoInstrumentationEventSource()
    {
    }

    public static AutoInstrumentationEventSource Log { get; } = new();

    /// <summary>Logs as Error level message.</summary>
    /// <param name="message">Error to log.</param>
    [Event(2, Message = "{0}", Level = EventLevel.Error)]
    public void Error(string message)
    {
        WriteEvent(2, message);
    }

    /// <summary>Logs as Warning level message.</summary>
    /// <param name="message">Message to log.</param>
    [Event(3, Message = "{0}", Level = EventLevel.Warning)]
    public void Warning(string message)
    {
        WriteEvent(3, message);
    }

    /// <summary>Logs as Information level message.</summary>
    /// <param name="message">Message to log.</param>
    [Event(4, Message = "{0}", Level = EventLevel.Informational)]
    public void Information(string message)
    {
        WriteEvent(4, message);
    }

    /// <summary>Logs as Verbose level message.</summary>
    /// <param name="message">Message to log.</param>
    [Event(5, Message = "{0}", Level = EventLevel.Verbose)]
    public void Verbose(string message)
    {
        WriteEvent(5, message);
    }
}
