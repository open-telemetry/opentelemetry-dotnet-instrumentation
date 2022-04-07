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

using System;
using System.Diagnostics.Tracing;
using System.IO;
using FluentAssertions;
using OpenTelemetry.AutoInstrumentation.Diagnostics;
using Xunit;

public class SdkSelfDiagnosticsEventListenerTests
{
    [Fact]
    public void EventSourceSetup_LowerSeverity()
    {
        var originalConsoleOut = Console.Out; // preserve the original stream
        using var writer = new StringWriter();
        var listener = new SdkSelfDiagnosticsEventListener(EventLevel.Error);

        // Emitting a Verbose event. Or any EventSource event with lower severity than Error.
        AspNetTelemetryEventSource.Log.ActivityRestored("123");
        OpenTelemetrySdkEventSource.Log.ActivityStarted("Activity started", "1");

        // Prepare the output for assertion
        writer.Flush();
        var outputString = writer.GetStringBuilder().ToString();
        outputString.Should().NotContain("EventSource=OpenTelemetry-Instrumentation-AspNet-Telemetry, Message=Activity restored, Id='123'");
        outputString.Should().NotContain("EventSource=OpenTelemetry-Sdk, Message=Activity started.");

        // Cleanup
        Console.SetOut(originalConsoleOut);
    }

    [Fact]
    public void EventSourceSetup_HigherSeverity()
    {
        // Redirect the ConsoleLogger
        var originalConsoleOut = Console.Out;
        using var writer = new StringWriter();
        Console.SetOut(writer);
        var listener = new SdkSelfDiagnosticsEventListener(EventLevel.Verbose);

        // Emitting a Verbose event. Or any EventSource event with lower severity than Error.
        AspNetTelemetryEventSource.Log.ActivityRestored("123");
        OpenTelemetrySdkEventSource.Log.ActivityStarted("Activity started", "1");

        // Prepare the output for assertion
        writer.Flush();
        var outputString = writer.GetStringBuilder().ToString();
        outputString.Should().Contain("EventSource=OpenTelemetry-Instrumentation-AspNet-Telemetry, Message=Activity restored, Id='123'");
        outputString.Should().Contain("EventSource=OpenTelemetry-Sdk, Message=Activity started.");

        // Cleanup
        Console.SetOut(originalConsoleOut);
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
}
