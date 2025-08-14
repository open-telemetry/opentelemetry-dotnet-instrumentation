// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using OpenTelemetry.AutoInstrumentation.DuckTyping;
using OpenTelemetry.AutoInstrumentation.Instrumentations.NLog.TraceContextInjection;
#if NET
using OpenTelemetry.AutoInstrumentation.Logger;
#endif
using OpenTelemetry.AutoInstrumentation.Logging;
using OpenTelemetry.Logs;
using Exception = System.Exception;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.NLog.Bridge;

/// <summary>
/// OpenTelemetry NLog Target implementation.
/// This class serves as a bridge between NLog logging framework and OpenTelemetry logging.
/// It captures NLog log events and converts them to OpenTelemetry log records.
///
/// The target integrates with NLog's architecture by implementing the target pattern,
/// allowing it to receive log events and forward them to OpenTelemetry for processing.
/// </summary>
internal class OpenTelemetryNLogTarget
{
    // NLog level ordinals as defined in NLog.LogLevel
    // https://github.com/NLog/NLog/blob/master/src/NLog/LogLevel.cs
    private const int TraceOrdinal = 0;
    private const int DebugOrdinal = 1;
    private const int InfoOrdinal = 2;
    private const int WarnOrdinal = 3;
    private const int ErrorOrdinal = 4;
    private const int FatalOrdinal = 5;
    private const int OffOrdinal = 6;

    private static readonly IOtelLogger Logger = OtelLogging.GetLogger();
    private static readonly Lazy<OpenTelemetryNLogTarget> InstanceField = new(InitializeTarget, true);

    private readonly Func<string?, object?>? _getLoggerFactory;
    private readonly ConcurrentDictionary<string, object> _loggers = new(StringComparer.Ordinal);

#if NET
    private int _warningLogged;
#endif

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenTelemetryNLogTarget"/> class.
    /// </summary>
    /// <param name="loggerProvider">The OpenTelemetry logger provider to use for creating loggers.</param>
    private OpenTelemetryNLogTarget(LoggerProvider loggerProvider)
    {
        _getLoggerFactory = CreateGetLoggerDelegate(loggerProvider);
    }

    /// <summary>
    /// Gets the singleton instance of the OpenTelemetry NLog target.
    /// </summary>
    public static OpenTelemetryNLogTarget Instance => InstanceField.Value;

    /// <summary>
    /// Gets or sets the name of the target.
    /// This property is used by NLog's duck typing system to identify the target.
    /// </summary>
    [DuckReverseMethod]
    public string Name { get; set; } = nameof(OpenTelemetryNLogTarget);

    /// <summary>
    /// Processes a log event from NLog and converts it to an OpenTelemetry log record.
    /// This method is called by NLog for each log event that should be processed by this target.
    /// </summary>
    /// <param name="loggingEvent">The NLog log event to process.</param>
    [DuckReverseMethod(ParameterTypeNames = new[] { "NLog.LogEventInfo, NLog" })]
    public void WriteLogEvent(ILoggingEvent loggingEvent)
    {
        // Skip processing if instrumentation is suppressed or logging is disabled
        if (Sdk.SuppressInstrumentation || loggingEvent.Level.Ordinal == OffOrdinal)
        {
            return;
        }

#if NET
        // Check if ILogger bridge has been initialized and warn if so
        // This prevents conflicts between different logging bridges
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

        // Get the OpenTelemetry logger for this NLog logger name
        var logger = GetLogger(loggingEvent.LoggerName);

        // Get the log emitter function for creating OpenTelemetry log records
        var logEmitter = OpenTelemetryLogHelpers.LogEmitter;

        if (logEmitter is null || logger is null)
        {
            return;
        }

        var level = loggingEvent.Level;
        var mappedLogLevel = MapLogLevel(level.Ordinal);

        string? messageTemplate = null;
        string? formattedMessage = null;
        object?[]? parameters = null;
        var messageObject = loggingEvent.Message;

        // Extract message template and parameters for structured logging
        // NLog supports structured logging through message templates
        if (loggingEvent.Parameters is { Length: > 0 })
        {
            messageTemplate = messageObject?.ToString();
            parameters = loggingEvent.Parameters;
        }

        // Add formatted message as an attribute if we have a message template
        // and the configuration requests inclusion of formatted messages
        if (messageTemplate is not null && Instrumentation.LogSettings.Value.IncludeFormattedMessage)
        {
            formattedMessage = loggingEvent.FormattedMessage;
        }

        // Create the OpenTelemetry log record using the log emitter
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

    /// <summary>
    /// Closes the target and releases any resources.
    /// This method is called by NLog when the target is being shut down.
    /// </summary>
    [DuckReverseMethod]
    public void Close()
    {
        // No specific cleanup needed for this implementation
    }

    /// <summary>
    /// Maps NLog log level ordinals to OpenTelemetry log record severity levels.
    /// </summary>
    /// <param name="levelOrdinal">The NLog level ordinal value.</param>
    /// <returns>The corresponding OpenTelemetry log record severity level.</returns>
    internal static int MapLogLevel(int levelOrdinal)
    {
        return levelOrdinal switch
        {
            // Fatal -> LogRecordSeverity.Fatal (21)
            FatalOrdinal => 21,
            // Error -> LogRecordSeverity.Error (17)
            ErrorOrdinal => 17,
            // Warn -> LogRecordSeverity.Warn (13)
            WarnOrdinal => 13,
            // Info -> LogRecordSeverity.Info (9)
            InfoOrdinal => 9,
            // Debug -> LogRecordSeverity.Debug (5)
            DebugOrdinal => 5,
            // Trace -> LogRecordSeverity.Trace (1)
            TraceOrdinal => 1,
            // Off or unknown -> LogRecordSeverity.Trace (1)
            _ => 1
        };
    }

    /// <summary>
    /// Extracts properties from the NLog log event for inclusion in the OpenTelemetry log record.
    /// This method safely retrieves custom properties while filtering out internal NLog properties
    /// and trace context properties that are handled separately.
    /// </summary>
    /// <param name="loggingEvent">The NLog log event.</param>
    /// <returns>A collection of key-value pairs representing the event properties, or null if retrieval fails.</returns>
    private static IEnumerable<KeyValuePair<string, object?>>? GetProperties(ILoggingEvent loggingEvent)
    {
        try
        {
            var properties = loggingEvent.GetProperties();
            return properties == null ? null : GetFilteredProperties(properties);
        }
        catch (Exception)
        {
            // Property retrieval can fail in some scenarios, particularly with certain NLog configurations
            // Return null to indicate that properties are not available
            return null;
        }
    }

    /// <summary>
    /// Filters the properties collection to exclude internal NLog properties and trace context properties.
    /// This ensures that only user-defined properties are included in the OpenTelemetry log record.
    /// </summary>
    /// <param name="properties">The properties collection from the NLog event.</param>
    /// <returns>A filtered collection of properties suitable for OpenTelemetry log records.</returns>
    private static IEnumerable<KeyValuePair<string, object?>> GetFilteredProperties(IDictionary properties)
    {
        foreach (var propertyKey in properties.Keys)
        {
            if (propertyKey is not string key)
            {
                continue;
            }

            // Filter out internal NLog properties and trace context properties
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

    /// <summary>
    /// Creates a delegate function for getting OpenTelemetry loggers from the logger provider.
    /// This uses reflection to access the internal GetLogger method on the LoggerProvider.
    /// </summary>
    /// <param name="loggerProvider">The OpenTelemetry logger provider.</param>
    /// <returns>A function that can create loggers by name, or null if creation fails.</returns>
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

    /// <summary>
    /// Initializes the OpenTelemetry NLog target with the current instrumentation logger provider.
    /// </summary>
    /// <returns>A new instance of the OpenTelemetry NLog target.</returns>
    private static OpenTelemetryNLogTarget InitializeTarget()
    {
        return new OpenTelemetryNLogTarget(Instrumentation.LoggerProvider!);
    }

    /// <summary>
    /// Gets or creates an OpenTelemetry logger for the specified logger name.
    /// This method implements caching to avoid creating duplicate loggers for the same name.
    /// </summary>
    /// <param name="loggerName">The name of the logger to retrieve.</param>
    /// <returns>The OpenTelemetry logger instance, or null if creation fails.</returns>
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

        // Limit the cache size to prevent memory leaks with many dynamic logger names
        if (_loggers.Count < 100)
        {
            return _loggers.GetOrAdd(name, _getLoggerFactory!);
        }

        // If cache is full, create logger without caching
        return _getLoggerFactory(name);
    }
}
