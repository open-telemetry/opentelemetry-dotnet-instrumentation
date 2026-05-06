// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace OpenTelemetry.AutoInstrumentation.CallTarget;

/// <summary>
/// Reflection-only entry point used by the native-generated mscorlib CallTarget trampoline.
/// </summary>
[Browsable(false)]
[EditorBrowsable(EditorBrowsableState.Never)]
internal static class CallTargetTrampolineInvoker
{
    private const string IndexerTypeFullName = "__OTelCallTargetIndexer`1";

    private static readonly MethodInfo CopyStateToTrampolineMethod = typeof(CallTargetTrampolineInvoker).GetMethod(nameof(CopyStateToTrampoline), BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly MethodInfo CopyStateFromTrampolineMethod = typeof(CallTargetTrampolineInvoker).GetMethod(nameof(CopyStateFromTrampoline), BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly MethodInfo CopyReturnToTrampolineMethod = typeof(CallTargetTrampolineInvoker).GetMethod(nameof(CopyReturnToTrampoline), BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly ConcurrentDictionary<Type, StateFields> StateFieldCache = new();

    private enum TrampolineDelegateKind
    {
        Begin,
        End,
        EndVoid,
        LogException
    }

    /// <summary>
    /// Creates a cached fast-path begin delegate for a generated mscorlib trampoline type.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Delegate CreateBeginDelegate(Type mapType, Type targetType, Type delegateType, int argumentCount)
    {
        try
        {
            var integrationType = ResolveIntegrationType(mapType);
            return CreateBeginDelegateCore(integrationType, targetType, delegateType, argumentCount, slowPath: false);
        }
        catch
        {
            return CreateDefaultDelegate(delegateType, TrampolineDelegateKind.Begin);
        }
    }

    /// <summary>
    /// Creates a cached slow-path begin delegate for a generated mscorlib trampoline type.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Delegate CreateSlowBeginDelegate(Type mapType, Type targetType, Type delegateType)
    {
        try
        {
            var integrationType = ResolveIntegrationType(mapType);
            return CreateBeginDelegateCore(integrationType, targetType, delegateType, argumentCount: 0, slowPath: true);
        }
        catch
        {
            return CreateDefaultDelegate(delegateType, TrampolineDelegateKind.Begin);
        }
    }

    /// <summary>
    /// Creates a cached non-void end delegate for a generated mscorlib trampoline type.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Delegate CreateEndDelegate(Type mapType, Type targetType, Type returnType, Type delegateType)
    {
        try
        {
            var integrationType = ResolveIntegrationType(mapType);
            return CreateEndDelegateCore(integrationType, targetType, returnType, delegateType);
        }
        catch
        {
            return CreateDefaultDelegate(delegateType, TrampolineDelegateKind.End);
        }
    }

    /// <summary>
    /// Creates a cached void end delegate for a generated mscorlib trampoline type.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Delegate CreateEndVoidDelegate(Type mapType, Type targetType, Type delegateType)
    {
        try
        {
            var integrationType = ResolveIntegrationType(mapType);
            return CreateEndVoidDelegateCore(integrationType, targetType, delegateType);
        }
        catch
        {
            return CreateDefaultDelegate(delegateType, TrampolineDelegateKind.EndVoid);
        }
    }

    /// <summary>
    /// Creates a cached log-exception delegate for a generated mscorlib trampoline type.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Delegate CreateLogExceptionDelegate(Type mapType, Type targetType, Type delegateType)
    {
        try
        {
            var integrationType = ResolveIntegrationType(mapType);
            return CreateLogExceptionDelegateCore(integrationType, targetType, delegateType);
        }
        catch
        {
            return CreateDefaultDelegate(delegateType, TrampolineDelegateKind.LogException);
        }
    }

    private static Delegate CreateBeginDelegateCore(Type integrationType, Type targetType, Type delegateType, int argumentCount, bool slowPath)
    {
        var invoke = delegateType.GetMethod("Invoke")!;
        var parameterTypes = invoke.GetParameters().Select(static p => p.ParameterType).ToArray();
        var trampolineStateType = invoke.ReturnType;

        var genericTypes = new Type[slowPath ? 2 : 2 + argumentCount];
        genericTypes[0] = integrationType;
        genericTypes[1] = targetType;
        if (!slowPath)
        {
            for (var i = 0; i < argumentCount; i++)
            {
                var parameterType = parameterTypes[i + 1];
                genericTypes[i + 2] = parameterType.IsByRef ? parameterType.GetElementType()! : parameterType;
            }
        }

        var beginMethod = typeof(CallTargetInvoker)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(m => m.Name == nameof(CallTargetInvoker.BeginMethod) &&
                         m.IsGenericMethodDefinition &&
                         m.GetGenericArguments().Length == genericTypes.Length &&
                         m.GetParameters().Length == parameterTypes.Length)
            .MakeGenericMethod(genericTypes);

        var dynamicMethod = new DynamicMethod(
            "OTelCallTargetTrampolineBegin",
            trampolineStateType,
            parameterTypes,
            typeof(CallTargetTrampolineInvoker).Module,
            skipVisibility: true);

        var il = dynamicMethod.GetILGenerator();
        for (var i = 0; i < parameterTypes.Length; i++)
        {
            il.EmitLdarg(i);
        }

        il.Emit(OpCodes.Call, beginMethod);
        EmitLoadType(il, trampolineStateType);
        il.Emit(OpCodes.Call, CopyStateToTrampolineMethod);
        il.Emit(OpCodes.Unbox_Any, trampolineStateType);
        il.Emit(OpCodes.Ret);
        return dynamicMethod.CreateDelegate(delegateType);
    }

    private static Delegate CreateEndDelegateCore(Type integrationType, Type targetType, Type returnType, Type delegateType)
    {
        var invoke = delegateType.GetMethod("Invoke")!;
        var parameterTypes = invoke.GetParameters().Select(static p => p.ParameterType).ToArray();
        var trampolineReturnType = invoke.ReturnType;
        var trampolineStateType = parameterTypes[3].GetElementType() ?? parameterTypes[3];

        var endMethod = typeof(CallTargetInvoker)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(m => m.Name == nameof(CallTargetInvoker.EndMethod) &&
                         m.IsGenericMethodDefinition &&
                         m.GetGenericArguments().Length == 3)
            .MakeGenericMethod(integrationType, targetType, returnType);

        var dynamicMethod = new DynamicMethod(
            "OTelCallTargetTrampolineEnd",
            trampolineReturnType,
            parameterTypes,
            typeof(CallTargetTrampolineInvoker).Module,
            skipVisibility: true);

        var il = dynamicMethod.GetILGenerator();
        var stateLocal = il.DeclareLocal(typeof(CallTargetState));

        il.EmitLdarg(3);
        il.Emit(OpCodes.Ldobj, trampolineStateType);
        il.Emit(OpCodes.Box, trampolineStateType);
        il.Emit(OpCodes.Call, CopyStateFromTrampolineMethod);
        il.Emit(OpCodes.Stloc, stateLocal);

        il.EmitLdarg(0);
        il.EmitLdarg(1);
        il.EmitLdarg(2);
        il.Emit(OpCodes.Ldloca, stateLocal);
        il.Emit(OpCodes.Call, endMethod);
        EmitLoadType(il, trampolineReturnType);
        il.Emit(OpCodes.Call, CopyReturnToTrampolineMethod.MakeGenericMethod(returnType));
        il.Emit(OpCodes.Unbox_Any, trampolineReturnType);
        il.Emit(OpCodes.Ret);
        return dynamicMethod.CreateDelegate(delegateType);
    }

    private static Delegate CreateEndVoidDelegateCore(Type integrationType, Type targetType, Type delegateType)
    {
        var invoke = delegateType.GetMethod("Invoke")!;
        var parameterTypes = invoke.GetParameters().Select(static p => p.ParameterType).ToArray();
        var trampolineReturnType = invoke.ReturnType;
        var trampolineStateType = parameterTypes[2].GetElementType() ?? parameterTypes[2];

        var endMethod = typeof(CallTargetInvoker)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(m => m.Name == nameof(CallTargetInvoker.EndMethod) &&
                         m.IsGenericMethodDefinition &&
                         m.GetGenericArguments().Length == 2)
            .MakeGenericMethod(integrationType, targetType);

        var dynamicMethod = new DynamicMethod(
            "OTelCallTargetTrampolineEndVoid",
            trampolineReturnType,
            parameterTypes,
            typeof(CallTargetTrampolineInvoker).Module,
            skipVisibility: true);

        var il = dynamicMethod.GetILGenerator();
        var stateLocal = il.DeclareLocal(typeof(CallTargetState));
        var returnLocal = il.DeclareLocal(trampolineReturnType);

        il.EmitLdarg(2);
        il.Emit(OpCodes.Ldobj, trampolineStateType);
        il.Emit(OpCodes.Box, trampolineStateType);
        il.Emit(OpCodes.Call, CopyStateFromTrampolineMethod);
        il.Emit(OpCodes.Stloc, stateLocal);

        il.EmitLdarg(0);
        il.EmitLdarg(1);
        il.Emit(OpCodes.Ldloca, stateLocal);
        il.Emit(OpCodes.Call, endMethod);
        il.Emit(OpCodes.Pop);

        il.Emit(OpCodes.Ldloca, returnLocal);
        il.Emit(OpCodes.Initobj, trampolineReturnType);
        il.Emit(OpCodes.Ldloc, returnLocal);
        il.Emit(OpCodes.Ret);
        return dynamicMethod.CreateDelegate(delegateType);
    }

    private static Delegate CreateLogExceptionDelegateCore(Type integrationType, Type targetType, Type delegateType)
    {
        var invoke = delegateType.GetMethod("Invoke")!;
        var parameterTypes = invoke.GetParameters().Select(static p => p.ParameterType).ToArray();
        var logMethod = typeof(CallTargetInvoker)
            .GetMethod(nameof(CallTargetInvoker.LogException), BindingFlags.Public | BindingFlags.Static)!
            .MakeGenericMethod(integrationType, targetType);

        var dynamicMethod = new DynamicMethod(
            "OTelCallTargetTrampolineLogException",
            typeof(void),
            parameterTypes,
            typeof(CallTargetTrampolineInvoker).Module,
            skipVisibility: true);

        var il = dynamicMethod.GetILGenerator();
        il.EmitLdarg(0);
        il.Emit(OpCodes.Call, logMethod);
        il.Emit(OpCodes.Ret);
        return dynamicMethod.CreateDelegate(delegateType);
    }

    private static Delegate CreateDefaultDelegate(Type delegateType, TrampolineDelegateKind kind)
    {
        var invoke = delegateType.GetMethod("Invoke")!;
        var parameterTypes = invoke.GetParameters().Select(static p => p.ParameterType).ToArray();
        var dynamicMethod = new DynamicMethod(
            "OTelCallTargetTrampolineDefault",
            invoke.ReturnType,
            parameterTypes,
            typeof(CallTargetTrampolineInvoker).Module,
            skipVisibility: true);

        var il = dynamicMethod.GetILGenerator();
        if (invoke.ReturnType == typeof(void))
        {
            il.Emit(OpCodes.Ret);
            return dynamicMethod.CreateDelegate(delegateType);
        }

        var returnLocal = il.DeclareLocal(invoke.ReturnType);
        if (kind == TrampolineDelegateKind.End && parameterTypes.Length > 1)
        {
            var ctor = invoke.ReturnType.GetConstructor([parameterTypes[1]]);
            if (ctor is not null)
            {
                il.EmitLdarg(1);
                il.Emit(OpCodes.Newobj, ctor);
                il.Emit(OpCodes.Ret);
                return dynamicMethod.CreateDelegate(delegateType);
            }
        }

        il.Emit(OpCodes.Ldloca, returnLocal);
        il.Emit(OpCodes.Initobj, invoke.ReturnType);
        il.Emit(OpCodes.Ldloc, returnLocal);
        il.Emit(OpCodes.Ret);
        return dynamicMethod.CreateDelegate(delegateType);
    }

    private static object CopyStateToTrampoline(CallTargetState state, Type trampolineStateType)
    {
        var fields = StateFieldCache.GetOrAdd(trampolineStateType, static type => new StateFields(type));
        var boxedState = Activator.CreateInstance(trampolineStateType)!;
        fields.PreviousActivity.SetValue(boxedState, state.PreviousActivity);
        fields.Activity.SetValue(boxedState, state.Activity);
        fields.State.SetValue(boxedState, state.State);
        fields.StartTime.SetValue(boxedState, state.StartTime);
        return boxedState;
    }

    private static CallTargetState CopyStateFromTrampoline(object? boxedState)
    {
        if (boxedState is null)
        {
            return default;
        }

        var fields = StateFieldCache.GetOrAdd(boxedState.GetType(), static type => new StateFields(type));
        var activity = fields.Activity.GetValue(boxedState) as Activity;
        var customState = fields.State.GetValue(boxedState);
        var startTimeValue = fields.StartTime.GetValue(boxedState);
        var startTime = startTimeValue is DateTimeOffset dateTimeOffset ? dateTimeOffset : (DateTimeOffset?)null;
        var callTargetState = new CallTargetState(activity, customState, startTime);
        var previousActivity = fields.PreviousActivity.GetValue(boxedState) as Activity;
        return previousActivity is null ? callTargetState : new CallTargetState(previousActivity, callTargetState);
    }

    private static object CopyReturnToTrampoline<TReturn>(CallTargetReturn<TReturn?> returnValue, Type trampolineReturnType)
    {
        return Activator.CreateInstance(trampolineReturnType, returnValue.GetReturnValue())!;
    }

    private static Type ResolveIntegrationType(Type mapType)
    {
        var integrationIndex = GetIntegrationIndex(mapType);
        var assemblyNamePtr = NativeMethods.GetCallTargetTrampolineIntegrationAssembly(integrationIndex);
        var integrationTypePtr = NativeMethods.GetCallTargetTrampolineIntegrationType(integrationIndex);
        if (assemblyNamePtr == IntPtr.Zero || integrationTypePtr == IntPtr.Zero)
        {
            throw new InvalidOperationException($"No CallTarget trampoline integration is registered for index {integrationIndex}.");
        }

        var assemblyName = Marshal.PtrToStringUni(assemblyNamePtr)!;
        var integrationTypeName = Marshal.PtrToStringUni(integrationTypePtr)!;
        return Assembly.Load(assemblyName).GetType(integrationTypeName, throwOnError: true)!;
    }

    private static int GetIntegrationIndex(Type mapType)
    {
        var depth = 0;
        var current = mapType;
        while (current.IsGenericType && current.GetGenericTypeDefinition().FullName == IndexerTypeFullName)
        {
            depth++;
            current = current.GetGenericArguments()[0];
        }

        if (depth == 0 || current != typeof(object))
        {
            throw new InvalidOperationException($"Invalid CallTarget trampoline map type: {mapType}.");
        }

        return depth - 1;
    }

    private static void EmitLoadType(ILGenerator il, Type type)
    {
        il.Emit(OpCodes.Ldtoken, type);
        il.Emit(OpCodes.Call, typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle))!);
    }

    private static void EmitLdarg(this ILGenerator il, int index)
    {
        switch (index)
        {
            case 0:
                il.Emit(OpCodes.Ldarg_0);
                break;
            case 1:
                il.Emit(OpCodes.Ldarg_1);
                break;
            case 2:
                il.Emit(OpCodes.Ldarg_2);
                break;
            case 3:
                il.Emit(OpCodes.Ldarg_3);
                break;
            default:
                il.Emit(index <= byte.MaxValue ? OpCodes.Ldarg_S : OpCodes.Ldarg, index);
                break;
        }
    }

    private sealed class StateFields
    {
        public StateFields(Type type)
        {
            PreviousActivity = type.GetField("PreviousActivity", BindingFlags.Public | BindingFlags.Instance)!;
            Activity = type.GetField("Activity", BindingFlags.Public | BindingFlags.Instance)!;
            State = type.GetField("State", BindingFlags.Public | BindingFlags.Instance)!;
            StartTime = type.GetField("StartTime", BindingFlags.Public | BindingFlags.Instance)!;
        }

        public FieldInfo PreviousActivity { get; }

        public FieldInfo Activity { get; }

        public FieldInfo State { get; }

        public FieldInfo StartTime { get; }
    }
}
