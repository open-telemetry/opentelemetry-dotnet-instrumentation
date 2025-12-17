// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using OpenTelemetry.AutoInstrumentation.CallTarget;
using OpenTelemetry.AutoInstrumentation.DuckTyping;
using OpenTelemetry.AutoInstrumentation.Instrumentations.NLog.Bridge;
using OpenTelemetry.AutoInstrumentation.Logging;
#if NET
using OpenTelemetry.AutoInstrumentation.Logger;
#endif

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.NLog.TraceContextInjection.Integrations;

/// <summary>
/// Shared helper for NLog integrations.
/// Provides common functionality for trace context injection and bridge forwarding.
/// </summary>
internal static class NLogIntegrationHelper
{
#if NET
    private static readonly IOtelLogger Logger = OtelLogging.GetLogger();

    private static int _warningLogged;
#endif

    /// <summary>
    /// Handles trace context injection and bridge forwarding for NLog log events.
    /// </summary>
    /// <param name="logEvent">The NLog LogEventInfo being logged.</param>
    /// <returns>A CallTargetState (unused).</returns>
    internal static CallTargetState OnMethodBegin(object logEvent)
    {
        // Duck cast to get properties for trace context injection
        if (logEvent.TryDuckCast<ILogEventInfoProperties>(out var propsEvent))
        {
            var current = Activity.Current;
            if (current != null && propsEvent.Properties != null)
            {
                propsEvent.Properties[LogsTraceContextInjectionConstants.TraceIdPropertyName] = current.TraceId.ToHexString();
                propsEvent.Properties[LogsTraceContextInjectionConstants.SpanIdPropertyName] = current.SpanId.ToHexString();
                propsEvent.Properties[LogsTraceContextInjectionConstants.TraceFlagsPropertyName] = (current.Context.TraceFlags & ActivityTraceFlags.Recorded) != 0 ? "01" : "00";
            }
        }

        // Forward to OpenTelemetry bridge if enabled
#if NET
        if (LoggerInitializer.IsInitializedAtLeastOnce)
        {
            if (Interlocked.Exchange(ref _warningLogged, 1) == default)
            {
                Logger.Warning("Disabling NLog bridge due to ILogger bridge initialization.");
            }

            return CallTargetState.GetDefault();
        }
#endif

        if (Instrumentation.LogSettings.Value.EnableNLogBridge)
        {
            // Duck cast to the full ILoggingEvent struct for the bridge
            if (logEvent.TryDuckCast<ILoggingEvent>(out var duckLogEvent))
            {
                OpenTelemetryNLogConverter.Instance.WriteLogEvent(duckLogEvent);
            }
        }

        return CallTargetState.GetDefault();
    }
}
