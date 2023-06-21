// <copyright file="ExecuteReaderAsyncIntegration.cs" company="OpenTelemetry Authors">
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

using System.Data;
using System.Threading;
using OpenTelemetry.AutoInstrumentation.CallTarget;
using OpenTelemetry.AutoInstrumentation.Util;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.MySqlData;

/// <summary>
/// MySql.Data.MySqlClient.MySqlCommand.ExecuteReaderAsyncIntegration calltarget instrumentation
/// </summary>
[InstrumentMethod(
    assemblyName: "MySql.Data",
    typeName: "MySql.Data.MySqlClient.MySqlCommand",
    methodName: "ExecuteReaderAsync",
    returnTypeName: "System.Threading.Tasks.Task`1<MySql.Data.MySqlClient.MySqlDataReader>",
    parameterTypeNames: new[] { "System.Data.CommandBehavior", ClrNames.Bool, ClrNames.CancellationToken },
    minimumVersion: "8.0.33",
    maximumVersion: "8.65535.65535",
    integrationName: "MySqlData",
    type: InstrumentationType.Trace)]
public static class ExecuteReaderAsyncIntegration
{
    /// <summary>
    /// OnMethodBegin callback
    /// </summary>
    /// <typeparam name="TTarget">Type of the target</typeparam>
    /// <param name="instance">Instance value, aka `this` of the instrumented method.</param>
    /// <param name="commandBehavior">The provided CommandBehavior value.</param>
    /// <param name="execAsync">Indicates whether to run the query asynchronously</param>
    /// <param name="cancellationToken">The provided CancellationToken value.</param>
    /// <returns>CallTarget state value</returns>
    internal static CallTargetState OnMethodBegin<TTarget>(TTarget instance, CommandBehavior commandBehavior, bool execAsync, CancellationToken cancellationToken)
        where TTarget : IMySqlCommand
    {
        return new CallTargetState(activity: MySqlDataCommon.CreateActivity(instance), state: instance!);
    }

    /// <summary>
    /// OnAsyncMethodEnd callback
    /// </summary>
    /// <typeparam name="TTarget">Type of the target</typeparam>
    /// <typeparam name="TResult">Type of the result value</typeparam>
    /// <param name="instance">Instance value, aka `this` of the instrumented method.</param>
    /// <param name="result">The result value.</param>
    /// <param name="exception">Exception instance in case the original code threw an exception.</param>
    /// <param name="state">Calltarget state value</param>
    /// <returns>A response value, in an async scenario will be T of Task of T</returns>
    internal static TResult OnAsyncMethodEnd<TTarget, TResult>(TTarget instance, TResult result, Exception? exception, in CallTargetState state)
    {
        var activity = state.Activity;
        if (activity is null)
        {
            return result;
        }

        try
        {
            if (exception != null)
            {
                activity.SetException(exception);
            }
        }
        finally
        {
            activity.Dispose();
        }

        return result;
    }
}
#endif
