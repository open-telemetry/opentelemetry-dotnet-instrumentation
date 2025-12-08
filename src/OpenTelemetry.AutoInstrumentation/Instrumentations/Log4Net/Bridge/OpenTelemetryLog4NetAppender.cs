// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using OpenTelemetry.AutoInstrumentation.DuckTyping;
using OpenTelemetry.AutoInstrumentation.Instrumentations.Log4Net.TraceContextInjection;
#if NET
using OpenTelemetry.AutoInstrumentation.Logger;
#endif
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
    private const int LevelOffValue = int.MaxValue;
    private const string SystemStringFormatTypeName = "log4net.Util.SystemStringFormat";

    private static readonly IOtelLogger Logger = OtelLogging.GetLogger();
    private static readonly Lazy<OpenTelemetryLog4NetAppender> InstanceField = new(InitializeAppender, true);

    private readonly Func<string?, object?>? _getLoggerFactory;
    private readonly ConcurrentDictionary<string, object> _loggers = new(StringComparer.Ordinal);

#if NET
    private int _warningLogged;
#endif

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
        if (Sdk.SuppressInstrumentation || loggingEvent.Level.Value == LevelOffValue)
        {
            return;
        }

#if NET
        if (LoggerInitializer.IsInitializedAtLeastOnce)
        {
            if (Interlocked.Exchange(ref _warningLogged, 1) != default)
            {
                return;
            }

            Logger.Warning("Disabling log4net bridge due to ILogger bridge initialization.");
            return;
        }
#endif

        var logger = GetLogger(loggingEvent.LoggerName);

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
        // when *Format methods are used for logging, e.g. InfoFormat
        if (messageObject.TryDuckCast<IStringFormatNew>(out var stringFormat) && stringFormat.Type is { FullName: SystemStringFormatTypeName })
        {
            format = stringFormat.Format;
            args = stringFormat.Args;
        }
        else if (messageObject.TryDuckCast<IStringFormatOld>(out var stringFormatOld) && stringFormatOld.Type is { FullName: SystemStringFormatTypeName })
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

    private static IEnumerable<KeyValuePair<string, object?>>? GetProperties(ILoggingEvent loggingEvent)
    {
        // Due to known issues, attempt to retrieve properties
        // might throw on operating systems other than Windows.
        // This seems to be fixed for versions 2.0.13 and above.
        try
        {
            var properties = loggingEvent.GetProperties();
            return properties == null ? null : GetFilteredProperties(properties);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static IEnumerable<KeyValuePair<string, object?>> GetFilteredProperties(IDictionary properties)
    {
        foreach (var propertyKey in properties.Keys)
        {
            if (propertyKey is not string key)
            {
                continue;
            }

            if (key.StartsWith("log4net:") ||
                key == LogsTraceContextInjectionConstants.SpanIdPropertyName ||
                key == LogsTraceContextInjectionConstants.TraceIdPropertyName ||
                key == LogsTraceContextInjectionConstants.TraceFlagsPropertyName)
            {
                continue;
            }

            yield return new KeyValuePair<string, object?>(key, properties[key]);
        }
    }

    private static Func<string?, object?>? CreateGetLoggerDelegate(LoggerProvider loggerProvider)
    {
        try
        {
            var methodInfo = typeof(LoggerProvider)
                .GetMethod("GetLogger", BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(string) }, null)!;
#if NET
            return methodInfo.CreateDelegate<Func<string?, object?>>(loggerProvider);
#else
            return (Func<string?, object?>)methodInfo.CreateDelegate(typeof(Func<string?, object?>), loggerProvider);
#endif
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

    private object? GetLogger(string? loggerName)
    {
        if (_getLoggerFactory is null)
        {
            return null;
        }

        var name = loggerName ?? string.Empty;
        if (_loggers.TryGetValue(name, out var logger))
        {
            return logger;
        }

        if (_loggers.Count < 100)
        {
            return _loggers.GetOrAdd(name, _getLoggerFactory!);
        }

        return _getLoggerFactory(name);
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
