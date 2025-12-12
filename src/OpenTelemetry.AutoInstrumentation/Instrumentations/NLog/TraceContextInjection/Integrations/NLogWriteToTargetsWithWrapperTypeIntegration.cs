// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.CallTarget;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.NLog.TraceContextInjection.Integrations;

/// <summary>
/// NLog integration for WriteToTargets/WriteLogEventToTargets methods (3-parameter overloads with wrapperType).
/// This integration intercepts NLog's internal methods to:
/// 1. Inject trace context (TraceId, SpanId, TraceFlags) into the LogEventInfo properties
/// 2. Forward log events to OpenTelemetry when the bridge is enabled
/// </summary>
/// <remarks>
/// Covers methods called when using Logger.Log(Type wrapperType, LogEventInfo logEvent):
/// - NLog 5.x WriteToTargets with ITargetWithFilterChain (5.3.0+)
/// - NLog 5.x WriteToTargets with TargetWithFilterChain (5.0.0-5.2.x)
/// - NLog 6.x WriteLogEventToTargets with ITargetWithFilterChain
/// The native profiler will match the correct signature at runtime.
/// </remarks>
[InstrumentMethod(
    assemblyName: "NLog",
    typeName: "NLog.Logger",
    methodName: "WriteToTargets",
    returnTypeName: ClrNames.Void,
    parameterTypeNames: new[] { "System.Type", "NLog.LogEventInfo", "NLog.Internal.ITargetWithFilterChain" },
    minimumVersion: "5.0.0",
    maximumVersion: "5.*.*",
    integrationName: "NLog",
    type: InstrumentationType.Log)]
[InstrumentMethod(
    assemblyName: "NLog",
    typeName: "NLog.Logger",
    methodName: "WriteToTargets",
    returnTypeName: ClrNames.Void,
    parameterTypeNames: new[] { "System.Type", "NLog.LogEventInfo", "NLog.Internal.TargetWithFilterChain" },
    minimumVersion: "5.0.0",
    maximumVersion: "5.*.*",
    integrationName: "NLog",
    type: InstrumentationType.Log)]
[InstrumentMethod(
    assemblyName: "NLog",
    typeName: "NLog.Logger",
    methodName: "WriteLogEventToTargets",
    returnTypeName: ClrNames.Void,
    parameterTypeNames: new[] { "System.Type", "NLog.LogEventInfo", "NLog.Internal.ITargetWithFilterChain" },
    minimumVersion: "6.0.0",
    maximumVersion: "6.*.*",
    integrationName: "NLog",
    type: InstrumentationType.Log)]
public static class NLogWriteToTargetsWithWrapperTypeIntegration
{
    /// <summary>
    /// Intercepts NLog's WriteToTargets/WriteLogEventToTargets method (with wrapperType) to inject trace context and forward to OpenTelemetry.
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
