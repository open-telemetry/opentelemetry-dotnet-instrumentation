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

#if NETCOREAPP3_1_OR_GREATER

using System;
using OpenTelemetry.AutoInstrumentation.CallTarget;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.StackExchangeRedis
{
    /// <summary>
    /// StackExchange.Redis.ConnectionMultiplexer calltarget instrumentation
    /// </summary>
    [InstrumentMethod(
        AssemblyName = StackExchangeRedisConstants.AssemblyName,
        TypeName = StackExchangeRedisConstants.ConnectionMultiplexerTypeName,
        MethodName = StackExchangeRedisConstants.ConnectMethodName,
        ReturnTypeName = StackExchangeRedisConstants.ConnectionMultiplexerTypeName,
        ParameterTypeNames = new[] { StackExchangeRedisConstants.ConfigurationOptionsTypeName, StackExchangeRedisConstants.TextWriterTypeName },
        MinimumVersion = StackExchangeRedisConstants.MinimumVersion,
        MaximumVersion = StackExchangeRedisConstants.MaximumVersion,
        IntegrationName = StackExchangeRedisConstants.IntegrationName)]
    public class StackExchangeRedisIntegration
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
        public static CallTargetReturn<TReturn> OnMethodEnd<TTarget, TReturn>(TReturn returnValue, Exception exception, CallTargetState state)
        {
            StackExchangeRedisInitializer.Initialize(returnValue);

            return new CallTargetReturn<TReturn>(returnValue);
        }
    }
}
#endif
