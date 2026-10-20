// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using OpenTelemetry.Logs;
using Log4NetLogHelpers = OpenTelemetry.AutoInstrumentation.Instrumentations.Log4Net.Bridge.OpenTelemetryLogHelpers;
using NLogLogHelpers = OpenTelemetry.AutoInstrumentation.Instrumentations.NLog.Bridge.OpenTelemetryLogHelpers;

namespace OpenTelemetry.AutoInstrumentation.Tests;

public class OpenTelemetryLogHelpersTests
{
    [Theory]
    [InlineData(typeof(Log4NetLogHelpers))]
    [InlineData(typeof(NLogLogHelpers))]
    internal void BuildLogRecord_SetsObservedTimestampToUtcNow(Type logHelpersType)
    {
        var logRecordDataType = typeof(LoggerProvider).Assembly.GetType("OpenTelemetry.Logs.LogRecordData")!;
        var severityType = typeof(LoggerProvider).Assembly.GetType("OpenTelemetry.Logs.LogRecordSeverity")!;

        var body = Expression.Parameter(typeof(string), "body");
        var timestamp = Expression.Parameter(typeof(DateTime), "timestamp");
        var severityText = Expression.Parameter(typeof(string), "severityText");
        var severityLevel = Expression.Parameter(typeof(int), "severityLevel");
        var activity = Expression.Parameter(typeof(Activity), "activity");

        var buildLogRecord = logHelpersType.GetMethod("BuildLogRecord", BindingFlags.Static | BindingFlags.NonPublic)!;
        var logRecordExpression = (BlockExpression)buildLogRecord.Invoke(
            null,
            [logRecordDataType, severityType, body, timestamp, severityText, severityLevel, activity])!;
        var factory = Expression.Lambda<Func<string, DateTime, string, int, Activity?, object>>(
                Expression.Convert(logRecordExpression, typeof(object)),
                body,
                timestamp,
                severityText,
                severityLevel,
                activity)
            .Compile();

        var logTimestamp = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        var beforeEmit = DateTime.UtcNow;

        var logRecord = factory("body", logTimestamp, "Info", 9, null);

        var afterEmit = DateTime.UtcNow;
        var actualTimestamp = (DateTime)logRecordDataType.GetProperty("Timestamp")!.GetValue(logRecord)!;
        var observedTimestamp = (DateTime)logRecordDataType.GetProperty("ObservedTimestamp")!.GetValue(logRecord)!;

        Assert.Equal(logTimestamp, actualTimestamp);
        Assert.InRange(observedTimestamp, beforeEmit, afterEmit);
        Assert.Equal(DateTimeKind.Utc, observedTimestamp.Kind);
    }
}
