// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.CallTarget;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.NLog.TraceContextInjection.Integrations;

/// <summary>
/// NLog integration for NLog 6.x.
/// This integration intercepts NLog's internal WriteLogEventToTargets method to:
/// 1. Inject trace context (TraceId, SpanId, TraceFlags) into the LogEventInfo properties
/// 2. Forward log events to OpenTelemetry when the bridge is enabled
/// </summary>
/// <remarks>
/// NLog 6.x renamed the WriteToTargets method to WriteLogEventToTargets.
/// </remarks>
[InstrumentMethod(
    assemblyName: "NLog",
    typeName: "NLog.Logger",
    methodName: "WriteLogEventToTargets",
    returnTypeName: ClrNames.Void,
    parameterTypeNames: new[] { "NLog.LogEventInfo", "NLog.Internal.ITargetWithFilterChain" },
    minimumVersion: "6.0.0",
    maximumVersion: "6.*.*",
    integrationName: "NLog",
    type: InstrumentationType.Log)]
public static class WriteLogEventToTargetsIntegration
{
    /// <summary>
    /// Intercepts NLog's WriteLogEventToTargets method to inject trace context and forward to OpenTelemetry.
    /// </summary>
    /// <typeparam name="TTarget">The type of the logger instance.</typeparam>
    /// <param name="instance">The NLog Logger instance.</param>
    /// <param name="logEvent">The NLog LogEventInfo being logged.</param>
    /// <param name="targetsForLevel">The target filter chain.</param>
    /// <returns>A CallTargetState (unused in this case).</returns>
    internal static CallTargetState OnMethodBegin<TTarget>(TTarget instance, object logEvent, object targetsForLevel)
    {
        return NLogIntegrationHelper.OnMethodBegin(logEvent);
    }
}
