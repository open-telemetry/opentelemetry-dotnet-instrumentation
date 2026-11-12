// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Serilog.Core;
using Serilog.Events;

namespace TestApplication.SerilogBridge;

internal sealed class CapturingSink : ILogEventSink
{
    public List<LogEvent> Events { get; } = [];

    public void Emit(LogEvent logEvent)
    {
        Events.Add(logEvent);
    }
}
