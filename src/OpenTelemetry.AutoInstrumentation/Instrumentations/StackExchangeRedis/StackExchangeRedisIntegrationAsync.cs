// <copyright file="StackExchangeRedisIntegrationAsync.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

using OpenTelemetry.AutoInstrumentation.CallTarget;
using OpenTelemetry.AutoInstrumentation.Configuration;

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
    internal static TReturn OnAsyncMethodEnd<TTarget, TReturn>(TTarget instance, TReturn returnValue, Exception exception, CallTargetState state)
    {
#if NET6_0_OR_GREATER
        if (returnValue != null && Instrumentation.TracerSettings.Value.EnabledInstrumentations.Contains(TracerInstrumentation.StackExchangeRedis))
        {
                StackExchangeRedisInitializer.Initialize(returnValue);
        }
#endif

        return returnValue;
    }
}
