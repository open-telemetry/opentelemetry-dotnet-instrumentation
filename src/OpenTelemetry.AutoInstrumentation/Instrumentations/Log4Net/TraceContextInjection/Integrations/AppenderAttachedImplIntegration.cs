// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using OpenTelemetry.AutoInstrumentation.CallTarget;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.Log4Net.TraceContextInjection.Integrations;

/// <summary>
/// Log4Net AppenderAttachedImplIntegration integration.
/// </summary>
[InstrumentMethod(
    assemblyName: "log4net",
    typeName: "log4net.Util.AppenderAttachedImpl",
    methodName: "AppendLoopOnAppenders",
    returnTypeName: ClrNames.Int32,
    parameterTypeNames: ["log4net.Core.LoggingEvent"],
    minimumVersion: "2.0.13",
    maximumVersion: "3.*.*",
    integrationName: "Log4Net",
    type: InstrumentationType.Log)]
public static class AppenderAttachedImplIntegration
{
    internal static CallTargetState OnMethodBegin<TTarget, TLoggingEvent>(TTarget instance, TLoggingEvent loggingEvent)
    where TLoggingEvent : ILoggingEvent
    {
        var current = Activity.Current;
        if (current == null || loggingEvent.Properties == null)
        {
            return CallTargetState.GetDefault();
        }

        loggingEvent.Properties[LogsTraceContextInjectionConstants.SpanIdPropertyName] = current.SpanId.ToHexString();
        loggingEvent.Properties[LogsTraceContextInjectionConstants.TraceIdPropertyName] = current.TraceId.ToHexString();
        loggingEvent.Properties[LogsTraceContextInjectionConstants.TraceFlagsPropertyName] = (current.Context.TraceFlags & ActivityTraceFlags.Recorded) != 0 ? "01" : "00";
        return CallTargetState.GetDefault();
    }
}
