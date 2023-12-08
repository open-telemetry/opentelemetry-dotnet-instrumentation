// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET6_0_OR_GREATER

using OpenTelemetry.AutoInstrumentation.CallTarget;
using OpenTelemetry.AutoInstrumentation.Configurations;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.StackExchangeRedis;

/// <summary>
/// StackExchange.Redis.ConnectionMultiplexer calltarget instrumentation
/// </summary>
[InstrumentMethod(// releases 2.0.495 - 2.1.39
    assemblyName: StackExchangeRedisConstants.AssemblyName,
    typeName: StackExchangeRedisConstants.ConnectionMultiplexerTypeName,
    methodName: StackExchangeRedisConstants.ConnectImplAsyncMethodName,
    returnTypeName: StackExchangeRedisConstants.TaskConnectionMultiplexerTypeName,
    parameterTypeNames: new[] { ClrNames.Object, StackExchangeRedisConstants.TextWriterTypeName },
    minimumVersion: StackExchangeRedisConstants.MinimumVersion,
    maximumVersion: StackExchangeRedisConstants.MaximumVersion,
    integrationName: StackExchangeRedisConstants.IntegrationName,
    type: InstrumentationType.Trace)]
[InstrumentMethod(// releases 2.1.50 - 2.5.43
    assemblyName: StackExchangeRedisConstants.AssemblyName,
    typeName: StackExchangeRedisConstants.ConnectionMultiplexerTypeName,
    methodName: StackExchangeRedisConstants.ConnectImplAsyncMethodName,
    returnTypeName: StackExchangeRedisConstants.TaskConnectionMultiplexerTypeName,
    parameterTypeNames: new[] { StackExchangeRedisConstants.ConfigurationOptionsTypeName, StackExchangeRedisConstants.TextWriterTypeName },
    minimumVersion: StackExchangeRedisConstants.MinimumVersion,
    maximumVersion: StackExchangeRedisConstants.MaximumVersion,
    integrationName: StackExchangeRedisConstants.IntegrationName,
    type: InstrumentationType.Trace)]
[InstrumentMethod(// releases 2.5.61+
    assemblyName: StackExchangeRedisConstants.AssemblyName,
    typeName: StackExchangeRedisConstants.ConnectionMultiplexerTypeName,
    methodName: StackExchangeRedisConstants.ConnectImplAsyncMethodName,
    returnTypeName: StackExchangeRedisConstants.TaskConnectionMultiplexerTypeName,
    parameterTypeNames: new[] { StackExchangeRedisConstants.ConfigurationOptionsTypeName, StackExchangeRedisConstants.TextWriterTypeName, StackExchangeRedisConstants.NullableServerTypeTypeName },
    minimumVersion: StackExchangeRedisConstants.MinimumVersion,
    maximumVersion: StackExchangeRedisConstants.MaximumVersion,
    integrationName: StackExchangeRedisConstants.IntegrationName,
    type: InstrumentationType.Trace)]
public static class StackExchangeRedisIntegrationAsync
{
    /// <summary>
    /// OnAsyncMethodEnd callback
    /// </summary>
    /// <param name="instance">Instance value, aka `this` of the instrumented method.</param>
    /// <param name="returnValue">Return value</param>
    /// <param name="exception">Exception value</param>
    /// <param name="state">CallTarget state</param>
    /// <typeparam name="TTarget">Type of the target</typeparam>
    /// <typeparam name="TReturn">Return type</typeparam>
    /// <returns>A response value, in an async scenario will be T of Task of T</returns>
    internal static TReturn OnAsyncMethodEnd<TTarget, TReturn>(TTarget instance, TReturn returnValue, Exception exception, in CallTargetState state)
    {
        if (returnValue != null)
        {
            StackExchangeRedisInitializer.Initialize(returnValue);
        }

        return returnValue;
    }
}
#endif
