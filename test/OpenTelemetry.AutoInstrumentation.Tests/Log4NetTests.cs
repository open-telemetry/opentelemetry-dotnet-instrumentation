// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using FluentAssertions;
using log4net.Core;
using OpenTelemetry.AutoInstrumentation.Instrumentations.Log4Net;
using OpenTelemetry.AutoInstrumentation.Instrumentations.Log4Net.Bridge;
using OpenTelemetry.Trace;
using Xunit;

namespace OpenTelemetry.AutoInstrumentation.Tests;

public class Log4NetTests
{
    // TODO: Remove when Logs Api is made public in non-rc builds.
    private static readonly Type OpenTelemetryLogSeverityType = typeof(Tracer).Assembly.GetType("OpenTelemetry.Logs.LogRecordSeverity")!;

    public static TheoryData<int, int> GetData()
    {
        var theoryData = new TheoryData<int, int>
        {
            { Level.Emergency.Value, GetOpenTelemetrySeverityValue("Fatal") },
            { Level.Fatal.Value, GetOpenTelemetrySeverityValue("Fatal") },
            { Level.Alert.Value, GetOpenTelemetrySeverityValue("Error") },
            { Level.Critical.Value, GetOpenTelemetrySeverityValue("Error") },
            { Level.Severe.Value, GetOpenTelemetrySeverityValue("Error") },
            { Level.Error.Value, GetOpenTelemetrySeverityValue("Error") },
            { Level.Warn.Value, GetOpenTelemetrySeverityValue("Warn") },
            { Level.Notice.Value, GetOpenTelemetrySeverityValue("Info") },
            { Level.Info.Value, GetOpenTelemetrySeverityValue("Info") },
            { Level.Debug.Value, GetOpenTelemetrySeverityValue("Debug") },
            { Level.Trace.Value, GetOpenTelemetrySeverityValue("Trace") },
            { Level.Verbose.Value, GetOpenTelemetrySeverityValue("Trace") }
        };

        return theoryData;
    }

    [Theory]
    [MemberData(nameof(GetData))]
    public void BuiltinLog4NetLevelValues_AreMapped(int log4NetLevelValue, int expectedOpenTelemetrySeverity)
    {
        OpenTelemetryLog4NetAppender.MapLogLevel(log4NetLevelValue).Should().Be(expectedOpenTelemetrySeverity);
    }

    [Theory]
    // LogLevel.Warn(60000) + 10, LogRecordSeverity.Warn (13)
    [InlineData(60010, 13)]
    // LogLevel.Info(40000) + 10, LogRecordSeverity.Info (9)
    [InlineData(40010, 9)]
    // Everything below Debug(30000) threshold is mapped to LogRecordSeverity.Trace
    [InlineData(29900, 1)]
    public void Log4NetLevelValuesWithoutADirectMatch_AreMappedToALessSevereValue(int log4NetLevelValue, int expectedOpenTelemetrySeverity)
    {
        OpenTelemetryLog4NetAppender.MapLogLevel(log4NetLevelValue).Should().Be(expectedOpenTelemetrySeverity);
    }

    private static int GetOpenTelemetrySeverityValue(string val)
    {
        return (int)Enum.Parse(OpenTelemetryLogSeverityType, val);
    }
}
