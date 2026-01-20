// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using OpenTelemetry.AutoInstrumentation.CallTarget;
using OpenTelemetry.AutoInstrumentation.Configurations;
using OpenTelemetry.AutoInstrumentation.Util;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.NoCode;

internal static class NoCodeIntegrationHelper
{
    private static readonly ActivitySource Source = new("OpenTelemetry.AutoInstrumentation.NoCode", AutoInstrumentationVersion.Version);
    private static readonly string[] GenericParameterClassNames = ["!0", "!1", "!2", "!3", "!4", "!5", "!6", "!7", "!8", "!9"];
    private static readonly string[] GenericParameterMethodNames = ["!!0", "!!1", "!!2", "!!3", "!!4", "!!5", "!!6", "!!7", "!!8", "!!9"];

    internal static List<NoCodeInstrumentedMethod> NoCodeEntries { get; set; } = [];

    internal static CallTargetState OnMethodBegin()
    {
        var method = GetFirstNonOtelAutoMethod();
        var methodName = method?.Name;
        var typeName = method?.DeclaringType?.FullName;
        var assemblyName = method?.DeclaringType?.Assembly.GetName().Name;
        var parameters = method?.GetParameters()!;
        var noCodeEntry = NoCodeEntries.Single(x =>
            x.Definition.TargetType == typeName &&
            x.Definition.TargetMethod == methodName &&
            x.Definition.TargetAssembly == assemblyName &&
            CheckParameters(x.SignatureTypes, parameters));

        // TODO Consider execute some dynamic code to build name/span/attributes based on config and taking data from method parameters
        var activity = Source.StartActivity(name: noCodeEntry.SpanName, kind: noCodeEntry.ActivityKind, tags: noCodeEntry.Attributes);
        return new CallTargetState(activity, null);
    }

    internal static CallTargetReturn<TReturn> OnMethodEnd<TReturn>(TReturn returnValue, Exception? exception, in CallTargetState state)
    {
        var activity = state.Activity;
        if (activity is null)
        {
            return new CallTargetReturn<TReturn>(returnValue);
        }

        var returnType = typeof(TReturn);
        if (returnType.IsGenericType)
        {
            var genericReturnType = returnType.GetGenericTypeDefinition();
            if (typeof(Task).IsAssignableFrom(returnType))
            {
                // The type is a Task<>
                return new CallTargetReturn<TReturn>(returnValue);
            }
#if NET

            if (genericReturnType == typeof(ValueTask<>))
            {
                // The type is a ValueTask<>
                return new CallTargetReturn<TReturn>(returnValue);
            }
#endif
        }
        else
        {
            if (returnType == typeof(Task))
            {
                // The type is a Task
                return new CallTargetReturn<TReturn>(returnValue);
            }
#if NET

            if (returnType == typeof(ValueTask))
            {
                // The type is a ValueTask
                return new CallTargetReturn<TReturn>(returnValue);
            }
#endif
        }

        HandleActivity(exception, activity);

        return new CallTargetReturn<TReturn>(returnValue);
    }

    internal static TReturn OnAsyncMethodEnd<TReturn>(TReturn returnValue, Exception exception, in CallTargetState state)
    {
        var activity = state.Activity;
        if (activity is null)
        {
            return returnValue;
        }

        HandleActivity(exception, activity);

        return returnValue;
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

        for (var i = 0; i < parameters.Length; i++)
        {
            var parameterTypeNameDefinition = GetParameterTypeNameDefinition(parameters[i]);
            if (targetSignatureTypes[i + 1] != parameterTypeNameDefinition)
            {
                return false;
            }
        }

        return true;
    }

    private static string GetParameterTypeNameDefinition(ParameterInfo parameterInfo)
    {
        if (!string.IsNullOrEmpty(parameterInfo.ParameterType.FullName))
        {
            return parameterInfo.ParameterType.FullName;
        }

        var definedOnMethod = parameterInfo.ParameterType.DeclaringMethod != null;
        var genericParameterPosition = parameterInfo.ParameterType.GenericParameterPosition;

        return definedOnMethod
                ? GenericParameterMethodNames[genericParameterPosition]
                : GenericParameterClassNames[genericParameterPosition];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static MethodBase? GetFirstNonOtelAutoMethod()
    {
        // Typically, the first method outside OpenTelemetry.AutoInstrumentation assembly is at skipFrames = 3
        // For some cases, compiler does not inline all OpenTelemetry.AutoInstrumentation methods, so we check up to skipFrames = 10

        for (var skipFrames = 3; skipFrames < 10; skipFrames++)
        {
            var method = new StackFrame(skipFrames).GetMethod();
            var assemblyName = method?.DeclaringType?.Assembly.GetName().Name;
            if (assemblyName != null && !assemblyName.Equals("OpenTelemetry.AutoInstrumentation", StringComparison.Ordinal))
            {
                return method;
            }
        }

        return null;
    }
}
