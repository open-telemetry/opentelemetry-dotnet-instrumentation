// <copyright file="LoggingBuilderIntegration.cs" company="OpenTelemetry Authors">
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

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.Logger;

/// <summary>
/// Microsoft.Extensions.Logging.LoggingBuilder calltarget instrumentation
/// </summary>
[InstrumentMethod(
    "Microsoft.Extensions.Logging",
    "Microsoft.Extensions.Logging.LoggingBuilder",
    ".ctor",
    ClrNames.Void,
    new[] { "Microsoft.Extensions.DependencyInjection.IServiceCollection" },
    "3.1.0",
    "7.*.*",
    "ILogger",
    InstrumentationType.Log)]
public static class LoggingBuilderIntegration
{
    /// <summary>
    /// OnMethodEnd callback
    /// </summary>
    /// <typeparam name="TTarget">Type of the target</typeparam>
    /// <param name="instance">Instance value, aka `this` of the instrumented method.</param>
    /// <param name="exception">Exception instance in case the original code threw an exception.</param>
    /// <param name="state">Calltarget state value</param>
    /// <returns>A default CallTargetReturn to satisfy the CallTarget contract</returns>
    internal static CallTargetReturn OnMethodEnd<TTarget>(TTarget instance, Exception exception, CallTargetState state)
    {
#if !NETFRAMEWORK
        if (instance is not null)
        {
            var logBuilderExtensionsType = Type.GetType("OpenTelemetry.AutoInstrumentation.Logger.LogBuilderExtensions, OpenTelemetry.AutoInstrumentation");
            var methodInfo = logBuilderExtensionsType?.GetMethod("AddOpenTelemetryLogs");
            methodInfo?.Invoke(null, new[] { (object)instance });
        }
#endif

        return CallTargetReturn.GetDefault();
    }
}
