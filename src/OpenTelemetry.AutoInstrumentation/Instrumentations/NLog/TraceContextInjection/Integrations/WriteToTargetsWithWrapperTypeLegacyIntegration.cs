// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.CallTarget;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.NLog.TraceContextInjection.Integrations;

/// <summary>
/// NLog integration for NLog 5.x (with wrapperType parameter and TargetWithFilterChain).
/// This integration intercepts NLog's internal WriteToTargets method to:
/// 1. Inject trace context (TraceId, SpanId, TraceFlags) into the LogEventInfo properties
/// 2. Forward log events to OpenTelemetry when the bridge is enabled
/// </summary>
/// <remarks>
/// This overload is called when using Logger.Log(Type wrapperType, LogEventInfo logEvent).
/// NLog 5.x has assembly version 5.0.0.0 regardless of the NuGet package version.
/// Early NLog 5.x versions (5.0.0 - 5.2.x) use the concrete TargetWithFilterChain class.
/// </remarks>
[InstrumentMethod(
    assemblyName: "NLog",
    typeName: "NLog.Logger",
    methodName: "WriteToTargets",
    returnTypeName: ClrNames.Void,
    parameterTypeNames: new[] { ClrNames.Type, "NLog.LogEventInfo", "NLog.Internal.TargetWithFilterChain" },
    minimumVersion: "5.0.0",
    maximumVersion: "5.*.*",
    integrationName: "NLog",
    type: InstrumentationType.Log)]
public static class WriteToTargetsWithWrapperTypeLegacyIntegration
{
    /// <summary>
    /// Intercepts NLog's WriteToTargets method (with wrapperType) to inject trace context and forward to OpenTelemetry.
    /// </summary>
    /// <typeparam name="TTarget">The type of the logger instance.</typeparam>
    /// <param name="instance">The NLog Logger instance.</param>
    /// <param name="wrapperType">The wrapper type parameter.</param>
    /// <param name="logEvent">The NLog LogEventInfo being logged.</param>
    /// <param name="targetsForLevel">The target filter chain.</param>
    /// <returns>A CallTargetState (unused in this case).</returns>
    internal static CallTargetState OnMethodBegin<TTarget>(TTarget instance, object? wrapperType, object logEvent, object targetsForLevel)
    {
        return NLogIntegrationHelper.OnMethodBegin(logEvent);
    }
}
