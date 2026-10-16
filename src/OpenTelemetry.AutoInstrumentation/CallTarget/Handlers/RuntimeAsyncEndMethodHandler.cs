// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET
using System.Runtime.CompilerServices;

namespace OpenTelemetry.AutoInstrumentation.CallTarget.Handlers;

internal static class RuntimeAsyncEndMethodHandler<TIntegration, TTarget, TReturn>
{
    private static readonly InvokeDelegate? _invokeDelegate;

#pragma warning disable CA1810 // Initialize reference type static fields inline. The delegate is generated from the integration method.
    static RuntimeAsyncEndMethodHandler()
#pragma warning restore CA1810 // Initialize reference type static fields inline. The delegate is generated from the integration method.
    {
        try
        {
            var result = IntegrationMapper.CreateAsyncEndMethodDelegate(typeof(TIntegration), typeof(TTarget), typeof(TReturn));
            if (result.Method != null)
            {
                _invokeDelegate = (InvokeDelegate)result.Method.CreateDelegate(typeof(InvokeDelegate));
            }
        }
        catch (Exception ex)
        {
#pragma warning disable CA1065 // Do not raise exceptions in unexpected locations. Needed for bytecode instrumentation.
            throw new CallTargetInvokerException(ex);
#pragma warning restore CA1065 // Do not raise exceptions in unexpected locations. Needed for bytecode instrumentation.
        }
    }

    internal delegate TReturn? InvokeDelegate(TTarget instance, TReturn? returnValue, Exception exception, in CallTargetState state);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static TReturn? Invoke(TTarget instance, TReturn? returnValue, Exception exception, in CallTargetState state)
    {
        return _invokeDelegate is null ? returnValue : _invokeDelegate(instance, returnValue, exception, in state);
    }
}
#endif
