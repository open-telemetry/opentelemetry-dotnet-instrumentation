// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using OpenTelemetry.AutoInstrumentation.CallTarget;
using OpenTelemetry.AutoInstrumentation.Configurations;
using OpenTelemetry.AutoInstrumentation.Logging;
using OpenTelemetry.AutoInstrumentation.Util;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.NoCode;

internal static class NoCodeIntegrationHelper
{
    private static readonly ActivitySource Source = new("OpenTelemetry.AutoInstrumentation.NoCode", AutoInstrumentationVersion.Version);
    private static readonly string[] GenericParameterClassNames = ["!0", "!1", "!2", "!3", "!4", "!5", "!6", "!7", "!8", "!9"];
    private static readonly string[] GenericParameterMethodNames = ["!!0", "!!1", "!!2", "!!3", "!!4", "!!5", "!!6", "!!7", "!!8", "!!9"];
    private static readonly IOtelLogger Log = OtelLogging.GetLogger();

    internal static List<NoCodeInstrumentedMethod> NoCodeEntries { get; set; } = [];

    internal static CallTargetState OnMethodBegin()
    {
        var noCodeEntry = GetInstrumentedMethod();

        if (noCodeEntry == null)
        {
            Log.Warning("NoCode OnMethodBegin: Could not find valid method in stack trace from NoCodeEntries list");
            return CallTargetState.GetDefault();
        }

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
    private static NoCodeInstrumentedMethod? GetInstrumentedMethod()
    {
        // Typically, the first method outside OpenTelemetry.AutoInstrumentation assembly is at skipFrames = 2 or 3
        // For some cases, compiler does not inline all OpenTelemetry.AutoInstrumentation methods, so we check up to skipFrames = 10

        for (var skipFrames = 2; skipFrames < 10; skipFrames++)
        {
            var method = new StackFrame(skipFrames).GetMethod();
            if (method == null)
            {
                // End of stack trace reached, no more frames to analyze
                return null;
            }

            var declaringType = method.DeclaringType;
            var assemblyName = declaringType?.Assembly.GetName().Name;
            var typeName = declaringType?.FullName;
            var methodName = method.Name;

            // Skip methods with no declaring type or assembly (dynamically generated methods, like <unknown>.NoCodeIntegration0.OnMethodBegin)
            if (declaringType == null || string.IsNullOrEmpty(assemblyName))
            {
                continue;
            }

            if (assemblyName!.Equals("OpenTelemetry.AutoInstrumentation", StringComparison.Ordinal))
            {
                continue;
            }

            // Skip compiler-generated types (async state machines like Program+<Main>d__0,
            // iterators, closures like <>c__DisplayClass0_0).
            // In Release mode on .NET Framework, async state machine MoveNext methods
            // appear in the stack instead of the original user method.
            if (IsCompilerGeneratedType(declaringType))
            {
                continue;
            }

            var parameters = method.GetParameters();

            // Only accept methods that exist in NoCodeEntries list
            var noCodeEntry = NoCodeEntries.SingleOrDefault(x =>
                x.Definition.TargetType == typeName &&
                x.Definition.TargetMethod == methodName &&
                x.Definition.TargetAssembly == assemblyName &&
                CheckParameters(x.SignatureTypes, parameters));

            if (noCodeEntry == null)
            {
                continue;
            }

            return noCodeEntry;
        }

        return null;
    }

    private static bool IsCompilerGeneratedType(Type type)
    {
        // Compiler-generated types have '<' in their name, e.g.:
        // - <Main>d__0 (async state machine)
        // - <>c__DisplayClass0_0 (closure/lambda)
        var typeName = type.Name;
#if NET
        if (typeName.Contains('<', StringComparison.Ordinal))
#else
        if (typeName.IndexOf('<') >= 0)
#endif
        {
            return true;
        }

        return type.IsDefined(typeof(CompilerGeneratedAttribute), false);
    }
}
