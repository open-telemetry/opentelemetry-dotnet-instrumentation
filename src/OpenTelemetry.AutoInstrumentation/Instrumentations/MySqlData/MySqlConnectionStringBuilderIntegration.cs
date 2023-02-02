// <copyright file="MySqlConnectionStringBuilderIntegration.cs" company="OpenTelemetry Authors">
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

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.MySqlData;

/// <summary>
/// MySql.Data.MySqlClient.MySqlConnectionStringBuilder calltarget instrumentation
/// </summary>
[InstrumentMethod(
    assemblyName: "MySql.Data",
    typeName: "MySql.Data.MySqlClient.MySqlConnectionStringBuilder",
    methodName: "get_Logging",
    returnTypeName: ClrNames.Bool,
    parameterTypeNames: new string[0],
    minimumVersion: "8.0.31",
    maximumVersion: "8.65535.65535",
    integrationName: "MySqlData",
    type: InstrumentationType.Trace)]
public static class MySqlConnectionStringBuilderIntegration
{
#if NET6_0_OR_GREATER
    private static readonly object TrueAsObject = true;
#endif

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
        where TTarget : struct
    {
#if NET6_0_OR_GREATER
        var alwaysReturnTrue = (TReturn)TrueAsObject;

        return new CallTargetReturn<TReturn>(alwaysReturnTrue);
#else
        return new CallTargetReturn<TReturn>(returnValue);
#endif
    }
}
