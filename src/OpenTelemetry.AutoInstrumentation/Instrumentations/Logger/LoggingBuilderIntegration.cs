// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET6_0_OR_GREATER
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
    minimumVersion: "3.1.0",
    maximumVersion: "8.*.*",
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
            var logBuilderExtensionsType = Type.GetType("OpenTelemetry.AutoInstrumentation.Logger.LogBuilderExtensions, OpenTelemetry.AutoInstrumentation");
            var methodInfo = logBuilderExtensionsType?.GetMethod("AddOpenTelemetryLogsFromIntegration");
            methodInfo?.Invoke(null, new[] { (object)instance });
        }

        return CallTargetReturn.GetDefault();
    }
}
#endif
