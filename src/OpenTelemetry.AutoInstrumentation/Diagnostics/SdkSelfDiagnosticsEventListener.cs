// <copyright file="SdkSelfDiagnosticsEventListener.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

// Source originated from https://github.com/open-telemetry/opentelemetry-dotnet/blob/23609730ddd73c860553de847e67c9b2226cff94/src/OpenTelemetry/Internal/SelfDiagnosticsEventListener.cs

using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using OpenTelemetry.AutoInstrumentation.Logging;

namespace OpenTelemetry.AutoInstrumentation.Diagnostics;

/// <summary>
/// SdkSelfDiagnosticsEventListener class enables the events from OpenTelemetry event sources
/// and write the events to the OpenTelemetry.AutoInstrumentation logger
/// </summary>
internal class SdkSelfDiagnosticsEventListener : EventListener
{
    private const string EventSourceNamePrefix = "OpenTelemetry-";

    private static readonly ILogger Log = OtelLogging.GetLogger();
    private readonly object lockObj = new();
    private readonly EventLevel logLevel;
    private readonly List<EventSource> eventSourcesBeforeConstructor = new();

    public SdkSelfDiagnosticsEventListener(EventLevel eventLevel)
    {
        logLevel = eventLevel;

        List<EventSource> eventSources;
        lock (lockObj)
        {
            eventSources = this.eventSourcesBeforeConstructor;
            eventSourcesBeforeConstructor = null;
        }

        foreach (var eventSource in eventSources)
        {
            EnableEvents(eventSource, logLevel, EventKeywords.All);
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
        var payloadArray = new object[eventData.Payload.Count];
        eventData.Payload.CopyTo(payloadArray, 0);

        switch (eventData.Level)
        {
            case EventLevel.Critical:
            case EventLevel.Error:
                Log.Error("EventSource={0}, Message={1}", eventData.EventSource.Name, string.Format(eventData.Message, payloadArray));
                break;
            case EventLevel.Warning:
                Log.Warning("EventSource={0}, Message={1}", eventData.EventSource.Name, string.Format(eventData.Message, payloadArray));
                break;
            case EventLevel.LogAlways:
            case EventLevel.Informational:
                Log.Information("EventSource={0}, Message={1}", eventData.EventSource.Name, string.Format(eventData.Message, payloadArray));
                break;
            case EventLevel.Verbose:
                Log.Debug("EventSource={0}, Message={1}", eventData.EventSource.Name, string.Format(eventData.Message, payloadArray));
                break;
        }
    }
}
