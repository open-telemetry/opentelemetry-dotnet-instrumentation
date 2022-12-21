// <copyright file="StackExchangeRedisIntegration.cs" company="OpenTelemetry Authors">
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

using System;
using OpenTelemetry.AutoInstrumentation.CallTarget;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.StackExchangeRedis;

/// <summary>
/// StackExchange.Redis.ConnectionMultiplexer calltarget instrumentation
/// </summary>
[InstrumentMethod(// releases 2.0.495 - 2.1.39
    StackExchangeRedisConstants.AssemblyName,
    StackExchangeRedisConstants.ConnectionMultiplexerTypeName,
    StackExchangeRedisConstants.ConnectImplMethodName,
    StackExchangeRedisConstants.ConnectionMultiplexerTypeName,
    new[] { ClrNames.Object, StackExchangeRedisConstants.TextWriterTypeName },
    StackExchangeRedisConstants.MinimumVersion,
    StackExchangeRedisConstants.MaximumVersion,
    StackExchangeRedisConstants.IntegrationName,
    InstrumentationType.Trace)]
[InstrumentMethod(// releases 2.1.50 - 2.5.43
    StackExchangeRedisConstants.AssemblyName,
    StackExchangeRedisConstants.ConnectionMultiplexerTypeName,
    StackExchangeRedisConstants.ConnectImplMethodName,
    StackExchangeRedisConstants.ConnectionMultiplexerTypeName,
    new[] { StackExchangeRedisConstants.ConfigurationOptionsTypeName, StackExchangeRedisConstants.TextWriterTypeName },
    StackExchangeRedisConstants.MinimumVersion,
    StackExchangeRedisConstants.MaximumVersion,
    StackExchangeRedisConstants.IntegrationName,
    InstrumentationType.Trace)]
[InstrumentMethod(// releases 2.5.61 - 2.6.48
    StackExchangeRedisConstants.AssemblyName,
    StackExchangeRedisConstants.ConnectionMultiplexerTypeName,
    StackExchangeRedisConstants.ConnectImplMethodName,
    StackExchangeRedisConstants.ConnectionMultiplexerTypeName,
    new[] { StackExchangeRedisConstants.ConfigurationOptionsTypeName, StackExchangeRedisConstants.TextWriterTypeName, StackExchangeRedisConstants.NullableServerTypeTypeName },
    StackExchangeRedisConstants.MinimumVersion,
    StackExchangeRedisConstants.MaximumVersion,
    StackExchangeRedisConstants.IntegrationName,
    InstrumentationType.Trace)]
[InstrumentMethod(// releases 2.6.66+
    StackExchangeRedisConstants.AssemblyName,
    StackExchangeRedisConstants.ConnectionMultiplexerTypeName,
    StackExchangeRedisConstants.ConnectImplMethodName,
    StackExchangeRedisConstants.ConnectionMultiplexerTypeName,
    new[] { StackExchangeRedisConstants.ConfigurationOptionsTypeName, StackExchangeRedisConstants.TextWriterTypeName, StackExchangeRedisConstants.NullableServerTypeTypeName, StackExchangeRedisConstants.EndPointCollectionTypeName },
    StackExchangeRedisConstants.MinimumVersion,
    StackExchangeRedisConstants.MaximumVersion,
    StackExchangeRedisConstants.IntegrationName,
    InstrumentationType.Trace)]
public static class StackExchangeRedisIntegration
{
    /// <summary>
    /// OnMethodEnd callback
    /// </summary>
    /// <param name="returnValue">Return value</param>
    /// <param name="exception">Exception value</param>
    /// <param name="state">CallTarget state</param>
    /// <typeparam name="TTarget">Type of the target</typeparam>
    /// <typeparam name="TReturn">Return type</typeparam>
    /// <returns>A response value, in an async scenario will be T of Task of T</returns>
    internal static CallTargetReturn<TReturn> OnMethodEnd<TTarget, TReturn>(TReturn returnValue, Exception exception, CallTargetState state)
    {
#if NET6_0_OR_GREATER
        StackExchangeRedisInitializer.Initialize(returnValue);
#endif

        return new CallTargetReturn<TReturn>(returnValue);
    }
}
