// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using System.Reflection;
using OpenTelemetry.AutoInstrumentation.CallTarget;
using OpenTelemetry.AutoInstrumentation.Util;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.NoCode;

internal static class NoCodeIntegrationHelper
{
    private static readonly ActivitySource Source = new("OpenTelemetry.AutoInstrumentation.NoCode");

    internal static CallTargetState OnMethodBegin()
    {
        const int methodNameFrameIndex = 3;

        var method = new StackFrame(methodNameFrameIndex).GetMethod();
        var methodName = method?.Name;
        var typeName = method?.DeclaringType?.FullName;
        var assemblyName = method?.DeclaringType?.Assembly.GetName().Name;
        var parameters = method?.GetParameters()!;
        var noCodeEntry = NoCodeBytecodeIntegrationBuilder.NoCodeEntries.Single(x =>
            x.TargetType == typeName &&
            x.TargetMethod == methodName &&
            x.TargetAssembly == assemblyName &&
            CheckParameters(x.TargetSignatureTypes, parameters));

        // TODO Span kind and attributes from configuration
        // TODO Consider execute some dynamic code to build name/span/attributes based on config and taking data from method parameters
        var activity = Source.StartActivity(name: noCodeEntry.SpanName, kind: ActivityKind.Internal, tags: [new("TODO", "attributes-from-configuration")]);
        return new CallTargetState(activity, null);
    }

    internal static CallTargetReturn<TReturn> OnMethodEnd<TReturn>(TReturn returnValue, Exception? exception, in CallTargetState state)
    {
        var activity = state.Activity;
        if (activity is null)
        {
            return new CallTargetReturn<TReturn>(returnValue);
        }

        HandleActivity(exception, activity);

        return new CallTargetReturn<TReturn>(returnValue);
    }

    internal static CallTargetReturn OnMethodEnd(Exception? exception, in CallTargetState state)
    {
        var activity = state.Activity;
        if (activity is null)
        {
            return CallTargetReturn.GetDefault();
        }

        HandleActivity(exception, activity);

        return CallTargetReturn.GetDefault();
    }

    private static void HandleActivity(Exception? exception, Activity activity)
    {
        if (exception is not null)
        {
            activity.SetException(exception);
        }

        activity.Stop();
    }

    private static bool CheckParameters(string[] targetSignatureTypes, ParameterInfo[] parameters)
    {
        if (targetSignatureTypes.Length != parameters.Length + 1)
        {
            return false;
        }

        for (int i = 0; i < parameters.Length; i++)
        {
            if (targetSignatureTypes[i + 1] != parameters[i].ParameterType.FullName)
            {
                return false;
            }
        }

        return true;
    }
}
