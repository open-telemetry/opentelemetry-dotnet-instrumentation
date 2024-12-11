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
    parameterTypeNames: new[] { "log4net.Core.LoggingEvent" },
    minimumVersion: "2.0.0",
    maximumVersion: "3.*.*",
    integrationName: "Log4Net",
    type: InstrumentationType.Log)]
public static class AppenderAttachedImplIntegration
{
    internal static CallTargetState OnMethodBegin<TTarget, TLoggingEvent>(TTarget instance, TLoggingEvent loggingEvent)
    where TLoggingEvent : ILoggingEvent
    {
        if (Activity.Current == null || loggingEvent.Properties == null)
        {
            return CallTargetState.GetDefault();
        }

        loggingEvent.Properties[LogsTraceContextInjectionConstants.SpanIdPropertyName] = Activity.Current.SpanId.ToHexString();
        loggingEvent.Properties[LogsTraceContextInjectionConstants.TraceIdPropertyName] = Activity.Current.TraceId.ToHexString();
        loggingEvent.Properties[LogsTraceContextInjectionConstants.TraceFlagsPropertyName] = (int)Activity.Current.ActivityTraceFlags;
        return CallTargetState.GetDefault();
    }
}
