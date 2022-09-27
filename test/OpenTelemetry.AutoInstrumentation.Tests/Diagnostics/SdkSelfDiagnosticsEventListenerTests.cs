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
using FluentAssertions.Execution;
using OpenTelemetry.AutoInstrumentation.Diagnostics;
using OpenTelemetry.AutoInstrumentation.Logging;
using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Tests.Diagnostics;

public class SdkSelfDiagnosticsEventListenerTests
{
    [Fact]
    public void EventSourceSetup_LowerSeverity()
    {
        var testSink = new TestSink();
        var logger = new CustomLogger(testSink);
        using var listener = new SdkSelfDiagnosticsEventListener(EventLevel.Error, logger);

        // Emitting a Verbose event. Or any EventSource event with lower severity than Error.
        AspNetTelemetryEventSourceForTests.Log.ActivityRestored("123");
        OpenTelemetrySdkEventSourceForTests.Log.ActivityStarted("Activity started", "1");

        testSink.Messages.Should().BeEmpty("events with lower severity than error should not be written.");
    }

    [Fact]
    public void EventSourceSetup_HigherSeverity()
    {
        var testSink = new TestSink();
        var logger = new CustomLogger(testSink);
        using var listener = new SdkSelfDiagnosticsEventListener(EventLevel.Verbose, logger);

        // Emitting a Verbose event. Or any EventSource event with lower severity than Error.
        AspNetTelemetryEventSourceForTests.Log.ActivityRestored("123");
        OpenTelemetrySdkEventSourceForTests.Log.ActivityStarted("Activity started", "1");

        using (new AssertionScope())
        {
            testSink.Messages.Should().Contain(msg => msg.Contains("EventSource=OpenTelemetry-Instrumentation-AspNet-Telemetry-For-Tests, Message=Activity restored, Id='123'"));
            testSink.Messages.Should().Contain(msg => msg.Contains("EventSource=OpenTelemetry-Sdk-For-Tests, Message=Activity started."));
        }
    }

    [EventSource(Name = "OpenTelemetry-Instrumentation-AspNet-Telemetry-For-Tests")]
    internal sealed class AspNetTelemetryEventSourceForTests : EventSource
    {
        public static readonly AspNetTelemetryEventSourceForTests Log = new();

        private AspNetTelemetryEventSourceForTests()
        {
        }

        [Event(4, Message = "Activity restored, Id='{0}'", Level = EventLevel.Informational)]
        public void ActivityRestored(string id)
        {
            WriteEvent(4, id);
        }
    }

    [EventSource(Name = "OpenTelemetry-Sdk-For-Tests")]
    internal class OpenTelemetrySdkEventSourceForTests : EventSource
    {
        public static readonly OpenTelemetrySdkEventSourceForTests Log = new();

        private OpenTelemetrySdkEventSourceForTests()
        {
        }

        [Event(24, Message = "Activity started. OperationName = '{0}', Id = '{1}'.", Level = EventLevel.Verbose)]
        public void ActivityStarted(string operationName, string id)
        {
            WriteEvent(24, operationName, id);
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
