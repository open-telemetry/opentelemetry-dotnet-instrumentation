// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.CallTarget;
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
        if (Instrumentation.LogSettings.Value.EnableNLogBridge && logEvent != null)
        {
            // Forward the log event to OpenTelemetry using our target
            OpenTelemetryNLogTarget.Instance.WriteLogEvent(logEvent);
        }

        // Return default state - we don't need to track anything between begin/end
        return CallTargetState.GetDefault();
    }
}
