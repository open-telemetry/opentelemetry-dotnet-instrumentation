// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.CallTarget;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.NLog.TraceContextInjection.Integrations;

/// <summary>
/// NLog integration for NLog 5.x (with TargetWithFilterChain).
/// This integration intercepts NLog's internal WriteToTargets method to:
/// 1. Inject trace context (TraceId, SpanId, TraceFlags) into the LogEventInfo properties
/// 2. Forward log events to OpenTelemetry when the bridge is enabled
/// </summary>
/// <remarks>
/// NLog 5.x has assembly version 5.0.0.0 regardless of the NuGet package version.
/// Early NLog 5.x versions (5.0.0 - 5.2.x) use the concrete TargetWithFilterChain class.
/// Both this integration and WriteToTargetsIntegration are registered for
/// NLog 5.x to handle both the interface and concrete class variants.
/// The native profiler will match the correct integration based on the actual
/// method signature at runtime.
/// </remarks>
[InstrumentMethod(
    assemblyName: "NLog",
    typeName: "NLog.Logger",
    methodName: "WriteToTargets",
    returnTypeName: ClrNames.Void,
    parameterTypeNames: ["NLog.LogEventInfo", "NLog.Internal.TargetWithFilterChain"],
    minimumVersion: "5.0.0",
    maximumVersion: "5.*.*",
    integrationName: "NLog",
    type: InstrumentationType.Log)]
public static class WriteToTargetsLegacyIntegration
{
    /// <summary>
    /// Intercepts NLog's WriteToTargets method to inject trace context and forward to OpenTelemetry.
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
