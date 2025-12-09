// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

// Source originated from https://github.com/open-telemetry/opentelemetry-dotnet/blob/23609730ddd73c860553de847e67c9b2226cff94/src/OpenTelemetry/Internal/SelfDiagnosticsEventListener.cs

using System.Diagnostics.Tracing;
using System.Globalization;
using OpenTelemetry.AutoInstrumentation.Logging;

namespace OpenTelemetry.AutoInstrumentation.Diagnostics;

/// <summary>
/// SdkSelfDiagnosticsEventListener class enables the events from OpenTelemetry event sources
/// and write the events to the OpenTelemetry.AutoInstrumentation logger
/// </summary>
internal class SdkSelfDiagnosticsEventListener : EventListener
{
    private const string EventSourceNamePrefix = "OpenTelemetry-";

    private readonly object lockObj = new();
    private readonly EventLevel logLevel;
    private readonly IOtelLogger log;
    private readonly List<EventSource>? eventSourcesBeforeConstructor = [];

    public SdkSelfDiagnosticsEventListener(IOtelLogger logger)
    {
        log = logger;
        logLevel = LogLevelToEventLevel(logger.Level);

        List<EventSource>? eventSources;
        lock (lockObj)
        {
            eventSources = this.eventSourcesBeforeConstructor;
            eventSourcesBeforeConstructor = null;
        }

        if (eventSources != null)
        {
            foreach (var eventSource in eventSources)
            {
                EnableEvents(eventSource, logLevel, EventKeywords.All);
            }
        }
    }

    protected override void OnEventSourceCreated(EventSource eventSource)
    {
        if (eventSource.Name.StartsWith(EventSourceNamePrefix, StringComparison.Ordinal))
        {
            // If there are EventSource classes already initialized as of now, this method would be called from
            // the base class constructor before the first line of code in SelfDiagnosticsEventListener constructor.
            // In this case logLevel is always its default value, "LogAlways".
            // Thus we should save the event source and enable them later, when code runs in constructor.
            if (eventSourcesBeforeConstructor != null)
            {
                lock (lockObj)
                {
                    if (eventSourcesBeforeConstructor != null)
                    {
                        eventSourcesBeforeConstructor.Add(eventSource);
                        return;
                    }
                }
            }

            EnableEvents(eventSource, logLevel, EventKeywords.All);
        }

        base.OnEventSourceCreated(eventSource);
    }

    /// <summary>
    /// This method records the events from event sources to a local file, which is provided as a stream object by
    /// SelfDiagnosticsConfigRefresher class. The file size is bound to a upper limit. Once the write position
    /// reaches the end, it will be reset to the beginning of the file.
    /// </summary>
    /// <param name="eventData">Data of the EventSource event.</param>
    protected override void OnEventWritten(EventWrittenEventArgs eventData)
    {
        if (!eventData.EventSource.Name.StartsWith(EventSourceNamePrefix, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        object[] payloadArray;
        if (eventData.Payload != null)
        {
            payloadArray = new object[eventData.Payload.Count];
            eventData.Payload.CopyTo(payloadArray, 0);
        }
        else
        {
            payloadArray = [];
        }

        var message = eventData.Message != null ? string.Format(CultureInfo.InvariantCulture, eventData.Message, payloadArray) : string.Empty;

        switch (eventData.Level)
        {
            case EventLevel.Critical:
            case EventLevel.Error:
                log.Error("EventSource={0}, Message={1}", eventData.EventSource.Name, message, false);
                break;
            case EventLevel.Warning:
                log.Warning("EventSource={0}, Message={1}", eventData.EventSource.Name, message, false);
                break;
            case EventLevel.LogAlways:
            case EventLevel.Informational:
                log.Information("EventSource={0}, Message={1}", eventData.EventSource.Name, message, false);
                break;
            case EventLevel.Verbose:
                log.Debug("EventSource={0}, Message={1}", eventData.EventSource.Name, message, false);
                break;
        }
    }

    private static EventLevel LogLevelToEventLevel(LogLevel loggerLevel)
    {
        return loggerLevel switch
        {
            LogLevel.Debug => EventLevel.Verbose,
            LogLevel.Error => EventLevel.Error,
            LogLevel.Warning => EventLevel.Warning,
            LogLevel.Information => EventLevel.Informational,
            _ => throw new ArgumentOutOfRangeException(nameof(loggerLevel), loggerLevel, null)
        };
    }
}
