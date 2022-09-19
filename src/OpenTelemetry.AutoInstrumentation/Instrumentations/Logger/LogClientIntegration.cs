// <copyright file="LogClientIntegration.cs" company="OpenTelemetry Authors">
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
using System.Linq.Expressions;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.AutoInstrumentation.CallTarget;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.Logger;

/// <summary>
/// Microsoft.Extensions.Logging.AddLogging calltarget instrumentation
/// </summary>
[InstrumentMethod(
    AssemblyName = "Microsoft.Extensions.Logging",
    TypeName = "Microsoft.Extensions.DependencyInjection.LoggingServiceCollectionExtensions",
    MethodName = "AddLogging",
    ReturnTypeName = "Microsoft.Extensions.DependencyInjection.IServiceCollection",
    ParameterTypeNames = new[] { "Microsoft.Extensions.DependencyInjection.IServiceCollection", "System.Action`1[Microsoft.Extensions.Logging.ILoggingBuilder]" },
    MinimumVersion = "3.1.0",
    MaximumVersion = "6.*.*",
    IntegrationName = "Logging")]
[InstrumentMethod(
    AssemblyName = "Microsoft.Extensions.Logging",
    TypeName = "Microsoft.Extensions.DependencyInjection.LoggingServiceCollectionExtensions",
    MethodName = "AddLogging",
    ReturnTypeName = "Microsoft.Extensions.DependencyInjection.IServiceCollection",
    ParameterTypeNames = new[] { "Microsoft.Extensions.DependencyInjection.IServiceCollection" },
    MinimumVersion = "3.1.0",
    MaximumVersion = "6.65535.65535",
    IntegrationName = "Logging")]
public class LogClientIntegration
{
    /// <summary>
    /// OnMethodBegin callback
    /// </summary>
    /// <typeparam name="TTarget">Type of the target</typeparam>
    /// <typeparam name="TServiceCollection">Type of the IServiceCollection</typeparam>
    /// <typeparam name="TAction">Type of the ILoggingBuilder delegate</typeparam>
    /// <param name="instance">Instance value, aka `this` of the instrumented method.</param>
    /// <param name="service">Value for IServiceCollection.</param>
    /// <param name="configure">Value for ILoggingBuilder delegate.</param>
    /// <returns>Calltarget state value</returns>
    public static CallTargetState OnMethodBegin<TTarget, TServiceCollection, TAction>(TTarget instance, TServiceCollection service, TAction configure)
        where TAction : Delegate
        where TServiceCollection : IServiceCollection
    {
        // Add .AddConsole to services.AddLogging(c => c.AddConsole()
        // Once this works, we could change the call for AddConsole with AddOpenTelemetry.
        var consoleLoggerExtensionsType = Type.GetType("Microsoft.Extensions.Logging.ConsoleLoggerExtensions, Microsoft.Extensions.Logging.Console");
        var loggingBuilderInterface = Type.GetType("Microsoft.Extensions.Logging.ILoggingBuilder, Microsoft.Extensions.Logging");

        var cbParamExpression = Expression.Parameter(loggingBuilderInterface);
        var callExpression = Expression.Call(consoleLoggerExtensionsType, "AddConsole", null, cbParamExpression);
        var lambdaType = typeof(Action<>).MakeGenericType(loggingBuilderInterface);
        var setListenerLambda = Expression.Lambda(lambdaType, callExpression, cbParamExpression);
        var setListenerDelegate = setListenerLambda.Compile();

        var existingDelegate = configure as Delegate;
        var currentDelegate = Delegate.Combine(existingDelegate, setListenerDelegate) as TAction;
        // Replace Action<ILoggingBuilder> method parameter with combined delegate
        configure = currentDelegate;

        return CallTargetState.GetDefault();
    }
}
