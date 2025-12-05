// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using OpenTelemetry.AutoInstrumentation.DuckTyping;
using OpenTelemetry.AutoInstrumentation.Instrumentations.NLog.TraceContextInjection;
using OpenTelemetry.AutoInstrumentation.Logging;
#if NET
using OpenTelemetry.AutoInstrumentation.Logger;  // Only needed for LoggerInitializer
#endif
using OpenTelemetry.Logs;
using Exception = System.Exception;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.NLog.Bridge;

/// <summary>
/// Converts NLog LogEventInfo into OpenTelemetry LogRecords.
/// </summary>
internal class OpenTelemetryNLogConverter
{
    private const int TraceOrdinal = 0;
    private const int DebugOrdinal = 1;
    private const int InfoOrdinal = 2;
    private const int WarnOrdinal = 3;
    private const int ErrorOrdinal = 4;
    private const int FatalOrdinal = 5;
    private const int OffOrdinal = 6;

    private static readonly IOtelLogger Logger = OtelLogging.GetLogger();
    private static readonly Lazy<OpenTelemetryNLogConverter> InstanceField = new(InitializeTarget, true);

    private readonly Func<string?, object?>? _getLoggerFactory;
    private readonly ConcurrentDictionary<string, object> _loggers = new(StringComparer.Ordinal);

#if NET
    private int _warningLogged;
#endif

    private OpenTelemetryNLogConverter(LoggerProvider loggerProvider)
    {
        _getLoggerFactory = CreateGetLoggerDelegate(loggerProvider);
    }

    public static OpenTelemetryNLogConverter Instance => InstanceField.Value;

    [DuckReverseMethod]
    public string Name { get; set; } = nameof(OpenTelemetryNLogConverter);

    [DuckReverseMethod(ParameterTypeNames = ["NLog.LogEventInfo, NLog"])]
    public void WriteLogEvent(ILoggingEvent loggingEvent)
    {
        if (Sdk.SuppressInstrumentation || loggingEvent.Level.Ordinal == OffOrdinal)
        {
            return;
        }

        var emitter = OpenTelemetryLogHelpers.LogEmitter;
        if (emitter == null)
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

            Logger.Warning("Disabling NLog bridge due to ILogger bridge initialization.");
            return;
        }
#endif

        var logger = GetLogger(loggingEvent.LoggerName);
        var logEmitter = OpenTelemetryLogHelpers.LogEmitter;
        if (logEmitter is null || logger is null)
        {
            return;
        }

        var mappedLogLevel = MapLogLevel(loggingEvent.Level.Ordinal);

        string? messageTemplate = null;
        string? formattedMessage = null;
        object?[]? parameters = null;
        var messageObject = loggingEvent.Message;
        if (loggingEvent.Parameters is { Length: > 0 })
        {
            messageTemplate = messageObject?.ToString();
            parameters = loggingEvent.Parameters;
        }

        if (messageTemplate is not null && Instrumentation.LogSettings.Value.IncludeFormattedMessage)
        {
            formattedMessage = loggingEvent.FormattedMessage;
        }

        logEmitter(
            logger,
            messageTemplate ?? loggingEvent.FormattedMessage,
            loggingEvent.TimeStamp,
            loggingEvent.Level.Name,
            mappedLogLevel,
            loggingEvent.Exception,
            GetProperties(loggingEvent),
            Activity.Current,
            parameters,
            formattedMessage);
    }

    internal static int MapLogLevel(int levelOrdinal)
    {
        return levelOrdinal switch
        {
            FatalOrdinal => 21,
            ErrorOrdinal => 17,
            WarnOrdinal => 13,
            InfoOrdinal => 9,
            DebugOrdinal => 5,
            TraceOrdinal => 1,
            _ => 1
        };
    }

    private static IEnumerable<KeyValuePair<string, object?>>? GetProperties(ILoggingEvent loggingEvent)
    {
        var result = new List<KeyValuePair<string, object?>>();

        try
        {
            var properties = loggingEvent.Properties;
            if (properties != null)
            {
                // Try to cast to IDictionary first, if that fails, try IEnumerable<KeyValuePair>
                if (properties is IDictionary dict)
                {
                    foreach (var prop in GetFilteredProperties(dict))
                    {
                        result.Add(prop);
                    }
                }
                else if (properties is IEnumerable<KeyValuePair<string, object?>> enumerable)
                {
                    foreach (var prop in GetFilteredProperties(enumerable))
                    {
                        result.Add(prop);
                    }
                }
                else
                {
                    // If it's some other type, try to duck cast it
                    if (properties.TryDuckCast<IDictionary>(out var duckDict))
                    {
                        foreach (var prop in GetFilteredProperties(duckDict))
                        {
                            result.Add(prop);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Debug($"Failed to get event properties: {ex.Message}");
        }

        return result.Count > 0 ? result : null;
    }

    private static IEnumerable<KeyValuePair<string, object?>> GetFilteredProperties(IDictionary properties)
    {
        foreach (var propertyKey in properties.Keys)
        {
            if (propertyKey is not string key)
            {
                continue;
            }

            if (key.StartsWith("NLog.") ||
                key.StartsWith("nlog:") ||
                key == LogsTraceContextInjectionConstants.SpanIdPropertyName ||
                key == LogsTraceContextInjectionConstants.TraceIdPropertyName ||
                key == LogsTraceContextInjectionConstants.TraceFlagsPropertyName)
            {
                continue;
            }

            yield return new KeyValuePair<string, object?>(key, properties[key]);
        }
    }

    private static IEnumerable<KeyValuePair<string, object?>> GetFilteredProperties(IEnumerable<KeyValuePair<string, object?>> properties)
    {
        foreach (var property in properties)
        {
            var key = property.Key;
            if (key.StartsWith("NLog.") ||
                key.StartsWith("nlog:") ||
                key == LogsTraceContextInjectionConstants.SpanIdPropertyName ||
                key == LogsTraceContextInjectionConstants.TraceIdPropertyName ||
                key == LogsTraceContextInjectionConstants.TraceFlagsPropertyName)
            {
                continue;
            }

            yield return property;
        }
    }

    private static Func<string?, object?>? CreateGetLoggerDelegate(LoggerProvider loggerProvider)
    {
        try
        {
            var methodInfo = typeof(LoggerProvider)
                .GetMethod("GetLogger", BindingFlags.NonPublic | BindingFlags.Instance, null, [typeof(string)], null)!;
            return (Func<string?, object?>)methodInfo.CreateDelegate(typeof(Func<string?, object?>), loggerProvider);
        }
        catch (Exception e)
        {
            Logger.Error(e, "Failed to create logger factory delegate.");
            return null;
        }
    }

    private static OpenTelemetryNLogConverter InitializeTarget()
    {
        return new OpenTelemetryNLogConverter(Instrumentation.LoggerProvider!);
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
}
