// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace OpenTelemetry.AutoInstrumentation.CallTarget.Handlers;

internal static class BeginMethodSlowHandler<TIntegration, TTarget>
{
    private static readonly InvokeDelegate _invokeDelegate;

    static BeginMethodSlowHandler()
    {
        try
        {
            var dynMethod = IntegrationMapper.CreateSlowBeginMethodDelegate(typeof(TIntegration), typeof(TTarget));
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
            _invokeDelegate ??= (instance, arguments) => CallTargetState.GetDefault();
        }
    }

    internal delegate CallTargetState InvokeDelegate(TTarget instance, object[] arguments);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static CallTargetState Invoke(TTarget instance, object[] arguments)
    {
        return new CallTargetState(Activity.Current, _invokeDelegate(instance, arguments));
    }
}
