// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.CallTarget;
using OpenTelemetry.AutoInstrumentation.Logging;
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
parameterTypeNames: [],
minimumVersion: "2.0.13",
maximumVersion: "3.*.*",
integrationName: "Log4Net",
type: InstrumentationType.Log)]
public static class AppenderCollectionIntegration
{
#if NET
    private static readonly IOtelLogger Logger = OtelLogging.GetLogger();
    private static int _warningLogged;
#endif

    internal static CallTargetReturn<TReturn> OnMethodEnd<TTarget, TReturn>(TTarget instance, TReturn returnValue, Exception exception, in CallTargetState state)
    {
#if NET
        if (LoggerInitializer.IsInitializedAtLeastOnce)
        {
            if (Interlocked.Exchange(ref _warningLogged, 1) != default)
            {
                return new CallTargetReturn<TReturn>(returnValue);
            }

            Logger.Warning("Disabling addition of log4net bridge due to ILogger bridge initialization.");
            return new CallTargetReturn<TReturn>(returnValue);
        }
#endif
        if (Instrumentation.LogSettings.Value.EnableLog4NetBridge && returnValue is Array responseArray)
        {
            var finalArray = OpenTelemetryAppenderInitializer<TReturn>.Initialize(responseArray);
            return new CallTargetReturn<TReturn>(finalArray);
        }

        return new CallTargetReturn<TReturn>(returnValue);
    }
}
