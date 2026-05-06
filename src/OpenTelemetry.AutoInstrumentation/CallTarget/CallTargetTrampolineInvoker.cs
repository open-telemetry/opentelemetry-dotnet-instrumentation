// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace OpenTelemetry.AutoInstrumentation.CallTarget;

/// <summary>
/// Reflection-only entry point used by the native-generated mscorlib CallTarget trampoline.
/// </summary>
[Browsable(false)]
[EditorBrowsable(EditorBrowsableState.Never)]
internal static class CallTargetTrampolineInvoker
{
    private static readonly MethodInfo BeginCoreMethod = typeof(CallTargetTrampolineInvoker).GetMethod(nameof(BeginCore), BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly MethodInfo EndCoreMethod = typeof(CallTargetTrampolineInvoker).GetMethod(nameof(EndCore), BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly MethodInfo EndVoidCoreMethod = typeof(CallTargetTrampolineInvoker).GetMethod(nameof(EndVoidCore), BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly MethodInfo LogExceptionCoreMethod = typeof(CallTargetTrampolineInvoker).GetMethod(nameof(LogExceptionCore), BindingFlags.NonPublic | BindingFlags.Static)!;

    /// <summary>
    /// Calls CallTarget begin from the generated trampoline.
    /// </summary>
    /// <param name="integrationType">Integration type.</param>
    /// <param name="targetType">Target type.</param>
    /// <param name="instance">Target instance.</param>
    /// <param name="arguments">Target arguments.</param>
    /// <returns>Boxed CallTarget state.</returns>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static object? Begin(Type integrationType, Type targetType, object? instance, object[] arguments)
    {
        return BeginCoreMethod.MakeGenericMethod(integrationType, targetType).Invoke(null, new object?[] { instance, arguments });
    }

    /// <summary>
    /// Calls CallTarget end from the generated trampoline.
    /// </summary>
    /// <param name="integrationType">Integration type.</param>
    /// <param name="targetType">Target type.</param>
    /// <param name="returnType">Return type.</param>
    /// <param name="instance">Target instance.</param>
    /// <param name="returnValue">Current return value.</param>
    /// <param name="exception">Target exception.</param>
    /// <param name="state">Boxed CallTarget state.</param>
    /// <returns>Final return value.</returns>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static object? End(Type integrationType, Type targetType, Type returnType, object? instance, object? returnValue, Exception? exception, object? state)
    {
        return EndCoreMethod.MakeGenericMethod(integrationType, targetType, returnType).Invoke(null, new object?[] { instance, returnValue, exception, state });
    }

    /// <summary>
    /// Calls CallTarget end from the generated trampoline for void methods.
    /// </summary>
    /// <param name="integrationType">Integration type.</param>
    /// <param name="targetType">Target type.</param>
    /// <param name="instance">Target instance.</param>
    /// <param name="exception">Target exception.</param>
    /// <param name="state">Boxed CallTarget state.</param>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void EndVoid(Type integrationType, Type targetType, object? instance, Exception? exception, object? state)
    {
        EndVoidCoreMethod.MakeGenericMethod(integrationType, targetType).Invoke(null, new object?[] { instance, exception, state });
    }

    /// <summary>
    /// Logs CallTarget exceptions from the generated trampoline.
    /// </summary>
    /// <param name="integrationType">Integration type.</param>
    /// <param name="targetType">Target type.</param>
    /// <param name="exception">Exception to log.</param>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void LogException(Type integrationType, Type targetType, Exception exception)
    {
        LogExceptionCoreMethod.MakeGenericMethod(integrationType, targetType).Invoke(null, new object?[] { exception });
    }

    private static CallTargetState BeginCore<TIntegration, TTarget>(object? instance, object[] arguments)
    {
        return CallTargetInvoker.BeginMethod<TIntegration, TTarget>((TTarget)instance!, arguments);
    }

    private static object? EndCore<TIntegration, TTarget, TReturn>(object? instance, object? returnValue, Exception? exception, object? state)
    {
        var callTargetState = state is CallTargetState typedState ? typedState : default;
        var callTargetReturn = CallTargetInvoker.EndMethod<TIntegration, TTarget, TReturn>((TTarget)instance!, (TReturn)returnValue!, exception!, in callTargetState);
        return callTargetReturn.GetReturnValue();
    }

    private static void EndVoidCore<TIntegration, TTarget>(object? instance, Exception? exception, object? state)
    {
        var callTargetState = state is CallTargetState typedState ? typedState : default;
        _ = CallTargetInvoker.EndMethod<TIntegration, TTarget>((TTarget)instance!, exception!, in callTargetState);
    }

    private static void LogExceptionCore<TIntegration, TTarget>(Exception exception)
    {
        CallTargetInvoker.LogException<TIntegration, TTarget>(exception);
    }
}
