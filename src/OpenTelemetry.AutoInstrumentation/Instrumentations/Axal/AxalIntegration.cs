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
    typeName: "?Examples",
    methodName: "?",
    returnTypeName: ClrNames.Void,
    parameterTypeNames: new string[] { },
    minimumVersion: "1.0.0",
    maximumVersion: "65535.65535.65535",
    integrationName: "Axal",
    type: InstrumentationType.Trace)]
public static class AxalIntegration
{
    private const string TypeDelimiter = "|||"; // Using a delimiter that cannot appear in type names
    private static readonly ActivitySource Source = new("OpenTelemetry.AutoInstrumentation.Axal");

    /// <summary>
    /// OnMethodBegin callback
    /// </summary>
    /// <typeparam name="TTarget">Type of the target</typeparam>
    /// <param name="instance">Instance value, aka `this` of the instrumented method.</param>
    /// <returns>Calltarget state value</returns>
    internal static CallTargetState OnMethodBegin<TTarget>(TTarget instance)
    {
        var typeName = typeof(TTarget).FullName ?? "Unknown";
        var activity = Source.StartActivity(typeName, ActivityKind.Internal);
        if (activity is { IsAllDataRequested: true })
        {
            activity.SetTag("axal.argument.type.names", string.Empty);
        }

        // Return the state with the activity so it can be accessed in OnMethodEnd
        return new CallTargetState(activity, null);
    }

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
        var activity = Source.StartActivity(typeName, ActivityKind.Internal);
        if (activity is { IsAllDataRequested: true })
        {
            var arg1Type = typeof(TArg1).FullName ?? "Unknown";
            activity.SetTag("axal.argument.type.names", arg1Type);
        }

        // Return the state with the activity so it can be accessed in OnMethodEnd
        return new CallTargetState(activity, null);
    }

    /// <summary>
    /// OnMethodBegin callback
    /// </summary>
    /// <typeparam name="TTarget">Type of the target</typeparam>
    /// <typeparam name="TArg1">Type of the first argument</typeparam>
    /// <typeparam name="TArg2">Type of the second argument</typeparam>
    /// <param name="instance">Instance value, aka `this` of the instrumented method.</param>
    /// <param name="arg1">First argument</param>
    /// <param name="arg2">Second argument</param>
    /// <returns>Calltarget state value</returns>
    internal static CallTargetState OnMethodBegin<TTarget, TArg1, TArg2>(TTarget instance, TArg1 arg1, TArg2 arg2)
    {
        var typeName = typeof(TTarget).FullName ?? "Unknown";
        var activity = Source.StartActivity(typeName, ActivityKind.Internal);
        if (activity is { IsAllDataRequested: true })
        {
            var arg1Type = typeof(TArg1).FullName ?? "Unknown";
            var arg2Type = typeof(TArg2).FullName ?? "Unknown";
            activity.SetTag("axal.argument.type.names", $"{arg1Type}{TypeDelimiter}{arg2Type}");
        }

        // Return the state with the activity so it can be accessed in OnMethodEnd
        return new CallTargetState(activity, null);
    }

    /// <summary>
    /// OnMethodBegin callback
    /// </summary>
    /// <typeparam name="TTarget">Type of the target</typeparam>
    /// <typeparam name="TArg1">Type of the first argument</typeparam>
    /// <typeparam name="TArg2">Type of the second argument</typeparam>
    /// <typeparam name="TArg3">Type of the third argument</typeparam>
    /// <param name="instance">Instance value, aka `this` of the instrumented method.</param>
    /// <param name="arg1">First argument</param>
    /// <param name="arg2">Second argument</param>
    /// <param name="arg3">Third argument</param>
    /// <returns>Calltarget state value</returns>
    internal static CallTargetState OnMethodBegin<TTarget, TArg1, TArg2, TArg3>(TTarget instance, TArg1 arg1, TArg2 arg2, TArg3 arg3)
    {
        var typeName = typeof(TTarget).FullName ?? "Unknown";
        var activity = Source.StartActivity(typeName, ActivityKind.Internal);
        if (activity is { IsAllDataRequested: true })
        {
            var arg1Type = typeof(TArg1).FullName ?? "Unknown";
            var arg2Type = typeof(TArg2).FullName ?? "Unknown";
            var arg3Type = typeof(TArg3).FullName ?? "Unknown";

            activity.SetTag("axal.argument.type.names", $"{arg1Type}{TypeDelimiter}{arg2Type}{TypeDelimiter}{arg3Type}");
        }

        // Return the state with the activity so it can be accessed in OnMethodEnd
        return new CallTargetState(activity, null);
    }

    /// <summary>
    /// OnMethodBegin callback
    /// </summary>
    /// <typeparam name="TTarget">Type of the target</typeparam>
    /// <typeparam name="TArg1">Type of the first argument</typeparam>
    /// <typeparam name="TArg2">Type of the second argument</typeparam>
    /// <typeparam name="TArg3">Type of the third argument</typeparam>
    /// <typeparam name="TArg4">Type of the fourth argument</typeparam>
    /// <param name="instance">Instance value, aka `this` of the instrumented method.</param>
    /// <param name="arg1">First argument</param>
    /// <param name="arg2">Second argument</param>
    /// <param name="arg3">Third argument</param>
    /// <param name="arg4">Fourth argument</param>
    /// <returns>Calltarget state value</returns>
    internal static CallTargetState OnMethodBegin<TTarget, TArg1, TArg2, TArg3, TArg4>(TTarget instance, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4)
    {
        var typeName = typeof(TTarget).FullName ?? "Unknown";
        var activity = Source.StartActivity(typeName, ActivityKind.Internal);
        if (activity is { IsAllDataRequested: true })
        {
            var arg1Type = typeof(TArg1).FullName ?? "Unknown";
            var arg2Type = typeof(TArg2).FullName ?? "Unknown";
            var arg3Type = typeof(TArg3).FullName ?? "Unknown";
            var arg4Type = typeof(TArg4).FullName ?? "Unknown";

            activity.SetTag("axal.argument.type.names", $"{arg1Type}{TypeDelimiter}{arg2Type}{TypeDelimiter}{arg3Type}{TypeDelimiter}{arg4Type}");
        }

        // Return the state with the activity so it can be accessed in OnMethodEnd
        return new CallTargetState(activity, null);
    }

    /// <summary>
    /// OnMethodBegin callback
    /// </summary>
    /// <typeparam name="TTarget">Type of the target</typeparam>
    /// <typeparam name="TArg1">Type of the first argument</typeparam>
    /// <typeparam name="TArg2">Type of the second argument</typeparam>
    /// <typeparam name="TArg3">Type of the third argument</typeparam>
    /// <typeparam name="TArg4">Type of the fourth argument</typeparam>
    /// <typeparam name="TArg5">Type of the fifth argument</typeparam>
    /// <param name="instance">Instance value, aka `this` of the instrumented method.</param>
    /// <param name="arg1">First argument</param>
    /// <param name="arg2">Second argument</param>
    /// <param name="arg3">Third argument</param>
    /// <param name="arg4">Fourth argument</param>
    /// <param name="arg5">Fifth argument</param>
    /// <returns>Calltarget state value</returns>
    internal static CallTargetState OnMethodBegin<TTarget, TArg1, TArg2, TArg3, TArg4, TArg5>(TTarget instance, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5)
    {
        var typeName = typeof(TTarget).FullName ?? "Unknown";
        var activity = Source.StartActivity(typeName, ActivityKind.Internal);
        if (activity is { IsAllDataRequested: true })
        {
            var arg1Type = typeof(TArg1).FullName ?? "Unknown";
            var arg2Type = typeof(TArg2).FullName ?? "Unknown";
            var arg3Type = typeof(TArg3).FullName ?? "Unknown";
            var arg4Type = typeof(TArg4).FullName ?? "Unknown";
            var arg5Type = typeof(TArg5).FullName ?? "Unknown";

            activity.SetTag("axal.argument.type.names", $"{arg1Type}{TypeDelimiter}{arg2Type}{TypeDelimiter}{arg3Type}{TypeDelimiter}{arg4Type}{TypeDelimiter}{arg5Type}");
        }

        // Return the state with the activity so it can be accessed in OnMethodEnd
        return new CallTargetState(activity, null);
    }

    /// <summary>
    /// OnMethodBegin callback
    /// </summary>
    /// <typeparam name="TTarget">Type of the target</typeparam>
    /// <typeparam name="TArg1">Type of the first argument</typeparam>
    /// <typeparam name="TArg2">Type of the second argument</typeparam>
    /// <typeparam name="TArg3">Type of the third argument</typeparam>
    /// <typeparam name="TArg4">Type of the fourth argument</typeparam>
    /// <typeparam name="TArg5">Type of the fifth argument</typeparam>
    /// <typeparam name="TArg6">Type of the sixth argument</typeparam>
    /// <param name="instance">Instance value, aka `this` of the instrumented method.</param>
    /// <param name="arg1">First argument</param>
    /// <param name="arg2">Second argument</param>
    /// <param name="arg3">Third argument</param>
    /// <param name="arg4">Fourth argument</param>
    /// <param name="arg5">Fifth argument</param>
    /// <param name="arg6">Sixth argument</param>
    /// <returns>Calltarget state value</returns>
    internal static CallTargetState OnMethodBegin<TTarget, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(TTarget instance, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6)
    {
        var typeName = typeof(TTarget).FullName ?? "Unknown";
        var activity = Source.StartActivity(typeName, ActivityKind.Internal);
        if (activity is { IsAllDataRequested: true })
        {
            var arg1Type = typeof(TArg1).FullName ?? "Unknown";
            var arg2Type = typeof(TArg2).FullName ?? "Unknown";
            var arg3Type = typeof(TArg3).FullName ?? "Unknown";
            var arg4Type = typeof(TArg4).FullName ?? "Unknown";
            var arg5Type = typeof(TArg5).FullName ?? "Unknown";
            var arg6Type = typeof(TArg6).FullName ?? "Unknown";

            activity.SetTag("axal.argument.type.names", $"{arg1Type}{TypeDelimiter}{arg2Type}{TypeDelimiter}{arg3Type}{TypeDelimiter}{arg4Type}{TypeDelimiter}{arg5Type}{TypeDelimiter}{arg6Type}");
        }

        // Return the state with the activity so it can be accessed in OnMethodEnd
        return new CallTargetState(activity, null);
    }

    /// <summary>
    /// OnMethodBegin callback
    /// </summary>
    /// <typeparam name="TTarget">Type of the target</typeparam>
    /// <typeparam name="TArg1">Type of the first argument</typeparam>
    /// <typeparam name="TArg2">Type of the second argument</typeparam>
    /// <typeparam name="TArg3">Type of the third argument</typeparam>
    /// <typeparam name="TArg4">Type of the fourth argument</typeparam>
    /// <typeparam name="TArg5">Type of the fifth argument</typeparam>
    /// <typeparam name="TArg6">Type of the sixth argument</typeparam>
    /// <typeparam name="TArg7">Type of the seventh argument</typeparam>
    /// <param name="instance">Instance value, aka `this` of the instrumented method.</param>
    /// <param name="arg1">First argument</param>
    /// <param name="arg2">Second argument</param>
    /// <param name="arg3">Third argument</param>
    /// <param name="arg4">Fourth argument</param>
    /// <param name="arg5">Fifth argument</param>
    /// <param name="arg6">Sixth argument</param>
    /// <param name="arg7">Seventh argument</param>
    /// <returns>Calltarget state value</returns>
    internal static CallTargetState OnMethodBegin<TTarget, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7>(TTarget instance, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7)
    {
        var typeName = typeof(TTarget).FullName ?? "Unknown";
        var activity = Source.StartActivity(typeName, ActivityKind.Internal);
        if (activity is { IsAllDataRequested: true })
        {
            var arg1Type = typeof(TArg1).FullName ?? "Unknown";
            var arg2Type = typeof(TArg2).FullName ?? "Unknown";
            var arg3Type = typeof(TArg3).FullName ?? "Unknown";
            var arg4Type = typeof(TArg4).FullName ?? "Unknown";
            var arg5Type = typeof(TArg5).FullName ?? "Unknown";
            var arg6Type = typeof(TArg6).FullName ?? "Unknown";
            var arg7Type = typeof(TArg7).FullName ?? "Unknown";

            activity.SetTag("axal.argument.type.names", $"{arg1Type}{TypeDelimiter}{arg2Type}{TypeDelimiter}{arg3Type}{TypeDelimiter}{arg4Type}{TypeDelimiter}{arg5Type}{TypeDelimiter}{arg6Type}{TypeDelimiter}{arg7Type}");
        }

        // Return the state with the activity so it can be accessed in OnMethodEnd
        return new CallTargetState(activity, null);
    }

    /// <summary>
    /// OnMethodBegin callback
    /// </summary>
    /// <typeparam name="TTarget">Type of the target</typeparam>
    /// <typeparam name="TArg1">Type of the first argument</typeparam>
    /// <typeparam name="TArg2">Type of the second argument</typeparam>
    /// <typeparam name="TArg3">Type of the third argument</typeparam>
    /// <typeparam name="TArg4">Type of the fourth argument</typeparam>
    /// <typeparam name="TArg5">Type of the fifth argument</typeparam>
    /// <typeparam name="TArg6">Type of the sixth argument</typeparam>
    /// <typeparam name="TArg7">Type of the seventh argument</typeparam>
    /// <typeparam name="TArg8">Type of the eighth argument</typeparam>
    /// <param name="instance">Instance value, aka `this` of the instrumented method.</param>
    /// <param name="arg1">First argument</param>
    /// <param name="arg2">Second argument</param>
    /// <param name="arg3">Third argument</param>
    /// <param name="arg4">Fourth argument</param>
    /// <param name="arg5">Fifth argument</param>
    /// <param name="arg6">Sixth argument</param>
    /// <param name="arg7">Seventh argument</param>
    /// <param name="arg8">Eighth argument</param>
    /// <returns>Calltarget state value</returns>
    internal static CallTargetState OnMethodBegin<TTarget, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8>(TTarget instance, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8)
    {
        var typeName = typeof(TTarget).FullName ?? "Unknown";
        var activity = Source.StartActivity(typeName, ActivityKind.Internal);
        if (activity is { IsAllDataRequested: true })
        {
            var arg1Type = typeof(TArg1).FullName ?? "Unknown";
            var arg2Type = typeof(TArg2).FullName ?? "Unknown";
            var arg3Type = typeof(TArg3).FullName ?? "Unknown";
            var arg4Type = typeof(TArg4).FullName ?? "Unknown";
            var arg5Type = typeof(TArg5).FullName ?? "Unknown";
            var arg6Type = typeof(TArg6).FullName ?? "Unknown";
            var arg7Type = typeof(TArg7).FullName ?? "Unknown";
            var arg8Type = typeof(TArg8).FullName ?? "Unknown";

            activity.SetTag("axal.argument.type.names", $"{arg1Type}{TypeDelimiter}{arg2Type}{TypeDelimiter}{arg3Type}{TypeDelimiter}{arg4Type}{TypeDelimiter}{arg5Type}{TypeDelimiter}{arg6Type}{TypeDelimiter}{arg7Type}{TypeDelimiter}{arg8Type}");
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
            activity.SetTag("axal.return.type.name", typeof(TReturn).FullName ?? "Unknown");
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
