// <copyright file="ExecuteAsyncIntegration.cs" company="OpenTelemetry Authors">
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
using System.Diagnostics;
using OpenTelemetry.AutoInstrumentation.CallTarget;
using OpenTelemetry.AutoInstrumentation.Util;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.GraphQL;

/// <summary>
/// GraphQL.Execution.ExecutionStrategy calltarget instrumentation
/// </summary>
[GraphQLExecuteAsync(
    AssemblyName = GraphQLCommon.GraphQLAssembly,
    TypeName = "GraphQL.Execution.ExecutionStrategy",
    MinimumVersion = GraphQLCommon.Major2Minor3,
    MaximumVersion = GraphQLCommon.Major2)]
[GraphQLExecuteAsync(
    AssemblyName = GraphQLCommon.GraphQLAssembly,
    TypeName = "GraphQL.Execution.SubscriptionExecutionStrategy",
    MinimumVersion = GraphQLCommon.Major2Minor3,
    MaximumVersion = GraphQLCommon.Major2)]
public class ExecuteAsyncIntegration
{
    private const string ErrorType = "GraphQL.ExecutionError";

    /// <summary>
    /// OnMethodBegin callback
    /// </summary>
    /// <typeparam name="TTarget">Type of the target</typeparam>
    /// <typeparam name="TContext">Type of the execution context</typeparam>
    /// <param name="instance">Instance value, aka `this` of the instrumented method.</param>
    /// <param name="context">The execution context of the GraphQL operation.</param>
    /// <returns>CallTarget state value</returns>
    public static CallTargetState OnMethodBegin<TTarget, TContext>(TTarget instance, TContext context)
        where TContext : IExecutionContext
    {
        return new CallTargetState(activity: GraphQLCommon.CreateActivityFromExecuteAsync(context), state: context);
    }

    /// <summary>
    /// OnAsyncMethodEnd callback
    /// </summary>
    /// <typeparam name="TTarget">Type of the target</typeparam>
    /// <typeparam name="TExecutionResult">Type of the execution result value</typeparam>
    /// <param name="instance">Instance value, aka `this` of the instrumented method.</param>
    /// <param name="executionResult">ExecutionResult instance</param>
    /// <param name="exception">Exception instance in case the original code threw an exception.</param>
    /// <param name="state">Calltarget state value</param>
    /// <returns>A response value, in an async scenario will be T of Task of T</returns>
    public static TExecutionResult OnAsyncMethodEnd<TTarget, TExecutionResult>(TTarget instance, TExecutionResult executionResult, Exception exception, CallTargetState state)
    {
        Activity activity = state.Activity;
        if (activity is null)
        {
            return executionResult;
        }

        try
        {
            if (exception != null)
            {
                activity?.SetException(exception);
            }
            else if (state.State is IExecutionContext context)
            {
                GraphQLCommon.RecordExecutionErrorsIfPresent(activity, ErrorType, context.Errors);
            }
        }
        finally
        {
            activity.Dispose();
        }

        return executionResult;
    }
}
