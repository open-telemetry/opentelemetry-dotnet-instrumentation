// <copyright file="SdkSelfDiagnosticsEventListenerTests.cs" company="OpenTelemetry Authors">
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

// Source originated from https://github.com/open-telemetry/opentelemetry-dotnet/blob/23609730ddd73c860553de847e67c9b2226cff94/test/OpenTelemetry.Tests/Internal/SelfDiagnosticsEventListenerTest.cs

using System.Collections.Generic;
using System.Diagnostics.Tracing;
using FluentAssertions;
using OpenTelemetry.AutoInstrumentation.Diagnostics;
using OpenTelemetry.AutoInstrumentation.Logging;
using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Tests.Diagnostics;

public class SdkSelfDiagnosticsEventListenerTests
{
    [Fact]
    public void EventSourceSetup_LowerSeverity()
    {
        // get default sink using reflection
        var defaultLogger = OtelLogging.GetLogger();
        var sinkField = defaultLogger.GetType().GetField("_sink", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var defaultSink = (ISink)sinkField.GetValue(defaultLogger);

        try
        {
            var testSink = new TestSink();
            var listener = new SdkSelfDiagnosticsEventListener(EventLevel.Error);

            // override default logger's sink with test sink
            sinkField.SetValue(defaultLogger, testSink);

            // Emitting a Verbose event. Or any EventSource event with lower severity than Error.
            AspNetTelemetryEventSource.Log.ActivityRestored("123");
            OpenTelemetrySdkEventSource.Log.ActivityStarted("Activity started", "1");

            testSink.Messages.Should().BeEmpty("events with lower severity than error should not be written.");
        }
        finally
        {
            // restore default sink
            sinkField.SetValue(defaultLogger, defaultSink);
        }
    }

    [Fact]
    public void EventSourceSetup_HigherSeverity()
    {
        // get default sink using reflection
        var defaultLogger = OtelLogging.GetLogger();
        var sinkField = defaultLogger.GetType().GetField("_sink", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var defaultSink = (ISink)sinkField.GetValue(defaultLogger);

        try
        {
            var testSink = new TestSink();

            var listener = new SdkSelfDiagnosticsEventListener(EventLevel.Verbose);

            // override default logger's sink with test sink
            sinkField.SetValue(defaultLogger, testSink);

            // Emitting a Verbose event. Or any EventSource event with lower severity than Error.
            AspNetTelemetryEventSource.Log.ActivityRestored("123");
            OpenTelemetrySdkEventSource.Log.ActivityStarted("Activity started", "1");

            testSink.Messages.Should().Contain(msg => msg.Contains("EventSource=OpenTelemetry-Instrumentation-AspNet-Telemetry, Message=Activity restored, Id='123'"));
            testSink.Messages.Should().Contain(msg => msg.Contains("EventSource=OpenTelemetry-Sdk, Message=Activity started."));
        }
        finally
        {
            // restore default sink
            sinkField.SetValue(defaultLogger, defaultSink);
        }
    }

    [EventSource(Name = "OpenTelemetry-Instrumentation-AspNet-Telemetry", Guid = "1de158cc-f7ce-4293-bd19-2358c93c8186")]
    internal sealed class AspNetTelemetryEventSource : EventSource
    {
        public static readonly AspNetTelemetryEventSource Log = new();

        [Event(4, Message = "Activity restored, Id='{0}'", Level = EventLevel.Informational)]
        public void ActivityRestored(string id)
        {
            this.WriteEvent(4, id);
        }
    }

    [EventSource(Name = "OpenTelemetry-Sdk")]
    internal class OpenTelemetrySdkEventSource : EventSource
    {
        public static readonly OpenTelemetrySdkEventSource Log = new();

        [Event(24, Message = "Activity started. OperationName = '{0}', Id = '{1}'.", Level = EventLevel.Verbose)]
        public void ActivityStarted(string operationName, string id)
        {
            this.WriteEvent(24, operationName, id);
        }
    }

    private class TestSink : ISink
    {
        public IList<string> Messages { get; } = new List<string>();

        public void Write(string message)
        {
            Messages.Add(message);
        }
    }
}
