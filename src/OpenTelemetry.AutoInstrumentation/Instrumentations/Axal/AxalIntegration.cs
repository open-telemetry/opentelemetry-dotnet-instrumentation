// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Diagnostics;
using OpenTelemetry.AutoInstrumentation.CallTarget;
using OpenTelemetry.AutoInstrumentation.Util;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.Axal;

/// <summary>
/// BusinessLogic instrumentation for ProcessBusinessOperation method
/// </summary>
[InstrumentMethod(
    assemblyName: "?Examples",
    typeName: "?Examples.AspNetCoreMvc.Logic",
    methodName: "?",
    returnTypeName: ClrNames.Void,
    parameterTypeNames: new string[] { },
    minimumVersion: "1.0.0",
    maximumVersion: "65535.65535.65535",
    integrationName: "Axal",
    type: InstrumentationType.Trace)]
public static class AxalIntegration
{
    private static readonly ActivitySource Source = new("OpenTelemetry.AutoInstrumentation.Axal");

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
        var typeName = typeof(TTarget).FullName ?? "Unknown";
        var activity = Source.StartActivity($"Axal.{typeName}", ActivityKind.Internal);
        if (activity is { IsAllDataRequested: true })
        {
            var argTypes = new List<string> { typeof(TArg1).FullName ?? "Unknown" };
            activity.SetTag("axal.class.name", typeName);
            activity.SetTag("axal.argument.type.names", string.Join(",", argTypes));
        }

        // Return the state with the activity so it can be accessed in OnMethodEnd
        return new CallTargetState(activity, null);
    }

    /// <summary>
    /// OnMethodEnd callback
    /// </summary>
    /// <typeparam name="TTarget">Type of the target</typeparam>
    /// <typeparam name="TReturn">Return type of the method</typeparam>
    /// <param name="instance">Instance value, aka `this` of the instrumented method.</param>
    /// <param name="returnValue">Return value</param>
    /// <param name="exception">Exception instance in case the original code threw an exception.</param>
    /// <param name="state">Calltarget state value</param>
    /// <returns>A response value, in an async scenario will be T of Task of T</returns>
    internal static CallTargetReturn<TReturn> OnMethodEnd<TTarget, TReturn>(TTarget instance, TReturn returnValue, Exception exception, in CallTargetState state)
    {
        var activity = state.Activity;
        if (activity != null)
        {
            activity.SetTag("return.type.name", typeof(TReturn).FullName ?? "Unknown");
            if (exception != null)
            {
                activity.SetException(exception);
                activity.SetStatus(ActivityStatusCode.Error, exception.Message);
            }
            else
            {
                activity.SetStatus(ActivityStatusCode.Ok);
            }

            activity.Stop();
        }

        return new CallTargetReturn<TReturn>(returnValue);
    }
}
