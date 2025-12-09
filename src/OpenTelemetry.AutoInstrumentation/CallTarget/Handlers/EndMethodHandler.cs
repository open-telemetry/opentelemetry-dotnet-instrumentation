// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.CompilerServices;

namespace OpenTelemetry.AutoInstrumentation.CallTarget.Handlers;

internal static class EndMethodHandler<TIntegration, TTarget>
{
    private static readonly InvokeDelegate _invokeDelegate;

    static EndMethodHandler()
    {
        try
        {
            var dynMethod = IntegrationMapper.CreateEndMethodDelegate(typeof(TIntegration), typeof(TTarget));
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
            if (_invokeDelegate is null)
            {
                _invokeDelegate = (TTarget instance, Exception exception, in CallTargetState state) => CallTargetReturn.GetDefault();
            }
        }
    }

    internal delegate CallTargetReturn InvokeDelegate(TTarget instance, Exception exception, in CallTargetState state);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static CallTargetReturn Invoke(TTarget instance, Exception exception, in CallTargetState state)
    {
        return _invokeDelegate(instance, exception, in state);
    }
}
