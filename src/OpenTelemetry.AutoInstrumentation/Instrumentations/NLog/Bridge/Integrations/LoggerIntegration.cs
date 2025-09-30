// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using System.Reflection;
using OpenTelemetry.AutoInstrumentation.CallTarget;
using OpenTelemetry.AutoInstrumentation.Instrumentations.NLog.AutoInjection;
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
minimumVersion: "4.0.0",
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
    /// allowing us to capture and forward log events to OpenTelemetry.
    /// </summary>
    /// <typeparam name="TTarget">The type of the logger instance.</typeparam>
    /// <param name="instance">The NLog Logger instance.</param>
    /// <param name="logEvent">The NLog LogEventInfo being logged.</param>
    /// <returns>A CallTargetState (unused in this case).</returns>
    internal static CallTargetState OnMethodBegin<TTarget>(TTarget instance, ILoggingEvent logEvent)
    {
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

        // Only process the log event if the NLog bridge is enabled
        if (Instrumentation.LogSettings.Value.EnableNLogBridge)
        {
            // Ensure the OpenTelemetry NLog target is configured (zero-config path)
            NLogAutoInjector.EnsureConfigured();

            // Inject trace context into NLog GlobalDiagnosticsContext for current destination outputs
            TrySetTraceContext(Activity.Current);
        }

        // Return default state - we don't need to track anything between begin/end
        return CallTargetState.GetDefault();
    }

    private static void TrySetTraceContext(Activity? activity)
    {
        try
        {
            var gdcType = Type.GetType("NLog.GlobalDiagnosticsContext, NLog");
            if (gdcType is null)
            {
                return;
            }

            var setMethod = gdcType.GetMethod("Set", BindingFlags.Public | BindingFlags.Static, null, [typeof(string), typeof(string)], null);
            if (setMethod is null)
            {
                return;
            }

            string spanId = activity?.SpanId.ToString() ?? "(null)";
            string traceId = activity?.TraceId.ToString() ?? "(null)";
            string traceFlags = activity is null ? "(null)" : ((byte)activity.ActivityTraceFlags).ToString("x2");

            setMethod.Invoke(null, new object[] { "span_id", spanId });
            setMethod.Invoke(null, new object[] { "trace_id", traceId });
            setMethod.Invoke(null, new object[] { "trace_flags", traceFlags });
        }
        catch
        {
            // best-effort only
        }
    }
}
