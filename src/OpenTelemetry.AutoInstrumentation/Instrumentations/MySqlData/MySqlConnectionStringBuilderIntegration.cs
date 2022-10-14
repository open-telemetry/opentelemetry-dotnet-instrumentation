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

#if !NETFRAMEWORK

using System;
using OpenTelemetry.AutoInstrumentation.CallTarget;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.MySqlData;

/// <summary>
/// Instrumentation targeting the test application used to validate the strong name scenario.
/// When an actual bytecode instrumentation targeting a strong named assembly on .NET Framework
/// is added we can remove this instrumentation.
/// </summary>
[InstrumentMethod(
    AssemblyName = "MySql.Data",
    TypeName = "MySql.Data.MySqlClient.MySqlConnectionStringBuilder",
    MethodName = "get_Logging",
    ReturnTypeName = ClrNames.Bool,
    ParameterTypeNames = new string[0],
    MinimumVersion = "8.0.31",
    MaximumVersion = "8.65535.65535",
    IntegrationName = "MySqlData")]
public class MySqlConnectionStringBuilderIntegration
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
        where TTarget : struct
    {
        var alwaysReturnTrue = (TReturn)(object)true;

        return new CallTargetReturn<TReturn>(alwaysReturnTrue);
    }
}
#endif
