// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using NLog;
using NLog.Config;
using NLog.Targets;
using OpenTelemetry;
using OpenTelemetry.AutoInstrumentation;
using OpenTelemetry.AutoInstrumentation.Configurations;
using OpenTelemetry.Logs;

namespace OpenTelemetry.AutoInstrumentation.NLogTarget;

[Target("OpenTelemetryTarget")]
public sealed class OpenTelemetryTarget : TargetWithContext
{
    private static readonly ConcurrentDictionary<string, object> LoggerCache = new(StringComparer.Ordinal);
    private static LoggerProvider? _loggerProvider;
    private static Func<string?, object?>? _getLoggerFactory;

    public OpenTelemetryTarget()
    {
        Layout = "${message}";
    }

    [RequiredParameter]
    public string? Endpoint { get; set; }

    public string? Headers { get; set; }

    public bool UseHttp { get; set; } = true;

    public string? ServiceName { get; set; }

    [ArrayParameter(typeof(TargetPropertyWithContext), "attribute")]
    public IList<TargetPropertyWithContext> Attributes { get; } = new List<TargetPropertyWithContext>();

    [ArrayParameter(typeof(TargetPropertyWithContext), "resource")]
    public IList<TargetPropertyWithContext> Resources { get; } = new List<TargetPropertyWithContext>();

    public bool IncludeFormattedMessage { get; set; } = true;

    public new bool IncludeEventProperties { get; set; } = true;

    public new bool IncludeScopeProperties { get; set; } = true;

    public bool IncludeEventParameters { get; set; } = true;

    public int ScheduledDelayMilliseconds { get; set; } = 5000;

    public int MaxQueueSize { get; set; } = 2048;

    public int MaxExportBatchSize { get; set; } = 512;

    protected override void InitializeTarget()
    {
        base.InitializeTarget();

        if (_loggerProvider != null)
        {
            return;
        }

        var createLoggerProviderBuilderMethod = typeof(Sdk).GetMethod("CreateLoggerProviderBuilder", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!;
        var loggerProviderBuilder = (LoggerProviderBuilder)createLoggerProviderBuilderMethod.Invoke(null, null)!;

        loggerProviderBuilder = loggerProviderBuilder
            .SetResourceBuilder(ResourceConfigurator.CreateResourceBuilder(Instrumentation.GeneralSettings.Value.EnabledResourceDetectors));

        loggerProviderBuilder = loggerProviderBuilder.AddOtlpExporter(options =>
        {
            var endpoint = Endpoint;
            if (string.IsNullOrEmpty(endpoint))
            {
                endpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");
            }

            if (!string.IsNullOrEmpty(endpoint))
            {
                options.Endpoint = new Uri(endpoint!, UriKind.RelativeOrAbsolute);
            }

            if (!string.IsNullOrEmpty(Headers))
            {
                options.Headers = Headers;
            }

            options.Protocol = UseHttp ? OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf : OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
            options.BatchExportProcessorOptions.ScheduledDelayMilliseconds = ScheduledDelayMilliseconds;
            options.BatchExportProcessorOptions.MaxQueueSize = MaxQueueSize;
            options.BatchExportProcessorOptions.MaxExportBatchSize = MaxExportBatchSize;
        });

        _loggerProvider = loggerProviderBuilder.Build();
        _getLoggerFactory = CreateGetLoggerDelegate(_loggerProvider);
    }

    protected override void Write(LogEventInfo logEvent)
    {
        if (_loggerProvider is null)
        {
            return;
        }

        if (Sdk.SuppressInstrumentation)
        {
            return;
        }

        var logger = GetOrCreateLogger(logEvent.LoggerName);

        // Build properties from event properties and context
        var properties = new List<KeyValuePair<string, object?>>();
        if (IncludeEventProperties && logEvent.HasProperties && logEvent.Properties is not null)
        {
            foreach (var kvp in logEvent.Properties)
            {
                properties.Add(new KeyValuePair<string, object?>(Convert.ToString(kvp.Key)!, kvp.Value));
            }
        }

        // Scope properties can be added via explicit <attribute> entries or NLog's contexts (GDC/MDLC)
        foreach (var attribute in Attributes)
        {
            var value = attribute.Layout?.Render(logEvent);
            if (!string.IsNullOrEmpty(attribute.Name))
            {
                properties.Add(new KeyValuePair<string, object?>(attribute.Name!, value));
            }
        }

        var body = IncludeFormattedMessage ? logEvent.FormattedMessage : Convert.ToString(logEvent.Message);

        var severityText = logEvent.Level.Name;
        var severityNumber = MapLogLevelToSeverity(logEvent.Level);

        var current = Activity.Current;

        // Emit using internal helpers via reflection delegate
        var renderedMessage = logEvent.FormattedMessage;
        var args = IncludeEventParameters && logEvent.Parameters is object[] p ? p : null;

        OpenTelemetry.AutoInstrumentation.Instrumentations.NLog.Bridge.OpenTelemetryLogHelpers.LogEmitter?.Invoke(
            logger,
            body,
            logEvent.TimeStamp,
            severityText,
            severityNumber,
            logEvent.Exception,
            properties,
            current,
            args,
            renderedMessage);
    }

    private static int MapLogLevelToSeverity(LogLevel level)
    {
        // Map NLog ordinals 0..5 to OTEL severity 1..24 approximate buckets
        return level.Ordinal switch
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
                .GetMethod("GetLogger", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance, null, new[] { typeof(string) }, null)!;
            return (Func<string?, object?>)methodInfo.CreateDelegate(typeof(Func<string?, object?>), loggerProvider);
        }
        catch
        {
            return null;
        }
    }

    private object GetOrCreateLogger(string? loggerName)
    {
        var key = loggerName ?? string.Empty;
        if (LoggerCache.TryGetValue(key, out var logger))
        {
            return logger;
        }

        var factory = _getLoggerFactory;
        if (factory is null)
        {
            return new object();
        }

        logger = factory(loggerName);
        if (logger is not null)
        {
            LoggerCache[key] = logger;
        }

        return logger ?? new object();
    }
}
