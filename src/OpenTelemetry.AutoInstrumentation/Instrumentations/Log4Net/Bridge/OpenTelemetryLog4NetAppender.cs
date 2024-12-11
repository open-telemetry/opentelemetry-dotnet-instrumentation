// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections;
using System.Diagnostics;
using System.Reflection;
using OpenTelemetry.AutoInstrumentation.DuckTyping;
using OpenTelemetry.AutoInstrumentation.Logging;
using OpenTelemetry.Logs;
using Exception = System.Exception;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.Log4Net.Bridge;

internal class OpenTelemetryLog4NetAppender
{
    // Level thresholds, as defined in https://github.com/apache/logging-log4net/blob/2d68abc25dd77a69926b16234510377c9b63acad/src/log4net/Core/Level.cs
    private const int FatalThreshold = 110_000;
    private const int ErrorThreshold = 70_000;
    private const int WarningThreshold = 60_000;
    private const int InfoThreshold = 40_000;
    private const int DebugThreshold = 30_000;

    private static readonly IOtelLogger Logger = OtelLogging.GetLogger();
    private static readonly Lazy<OpenTelemetryLog4NetAppender> InstanceField = new(InitializeAppender, true);

    private readonly Func<string?, object?>? _getLoggerFactory;

    private OpenTelemetryLog4NetAppender(LoggerProvider loggerProvider)
    {
        _getLoggerFactory = CreateGetLoggerDelegate(loggerProvider);
    }

    public static OpenTelemetryLog4NetAppender Instance => InstanceField.Value;

    [DuckReverseMethod]
    public string Name { get; set; } = nameof(OpenTelemetryLog4NetAppender);

    [DuckReverseMethod(ParameterTypeNames = new[] { "log4net.Core.LoggingEvent, log4net" })]
    public void DoAppend(ILoggingEvent loggingEvent)
    {
        if (Sdk.SuppressInstrumentation)
        {
            return;
        }

        object? logger = null;

        if (_getLoggerFactory != null)
        {
            logger = _getLoggerFactory(loggingEvent.LoggerName);
        }

        var logEmitter = OpenTelemetryLogHelpers.LogEmitter;

        if (logEmitter is null || logger is null)
        {
            return;
        }

        var level = loggingEvent.Level;
        var mappedLogLevel = MapLogLevel(level.Value);

        string? format = null;
        string? renderedMessage = null;
        object?[]? args = null;
        var messageObject = loggingEvent.MessageObject;

        // Try to extract message format and args used
        // when *Format overloads are used for logging, e.g. InfoFormat
        if (messageObject.TryDuckCast<IStringFormatNew>(out var stringFormat))
        {
            format = stringFormat.Format;
            args = stringFormat.Args;
        }
        else if (messageObject.TryDuckCast<IStringFormatOld>(out var stringFormatOld))
        {
            format = stringFormatOld.Format;
            args = stringFormatOld.Args;
        }

        // Add rendered message as an attribute only if format was extracted successfully,
        // and addition of rendered message was requested.
        if (format is not null && Instrumentation.LogSettings.Value.IncludeFormattedMessage)
        {
            renderedMessage = loggingEvent.RenderedMessage;
        }

        logEmitter(
            logger,
            format ?? loggingEvent.RenderedMessage,
            loggingEvent.TimeStampUtc,
            loggingEvent.Level.Name,
            mappedLogLevel,
            loggingEvent.ExceptionObject,
            GetProperties(loggingEvent),
            Activity.Current,
            args,
            renderedMessage);
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

    private static Func<string?, object?>? CreateGetLoggerDelegate(LoggerProvider loggerProvider)
    {
        try
        {
            var methodInfo = typeof(LoggerProvider)
                .GetMethod("GetLogger", BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(string) }, null)!;
            return (Func<string?, object?>)methodInfo.CreateDelegate(typeof(Func<string?, object?>), loggerProvider);
        }
        catch (Exception e)
        {
            Logger.Error(e, "Failed to create logger factory delegate.");
            return null;
        }
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
            // Fatal and above -> LogRecordSeverity.Fatal
            >= FatalThreshold => 21,
            // Between Error and Fatal -> LogRecordSeverity.Error
            >= ErrorThreshold => 17,
            // Between Warn and Error -> LogRecordSeverity.Warn
            >= WarningThreshold => 13,
            // Between Info and Warn -> LogRecordSeverity.Info
            >= InfoThreshold => 9,
            // Between Debug and Info -> LogRecordSeverity.Debug
            >= DebugThreshold => 5,
            // Smaller than Debug -> LogRecordSeverity.Trace
            _ => 1
        };
    }
}
