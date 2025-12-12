// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using System.Runtime.CompilerServices;

#pragma warning disable SA1649 // File name must match first type name

namespace OpenTelemetry.AutoInstrumentation.CallTarget.Handlers;

internal static class BeginMethodHandler<TIntegration, TTarget, TArg1, TArg2, TArg3, TArg4>
{
    private static readonly InvokeDelegate _invokeDelegate;

    static BeginMethodHandler()
    {
        try
        {
            var tArg1ByRef = typeof(TArg1).IsByRef ? typeof(TArg1) : typeof(TArg1).MakeByRefType();
            var tArg2ByRef = typeof(TArg2).IsByRef ? typeof(TArg2) : typeof(TArg2).MakeByRefType();
            var tArg3ByRef = typeof(TArg3).IsByRef ? typeof(TArg3) : typeof(TArg3).MakeByRefType();
            var tArg4ByRef = typeof(TArg4).IsByRef ? typeof(TArg4) : typeof(TArg4).MakeByRefType();
            var dynMethod = IntegrationMapper.CreateBeginMethodDelegate(typeof(TIntegration), typeof(TTarget), [tArg1ByRef, tArg2ByRef, tArg3ByRef, tArg4ByRef]);
            if (dynMethod != null)
            {
                _invokeDelegate = (InvokeDelegate)dynMethod.CreateDelegate(typeof(InvokeDelegate));
            }
        }
        catch (Exception ex)
        {
#pragma warning disable CA1065 // Do not raise exceptions in unexpected locations. Needed for bytecode instrumentation.
            throw new CallTargetInvokerException(ex);
#pragma warning restore CA1065 // Do not raise exceptions in unexpected locations. Needed for bytecode instrumentation.
        }
        finally
        {
            _invokeDelegate ??= (TTarget instance, ref TArg1 arg1, ref TArg2 arg2, ref TArg3 arg3, ref TArg4 arg4) => CallTargetState.GetDefault();
        }
    }

    internal delegate CallTargetState InvokeDelegate(TTarget instance, ref TArg1 arg1, ref TArg2 arg2, ref TArg3 arg3, ref TArg4 arg4);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static CallTargetState Invoke(TTarget instance, ref TArg1 arg1, ref TArg2 arg2, ref TArg3 arg3, ref TArg4 arg4)
    {
        return new CallTargetState(Activity.Current, _invokeDelegate(instance, ref arg1, ref arg2, ref arg3, ref arg4));
    }
}
