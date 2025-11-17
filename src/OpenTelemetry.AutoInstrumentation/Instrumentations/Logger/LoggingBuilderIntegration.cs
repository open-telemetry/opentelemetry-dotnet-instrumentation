// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET
using OpenTelemetry.AutoInstrumentation.CallTarget;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.Logger;

/// <summary>
/// Microsoft.Extensions.Logging.LoggingBuilder calltarget instrumentation
/// </summary>
[InstrumentMethod(
    assemblyName: "Microsoft.Extensions.Logging",
    typeName: "Microsoft.Extensions.Logging.LoggingBuilder",
    methodName: ".ctor",
    returnTypeName: ClrNames.Void,
    parameterTypeNames: new[] { "Microsoft.Extensions.DependencyInjection.IServiceCollection" },
    minimumVersion: "8.0.0",
    maximumVersion: "10.*.*",
    integrationName: "ILogger",
    type: InstrumentationType.Log)]
public static class LoggingBuilderIntegration
{
    /// <summary>
    /// OnMethodEnd callback
    /// </summary>
    /// <typeparam name="TTarget">Type of the target</typeparam>
    /// <param name="instance">Instance value, aka `this` of the instrumented method.</param>
    /// <param name="exception">Exception instance in case the original code threw an exception.</param>
    /// <param name="state">Calltarget state value</param>
    /// <returns>A default CallTargetReturn to satisfy the CallTarget contract</returns>
    internal static CallTargetReturn OnMethodEnd<TTarget>(TTarget instance, Exception exception, in CallTargetState state)
    {
        if (instance is not null)
        {
            var loggerInitializer = Type.GetType("OpenTelemetry.AutoInstrumentation.Logger.LoggerInitializer, OpenTelemetry.AutoInstrumentation");
            var methodInfo = loggerInitializer?.GetMethod("AddOpenTelemetryLogsFromIntegration");
            methodInfo?.Invoke(null, [instance]);
        }

        return CallTargetReturn.GetDefault();
    }
}
#endif
