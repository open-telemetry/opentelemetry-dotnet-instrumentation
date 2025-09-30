// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using OpenTelemetry.AutoInstrumentation.Configurations;
using OpenTelemetry.AutoInstrumentation.DuckTyping;
using OpenTelemetry.AutoInstrumentation.Instrumentations.NLog.Bridge;
using OpenTelemetry.Logs;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.NLog;

/// <summary>
/// OpenTelemetry Target for NLog using duck typing to avoid direct NLog assembly references.
/// This target is designed to be used through auto-injection and duck-typed proxies.
/// </summary>
internal sealed class OpenTelemetryTarget
{
    private static readonly ConcurrentDictionary<string, object> LoggerCache = new(StringComparer.Ordinal);

    private static LoggerProvider? _loggerProvider;
    private static Func<string?, object?>? _getLoggerFactory;

    [DuckReverseMethod]
    public string Name { get; set; } = nameof(OpenTelemetryTarget);

    [DuckReverseMethod]
    public void InitializeTarget()
    {
        if (_loggerProvider != null)
        {
            return;
        }

        var createLoggerProviderBuilderMethod = typeof(Sdk).GetMethod("CreateLoggerProviderBuilder", BindingFlags.Static | BindingFlags.NonPublic)!;
        var loggerProviderBuilder = (LoggerProviderBuilder)createLoggerProviderBuilderMethod.Invoke(null, null)!;

        loggerProviderBuilder = loggerProviderBuilder
            .SetResourceBuilder(ResourceConfigurator.CreateResourceBuilder(Instrumentation.ResourceSettings.Value));

        loggerProviderBuilder = loggerProviderBuilder.AddOtlpExporter();

        _loggerProvider = loggerProviderBuilder.Build();
        _getLoggerFactory = CreateGetLoggerDelegate(_loggerProvider);
    }

    [DuckReverseMethod(ParameterTypeNames = new[] { "NLog.LogEventInfo, NLog" })]
    public void Write(ILoggingEvent? logEvent)
    {
        if (logEvent is null || _loggerProvider is null)
        {
            return;
        }

        if (Sdk.SuppressInstrumentation)
        {
            return;
        }

        var logger = GetOrCreateLogger(logEvent.LoggerName);
        if (logger is null)
        {
            return;
        }

        var properties = GetLogEventProperties(logEvent);

        // Use formatted message if available, otherwise use raw message
        var body = logEvent.FormattedMessage ?? logEvent.Message?.ToString();

        var severityText = logEvent.Level.Name;
        var severityNumber = MapLogLevelToSeverity(logEvent.Level.Ordinal);

        // Use Activity.Current for trace context
        var current = Activity.Current;

        // Include event parameters if available
        var args = logEvent.Parameters is object[] p ? p : null;

        OpenTelemetryLogHelpers.LogEmitter?.Invoke(
            logger,
            body,
            logEvent.TimeStamp,
            severityText,
            severityNumber,
            logEvent.Exception,
            properties,
            current,
            args,
            logEvent.FormattedMessage);
    }

    private static int MapLogLevelToSeverity(int levelOrdinal)
    {
        // Map NLog ordinals 0..5 to OTEL severity 1..24 approximate buckets
        return levelOrdinal switch
        {
            0 => 1,   // Trace
            1 => 5,   // Debug
            2 => 9,   // Info
            3 => 13,  // Warn
            4 => 17,  // Error
            5 => 21,  // Fatal
            _ => 9
        };
    }

    private static Func<string?, object?>? CreateGetLoggerDelegate(LoggerProvider loggerProvider)
    {
        try
        {
            var methodInfo = typeof(LoggerProvider)
                .GetMethod("GetLogger", BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(string) }, null)!;
            return (Func<string?, object?>)methodInfo.CreateDelegate(typeof(Func<string?, object?>), loggerProvider);
        }
        catch
        {
            return null;
        }
    }

    private static IEnumerable<KeyValuePair<string, object?>>? GetLogEventProperties(ILoggingEvent logEvent)
    {
        try
        {
            var properties = logEvent.GetProperties();
            return properties == null ? null : GetFilteredProperties(properties);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static IEnumerable<KeyValuePair<string, object?>> GetFilteredProperties(System.Collections.IDictionary properties)
    {
        foreach (var propertyKey in properties.Keys)
        {
            if (propertyKey is not string key)
            {
                continue;
            }

            if (key.StartsWith("NLog.") ||
                key.StartsWith("nlog:") ||
                key == TraceContextInjection.LogsTraceContextInjectionConstants.SpanIdPropertyName ||
                key == TraceContextInjection.LogsTraceContextInjectionConstants.TraceIdPropertyName ||
                key == TraceContextInjection.LogsTraceContextInjectionConstants.TraceFlagsPropertyName)
            {
                continue;
            }

            yield return new KeyValuePair<string, object?>(key, properties[key]);
        }
    }

    private object? GetOrCreateLogger(string? loggerName)
    {
        var key = loggerName ?? string.Empty;
        if (LoggerCache.TryGetValue(key, out var logger))
        {
            return logger;
        }

        var factory = _getLoggerFactory;
        if (factory is null)
        {
            return null;
        }

        logger = factory(loggerName);
        if (logger is not null)
        {
            LoggerCache[key] = logger;
        }

        return logger;
    }
}
