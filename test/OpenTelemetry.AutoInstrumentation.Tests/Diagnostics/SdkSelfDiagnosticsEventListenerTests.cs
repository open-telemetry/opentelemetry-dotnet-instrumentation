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

using System.Diagnostics.Tracing;
using Moq;
using OpenTelemetry.AutoInstrumentation.Diagnostics;
using OpenTelemetry.AutoInstrumentation.Logging;
using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Tests.Diagnostics;

// use collection to indicate that tests should not be run
// in parallel
// see https://xunit.net/docs/running-tests-in-parallel
[Collection("EventEmittingTests")]
public class SdkSelfDiagnosticsEventListenerTests
{
    [Fact]
    public void EventSourceSetup_LowerSeverity()
    {
        var logger = new Mock<IOtelLogger>();
        logger.Setup(otelLogger => otelLogger.Level).Returns(LogLevel.Error);

        using var listener = new SdkSelfDiagnosticsEventListener(logger.Object);

        // Emitting a Verbose event. Or any EventSource event with lower severity than Error.
        AspNetTelemetryEventSourceForTests.Log.ActivityRestored("123");
        OpenTelemetrySdkEventSourceForTests.Log.ActivityStarted("Activity started", "1");

        logger.Verify(otelLogger => otelLogger.Level, Times.Once);
        logger.VerifyNoOtherCalls();
    }

    [Fact]
    public void EventSourceSetup_HigherSeverity()
    {
        var logger = new Mock<IOtelLogger>();
        logger.Setup(otelLogger => otelLogger.Level).Returns(LogLevel.Debug);

        using var listener = new SdkSelfDiagnosticsEventListener(logger.Object);

        // Emitting a Verbose event. Or any EventSource event with lower severity than Error.
        AspNetTelemetryEventSourceForTests.Log.ActivityRestored("123");
        OpenTelemetrySdkEventSourceForTests.Log.ActivityStarted("Activity started", "1");

        logger.Verify(otelLogger => otelLogger.Level, Times.Once);
        logger.Verify(otelLogger => otelLogger.Information("EventSource={0}, Message={1}", "OpenTelemetry-Instrumentation-AspNet-Telemetry-For-Tests", "Activity restored, Id='123'", false), Times.Once);
        logger.Verify(otelLogger => otelLogger.Debug("EventSource={0}, Message={1}", "OpenTelemetry-Sdk-For-Tests", "Activity started. OperationName = 'Activity started', Id = '1'.", false), Times.Once);
        logger.VerifyNoOtherCalls();
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
}
