// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

// Source originated from https://github.com/open-telemetry/opentelemetry-dotnet/blob/23609730ddd73c860553de847e67c9b2226cff94/test/OpenTelemetry.Tests/Internal/SelfDiagnosticsEventListenerTest.cs

using System.Diagnostics.Tracing;
using FluentAssertions;
using FluentAssertions.Execution;
using OpenTelemetry.AutoInstrumentation.Diagnostics;
using OpenTelemetry.AutoInstrumentation.Logging;
using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Tests.Diagnostics;

// use collection to indicate that tests should not be run
// in parallel
// see https://xunit.net/docs/running-tests-in-parallel
[Collection("Non-Parallel Collection")]
public class SdkSelfDiagnosticsEventListenerTests
{
    [Fact]
    public void EventSourceSetup_LowerSeverity()
    {
        var testSink = new TestSink();
        var logger = new InternalLogger(testSink, LogLevel.Error);
        using var listener = new SdkSelfDiagnosticsEventListener(logger);

        AspNetTelemetryEventSourceForTests.Log.Information();
        AspNetTelemetryEventSourceForTests.Log.Warning();
        OpenTelemetrySdkEventSourceForTests.Log.Verbose();

        // Events with both Critical and Error EventLevel are logged as errors.
        OpenTelemetrySdkEventSourceForTests.Log.Error();
        OpenTelemetrySdkEventSourceForTests.Log.Critical();

        using (new AssertionScope())
        {
            testSink.Messages.Count.Should().Be(2);
            testSink.Messages.Should().OnlyContain(msg => msg.Contains("Error"));
        }
    }

    [Fact]
    public void EventSourceSetup_HigherSeverity()
    {
        var testSink = new TestSink();
        var logger = new InternalLogger(testSink, LogLevel.Debug);
        using var listener = new SdkSelfDiagnosticsEventListener(logger);

        AspNetTelemetryEventSourceForTests.Log.Information();
        AspNetTelemetryEventSourceForTests.Log.Warning();
        OpenTelemetrySdkEventSourceForTests.Log.Verbose();
        OpenTelemetrySdkEventSourceForTests.Log.Error();

        using (new AssertionScope())
        {
            testSink.Messages.Count.Should().Be(4);
            testSink.Messages.Should().Contain(msg => msg.Contains("Error"));
            testSink.Messages.Should().Contain(msg => msg.Contains("Warning"));
            testSink.Messages.Should().Contain(msg => msg.Contains("Information"));
            testSink.Messages.Should().Contain(msg => msg.Contains("Debug"));
        }
    }

    [EventSource(Name = "OpenTelemetry-Instrumentation-AspNet-Telemetry-For-Tests")]
    internal sealed class AspNetTelemetryEventSourceForTests : EventSource
    {
        public static readonly AspNetTelemetryEventSourceForTests Log = new();

        private AspNetTelemetryEventSourceForTests()
        {
        }

        [Event(4, Message = "Activity info.", Level = EventLevel.Informational)]
        public void Information()
        {
            WriteEvent(4);
        }

        [Event(5, Message = "Activity warning.", Level = EventLevel.Warning)]
        public void Warning()
        {
            WriteEvent(5);
        }
    }

    [EventSource(Name = "OpenTelemetry-Sdk-For-Tests")]
    internal class OpenTelemetrySdkEventSourceForTests : EventSource
    {
        public static readonly OpenTelemetrySdkEventSourceForTests Log = new();

        private OpenTelemetrySdkEventSourceForTests()
        {
        }

        [Event(24, Message = "Activity verbose.", Level = EventLevel.Verbose)]
        public void Verbose()
        {
            WriteEvent(24);
        }

        [Event(25, Message = "Activity error.", Level = EventLevel.Error)]
        public void Error()
        {
            WriteEvent(25);
        }

        [Event(26, Message = "Activity critical.", Level = EventLevel.Critical)]
        public void Critical()
        {
            WriteEvent(26);
        }
    }
}
