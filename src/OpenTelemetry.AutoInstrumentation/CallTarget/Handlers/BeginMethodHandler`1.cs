// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

#pragma warning disable SA1649 // File name must match first type name

namespace OpenTelemetry.AutoInstrumentation.CallTarget.Handlers;

internal static class BeginMethodHandler<TIntegration, TTarget, TArg1>
{
    private static readonly InvokeDelegate _invokeDelegate;

    static BeginMethodHandler()
    {
        try
        {
            Type tArg1ByRef = typeof(TArg1).IsByRef ? typeof(TArg1) : typeof(TArg1).MakeByRefType();
            DynamicMethod? dynMethod = IntegrationMapper.CreateBeginMethodDelegate(typeof(TIntegration), typeof(TTarget), [tArg1ByRef]);
            if (dynMethod != null)
            {
                _invokeDelegate = (InvokeDelegate)dynMethod.CreateDelegate(typeof(InvokeDelegate));
            }
        }
        catch (Exception ex)
        {
            throw new CallTargetInvokerException(ex);
        }
        finally
        {
            _invokeDelegate ??= (TTarget instance, ref TArg1 arg1) => CallTargetState.GetDefault();
        }
    }

    internal delegate CallTargetState InvokeDelegate(TTarget instance, ref TArg1 arg1);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static CallTargetState Invoke(TTarget instance, ref TArg1 arg1)
    {
        return new CallTargetState(Activity.Current, _invokeDelegate(instance, ref arg1));
    }
}
