// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections;
using System.Diagnostics;
using OpenTelemetry.AutoInstrumentation.CallTarget;
using OpenTelemetry.AutoInstrumentation.DuckTyping;
using OpenTelemetry.AutoInstrumentation.Instrumentations.NLog.Bridge;
using OpenTelemetry.AutoInstrumentation.Instrumentations.NLog.TraceContextInjection;
using OpenTelemetry.AutoInstrumentation.Logging;
#if NET
using OpenTelemetry.AutoInstrumentation.Logger;
#endif

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.NLog.Bridge.Integrations;

/// <summary>
/// NLog Logger integration that hooks into the actual logging process.
/// This integration intercepts NLog's Logger.Log method calls to automatically
/// capture log events and forward them to OpenTelemetry when the NLog bridge is enabled.
///
/// The integration targets NLog.Logger.Log method which is the core method called
/// for all logging operations, allowing us to capture events without modifying configuration.
/// </summary>
[InstrumentMethod(
assemblyName: "NLog",
typeName: "NLog.Logger",
methodName: "Log",
returnTypeName: ClrNames.Void,
parameterTypeNames: new[] { "NLog.LogEventInfo" },
minimumVersion: "5.0.0",
maximumVersion: "6.*.*",
integrationName: "NLog",
type: InstrumentationType.Log)]
public static class LoggerIntegration
{
#if NET
    private static readonly IOtelLogger Logger = OtelLogging.GetLogger();
    private static int _warningLogged;
#endif

    /// <summary>
    /// Intercepts NLog's Logger.Log method calls to capture log events.
    /// This method is called before the original Log method executes,
    /// allowing us to inject trace context and forward log events to OpenTelemetry.
    /// </summary>
    /// <typeparam name="TTarget">The type of the logger instance.</typeparam>
    /// <param name="instance">The NLog Logger instance.</param>
    /// <param name="logEvent">The NLog LogEventInfo being logged.</param>
    /// <returns>A CallTargetState (unused in this case).</returns>
    internal static CallTargetState OnMethodBegin<TTarget>(TTarget instance, object logEvent)
    {
        Logger.Debug($"NLog LoggerIntegration.OnMethodBegin called! LogEvent: {logEvent.GetType().Name}");

        // Always inject trace context into NLog properties for all sinks to use
        // This allows NLog's own targets (file, console, etc.) to access trace context
        TryInjectTraceContext(logEvent);

#if NET
        // Check if ILogger bridge has been initialized and warn if so
        // This prevents conflicts between different logging bridges
        if (LoggerInitializer.IsInitializedAtLeastOnce)
        {
            if (Interlocked.Exchange(ref _warningLogged, 1) != default)
            {
                return CallTargetState.GetDefault();
            }

            Logger.Warning("Disabling NLog bridge due to ILogger bridge initialization.");
            return CallTargetState.GetDefault();
        }
#endif

        Logger.Debug($"NLog bridge enabled: {Instrumentation.LogSettings.Value.EnableNLogBridge}");

        // Only forward to OpenTelemetry if the NLog bridge is enabled
        if (Instrumentation.LogSettings.Value.EnableNLogBridge)
        {
            Logger.Debug("Forwarding log event to OpenTelemetryNLogConverter");
            // Convert the object to our duck-typed struct
            if (logEvent.TryDuckCast<ILoggingEvent>(out var duckLogEvent))
            {
                // Forward the log event to the OpenTelemetry converter
                OpenTelemetryNLogConverter.Instance.WriteLogEvent(duckLogEvent);
            }
            else
            {
                Logger.Debug($"Failed to duck cast logEvent of type {logEvent.GetType().Name} to ILoggingEvent");
            }
        }

        // Return default state - we don't need to track anything between begin/end
        return CallTargetState.GetDefault();
    }

    /// <summary>
    /// Injects OpenTelemetry trace context into NLog's LogEventInfo properties.
    /// This allows NLog's own targets (file, console, database, etc.) to access
    /// trace context even when the OpenTelemetry bridge is disabled.
    /// </summary>
    /// <param name="logEvent">The NLog LogEventInfo object.</param>
    private static void TryInjectTraceContext(object logEvent)
    {
        try
        {
            var activity = Activity.Current;
            if (activity == null)
            {
                return;
            }

            // Duck cast to access Properties collection
            if (!logEvent.TryDuckCast<ILoggingEvent>(out var duckLogEvent))
            {
                return;
            }

            // Get the Properties object
            var properties = duckLogEvent.Properties;
            if (properties == null)
            {
                return;
            }

            // Try to cast to IDictionary to add trace context properties
            if (properties is IDictionary dict)
            {
                dict[LogsTraceContextInjectionConstants.TraceIdPropertyName] = activity.TraceId.ToString();
                dict[LogsTraceContextInjectionConstants.SpanIdPropertyName] = activity.SpanId.ToString();
                dict[LogsTraceContextInjectionConstants.TraceFlagsPropertyName] = activity.ActivityTraceFlags.ToString();
            }
            else if (properties.TryDuckCast<IDictionary>(out var duckDict))
            {
                duckDict[LogsTraceContextInjectionConstants.TraceIdPropertyName] = activity.TraceId.ToString();
                duckDict[LogsTraceContextInjectionConstants.SpanIdPropertyName] = activity.SpanId.ToString();
                duckDict[LogsTraceContextInjectionConstants.TraceFlagsPropertyName] = activity.ActivityTraceFlags.ToString();
            }
        }
        catch (Exception ex)
        {
            Logger.Debug($"Failed to inject trace context into NLog properties: {ex.Message}");
        }
    }
}
