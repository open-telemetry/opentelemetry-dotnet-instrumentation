// <copyright file="MySqlDataInstrumentationConstructorIntegration.cs" company="OpenTelemetry Authors">
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

#if NET6_0_OR_GREATER

using OpenTelemetry.AutoInstrumentation.CallTarget;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.MySqlData;

/// <summary>
/// MySql.Data.MySqlClient.MySqlCommand.ExecuteReaderAsyncIntegration calltarget instrumentation
/// </summary>
[InstrumentMethod(
    assemblyName: "OpenTelemetry.Instrumentation.MySqlData",
    typeName: "OpenTelemetry.Instrumentation.MySqlData.MySqlDataInstrumentation",
    methodName: ".ctor",
    returnTypeName: ClrNames.Void,
    parameterTypeNames: new string[] { "OpenTelemetry.Instrumentation.MySqlData.MySqlDataInstrumentationOptions" },
    minimumVersion: "1.0.0",
    maximumVersion: "1.65535.65535",
    integrationName: "MySqlData",
    type: InstrumentationType.Trace)]
public static class MySqlDataInstrumentationConstructorIntegration
{
    /// <summary>
    /// OnMethodEnd callback
    /// </summary>
    /// <param name="instance">Instance value, aka `this` of the instrumented method.</param>
    /// <param name="exception">Exception value</param>
    /// <param name="state">CallTarget state</param>
    /// <typeparam name="TTarget">Type of the target</typeparam>
    /// <returns>A response value, in an async scenario will be T of Task of T</returns>
    internal static CallTargetReturn OnMethodEnd<TTarget>(TTarget instance, Exception exception, in CallTargetState state)
        where TTarget : IMySqlDataInstrumentation
    {
        MySqlDataCommon.MySqlDataInstrumentationOptions = instance.Options;
        return CallTargetReturn.GetDefault();
    }
}
#endif
