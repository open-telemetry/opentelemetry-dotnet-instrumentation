// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections;
using System.Diagnostics;
using System.Reflection;
using OpenTelemetry.AutoInstrumentation.DuckTyping;
using OpenTelemetry.Logs;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.Log4Net;

internal class OpenTelemetryLog4NetAppender
{
    private static readonly Lazy<OpenTelemetryLog4NetAppender> InstanceField = new(InitializeAppender, true);

    private readonly object? _logger;

    private OpenTelemetryLog4NetAppender(LoggerProvider loggerProvider)
    {
        _logger = GetLogger(loggerProvider);
    }

    public static OpenTelemetryLog4NetAppender Instance => InstanceField.Value;

    [DuckReverseMethod]
    public string Name { get; set; } = nameof(OpenTelemetryLog4NetAppender);

    [DuckReverseMethod(ParameterTypeNames = new[] { "log4net.Core.LoggingEvent" })]
    public void DoAppend(ILoggingEvent loggingEvent)
    {
        LoggingLevel level = loggingEvent.Level;
        var mappedLogLevel = MapLogLevel(level.Value);

        OpenTelemetryLogHelpers.LogEmitter(
            _logger!,
            loggingEvent.RenderedMessage,
            loggingEvent.TimeStampUtc,
            loggingEvent.Level.Name,
            mappedLogLevel,
            loggingEvent.ExceptionObject,
            GetProperties(loggingEvent),
            Activity.Current);
    }

    [DuckReverseMethod]
    public void Close()
    {
    }

    private static IDictionary? GetProperties(ILoggingEvent loggingEvent)
    {
        // Due to known issues, attempt to retrieve properties
        // might throw on operating systems other than Windows.
        // This seems to be fixed for versions 2.0.13 and above.
        try
        {
            return loggingEvent.GetProperties();
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static object GetLogger(LoggerProvider loggerProvider)
    {
        return typeof(LoggerProvider).GetMethod("GetLogger", BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null)!.Invoke(loggerProvider, null)!;
    }

    private static OpenTelemetryLog4NetAppender InitializeAppender()
    {
        return new OpenTelemetryLog4NetAppender(Instrumentation.LoggerProvider!);
    }

#pragma warning disable SA1202
    internal static int MapLogLevel(int levelValue)
#pragma warning restore SA1202
    {
        return levelValue switch
        {
            // EMERGENCY -> FATAL2
            >= 120_000 => 22,
            // FATAL -> FATAL
            >= 110_000 => 21,
            // ALERT -> ERROR4
            >= 100_000 => 20,
            // CRITICAL -> ERROR3
            >= 90_000 => 19,
            // SEVERE -> ERROR2
            >= 80_000 => 18,
            // ERROR -> ERROR
            >= 70_000 => 17,
            // WARN -> WARN
            >= 60_000 => 13,
            // NOTICE -> INFO2
            >= 50_000 => 10,
            // INFO -> INFO
            >= 40_000 => 9,
            // FINE / DEBUG -> DEBUG
            >= 30_000 => 5,
            // FINER / TRACE -> TRACE2
            >= 20_000 => 2,
            // FINEST / VERBOSE -> TRACE
            >= 10_000 => 1,
            _ => 0
        };
    }
}
