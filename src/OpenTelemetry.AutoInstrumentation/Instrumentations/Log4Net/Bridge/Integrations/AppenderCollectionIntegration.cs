// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.CallTarget;
#if NET
using OpenTelemetry.AutoInstrumentation.Logger;
#endif

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.Log4Net.Bridge.Integrations;

/// <summary>
/// Log4Net AppenderCollection integration.
/// </summary>
[InstrumentMethod(
assemblyName: "log4net",
typeName: "log4net.Appender.AppenderCollection",
methodName: "ToArray",
returnTypeName: "log4net.Appender.IAppender[]",
parameterTypeNames: new string[0],
minimumVersion: "2.0.13",
maximumVersion: "3.*.*",
integrationName: "Log4Net",
type: InstrumentationType.Log)]
public static class AppenderCollectionIntegration
{
    internal static CallTargetReturn<TReturn> OnMethodEnd<TTarget, TReturn>(TTarget instance, TReturn returnValue, Exception exception, in CallTargetState state)
    {
        if (
            Instrumentation.LogSettings.Value.EnableLog4NetBridge &&
            #if NET
#pragma warning disable SA1003
            !LoggerInitializer.IsInitializedAtLeastOnce &&
#pragma warning restore SA1003
#endif
            returnValue is Array responseArray)
        {
            var finalArray = OpenTelemetryAppenderInitializer<TReturn>.Initialize(responseArray);
            return new CallTargetReturn<TReturn>(finalArray);
        }

        return new CallTargetReturn<TReturn>(returnValue);
    }
}
