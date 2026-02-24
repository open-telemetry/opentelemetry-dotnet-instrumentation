// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using System.Globalization;
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

    internal static CallTargetState OnMethodBegin<TInstance>(TInstance? instance, object?[]? arguments)
    {
        var noCodeEntry = GetInstrumentedMethod();

        if (noCodeEntry == null)
        {
            Log.Warning("NoCode OnMethodBegin: Could not find valid method in stack trace from NoCodeEntries list");
            return CallTargetState.GetDefault();
        }

        // Start with static attributes
        var tags = noCodeEntry.Attributes;

        // Evaluate and add dynamic attributes
        if (noCodeEntry.DynamicAttributes.Count > 0)
        {
            var context = new NoCodeExpressionContext(
                instance: instance,
                arguments: arguments,
                returnValue: null,
                methodName: noCodeEntry.Definition.TargetMethod,
                typeName: noCodeEntry.Definition.TargetType);

            foreach (var dynamicAttr in noCodeEntry.DynamicAttributes)
            {
                try
                {
                    var value = dynamicAttr.Evaluate(context);
                    if (value != null)
                    {
                        AddTagValue(ref tags, dynamicAttr.Name, value, dynamicAttr.Type);
                    }
                }
                catch (Exception ex)
                {
                    Log.Debug("Failed to evaluate dynamic attribute '{0}': {1}", dynamicAttr.Name, ex.Message);
                }
            }
        }

        // Evaluate dynamic span name if configured
        var spanName = noCodeEntry.SpanName;
        if (noCodeEntry.DynamicSpanName != null)
        {
            try
            {
                var context = new NoCodeExpressionContext(
                    instance: instance,
                    arguments: arguments,
                    returnValue: null,
                    methodName: noCodeEntry.Definition.TargetMethod,
                    typeName: noCodeEntry.Definition.TargetType);

                var dynamicNameValue = noCodeEntry.DynamicSpanName.Evaluate(context);
                if (dynamicNameValue != null)
                {
                    spanName = dynamicNameValue.ToString() ?? noCodeEntry.SpanName;
                }
            }
            catch (Exception ex)
            {
                Log.Debug("Failed to evaluate dynamic span name, using static name '{0}'. Error: {1}", noCodeEntry.SpanName, ex.Message);
            }
        }

        var activity = Source.StartActivity(name: spanName, kind: noCodeEntry.ActivityKind, tags: tags);

        // Store state for OnMethodEnd (needed for status rules evaluation)
        var noCodeState = new NoCodeCallTargetState(noCodeEntry, instance, arguments, noCodeEntry.Definition.TargetMethod, noCodeEntry.Definition.TargetType);
        return new CallTargetState(activity, noCodeState);
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

        HandleActivity(returnValue, exception, activity, state.State as NoCodeCallTargetState);

        return new CallTargetReturn<TReturn>(returnValue);
    }

    internal static TReturn OnAsyncMethodEnd<TReturn>(TReturn returnValue, Exception exception, in CallTargetState state)
    {
        var activity = state.Activity;
        if (activity is null)
        {
            return returnValue;
        }

        HandleActivity(returnValue, exception, activity, state.State as NoCodeCallTargetState);

        return returnValue;
    }

    internal static CallTargetReturn OnMethodEnd(Exception? exception, in CallTargetState state)
    {
        var activity = state.Activity;
        if (activity is null)
        {
            return CallTargetReturn.GetDefault();
        }

        HandleActivity<object>(null, exception, activity, state.State as NoCodeCallTargetState);

        return CallTargetReturn.GetDefault();
    }

    private static CallTargetState StartActivity(NoCodeInstrumentedMethod noCodeEntry, object? instance, object?[]? arguments, string? methodName, string? typeName)
    {
        // Start with static attributes
        var tags = noCodeEntry.Attributes;

        // Evaluate and add dynamic attributes
        if (noCodeEntry.DynamicAttributes.Count > 0)
        {
            var context = new NoCodeExpressionContext(
                instance: instance,
                arguments: arguments,
                returnValue: null,
                methodName: methodName,
                typeName: typeName);

            foreach (var dynamicAttr in noCodeEntry.DynamicAttributes)
            {
                try
                {
                    var value = dynamicAttr.Evaluate(context);
                    if (value != null)
                    {
                        AddTagValue(ref tags, dynamicAttr.Name, value, dynamicAttr.Type);
                    }
                }
                catch (Exception ex)
                {
                    Log.Debug("Failed to evaluate dynamic attribute '{0}': {1}", dynamicAttr.Name, ex.Message);
                }
            }
        }

        // Evaluate dynamic span name if configured
        var spanName = noCodeEntry.SpanName;
        if (noCodeEntry.DynamicSpanName != null)
        {
            try
            {
                var context = new NoCodeExpressionContext(
                    instance: instance,
                    arguments: arguments,
                    returnValue: null,
                    methodName: methodName,
                    typeName: typeName);

                var dynamicNameValue = noCodeEntry.DynamicSpanName.Evaluate(context);
                if (dynamicNameValue != null)
                {
                    spanName = dynamicNameValue.ToString() ?? noCodeEntry.SpanName;
                }
            }
            catch (Exception ex)
            {
                Log.Debug("Failed to evaluate dynamic span name, using static name '{0}'. Error: {1}", noCodeEntry.SpanName, ex.Message);
            }
        }

        var activity = Source.StartActivity(name: spanName, kind: noCodeEntry.ActivityKind, tags: tags);

        // Store state for OnMethodEnd (needed for status rules evaluation)
        var noCodeState = new NoCodeCallTargetState(noCodeEntry, instance, arguments, methodName, typeName);
        return new CallTargetState(activity, noCodeState);
    }

    private static void HandleActivity<TReturn>(TReturn? returnValue, Exception? exception, Activity activity, NoCodeCallTargetState? noCodeState)
    {
        // Handle exception first
        if (exception is not null)
        {
            activity.SetException(exception);
            activity.SetStatus(ActivityStatusCode.Error, exception.Message);
        }

        // Apply status rules if configured
        if (noCodeState?.Entry.StatusRules.Count > 0)
        {
            var context = new NoCodeExpressionContext(
                instance: noCodeState.Instance,
                arguments: noCodeState.Arguments,
                returnValue: returnValue,
                methodName: noCodeState.MethodName,
                typeName: noCodeState.TypeName);

            foreach (var rule in noCodeState.Entry.StatusRules)
            {
                try
                {
                    if (rule.EvaluateCondition(context))
                    {
                        activity.SetStatus(rule.StatusCode, rule.Description);
                        break; // First matching rule wins
                    }
                }
                catch (Exception ex)
                {
                    Log.Debug("Failed to evaluate status rule: {0}", ex.Message);
                }
            }
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
        // Per Microsoft naming conventions: Identifiers shouldn't contain two consecutive underscore (_) characters.
        // Those names are reserved for compiler-generated identifiers.
        // This covers async state machines (e.g., <Main>d__0), closures (e.g., <>c__DisplayClass0_0), etc.
        // https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/identifier-names#naming-conventions
        var typeName = type.Name;
#if NET
        if (typeName.Contains("__", StringComparison.Ordinal))
#else
        if (typeName.IndexOf("__", StringComparison.Ordinal) >= 0)
#endif
        {
            return true;
        }

        return type.IsDefined(typeof(CompilerGeneratedAttribute), false);
    }

    private static void AddTagValue(ref TagList tags, string name, object value, string type)
    {
        try
        {
            switch (type)
            {
                case "string":
                    tags.Add(name, value.ToString());
                    break;

                case "int":
                    if (value is long longValue)
                    {
                        tags.Add(name, longValue);
                    }
                    else if (value is int intValue)
                    {
                        tags.Add(name, (long)intValue);
                    }
                    else if (long.TryParse(value.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedLong))
                    {
                        tags.Add(name, parsedLong);
                    }
                    else
                    {
                        Log.Debug("Cannot convert value to int for attribute '{0}': {1}", name, value);
                    }

                    break;

                case "double":
                    if (value is double doubleValue)
                    {
                        tags.Add(name, doubleValue);
                    }
                    else if (value is float floatValue)
                    {
                        tags.Add(name, (double)floatValue);
                    }
                    else if (value is decimal decimalValue)
                    {
                        tags.Add(name, (double)decimalValue);
                    }
                    else if (double.TryParse(value.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedDouble))
                    {
                        tags.Add(name, parsedDouble);
                    }
                    else
                    {
                        Log.Debug("Cannot convert value to double for attribute '{0}': {1}", name, value);
                    }

                    break;

                case "bool":
                    if (value is bool boolValue)
                    {
                        tags.Add(name, boolValue);
                    }
                    else if (bool.TryParse(value.ToString(), out var parsedBool))
                    {
                        tags.Add(name, parsedBool);
                    }
                    else
                    {
                        Log.Debug("Cannot convert value to bool for attribute '{0}': {1}", name, value);
                    }

                    break;

                default:
                    // Default to string representation
                    tags.Add(name, value.ToString());
                    break;
            }
        }
        catch (Exception ex)
        {
            Log.Debug("Failed to add tag value for attribute '{0}': {1}", name, ex.Message);
        }
    }
}
