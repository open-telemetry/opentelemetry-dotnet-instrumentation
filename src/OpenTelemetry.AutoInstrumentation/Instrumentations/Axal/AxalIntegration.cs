// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System;
using OpenTelemetry.AutoInstrumentation.CallTarget;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.Axal;

/// <summary>
/// BusinessLogic instrumentation for ProcessBusinessOperation method
/// </summary>
[InstrumentMethod(
assemblyName: "Examples.AspNetCoreMvc",
typeName: "?Examples",
methodName: "ProcessBusinessOperation",
returnTypeName: "System.String",
parameterTypeNames: new[] { "System.String" },
minimumVersion: "1.0.0",
maximumVersion: "65535.65535.65535",
integrationName: "Axal",
type: InstrumentationType.Trace)]
public static class AxalIntegration
{
    /// <summary>
    /// OnMethodBegin callback
    /// </summary>
    /// <typeparam name="TTarget">Type of the target</typeparam>
    /// <typeparam name="TArg1">Type of the first argument</typeparam>
    /// <param name="instance">Instance value, aka `this` of the instrumented method.</param>
    /// <param name="arg1">First argument</param>
    /// <returns>Calltarget state value</returns>
    internal static CallTargetState OnMethodBegin<TTarget, TArg1>(TTarget instance, TArg1 arg1)
    {
        Console.WriteLine("HELLO WORLD");
        return new CallTargetState(null, null);
    }

    /// <summary>
    /// OnMethodEnd callback
    /// </summary>
    /// <typeparam name="TTarget">Type of the target</typeparam>
    /// <param name="instance">Instance value, aka `this` of the instrumented method.</param>
    /// <param name="returnValue">Return value</param>
    /// <param name="exception">Exception instance in case the original code threw an exception.</param>
    /// <param name="state">Calltarget state value</param>
    /// <returns>A response value, in an async scenario will be T of Task of T</returns>
    internal static CallTargetReturn<string> OnMethodEnd<TTarget>(TTarget instance, string returnValue, Exception exception, in CallTargetState state)
    {
        return new CallTargetReturn<string>(returnValue);
    }
}
