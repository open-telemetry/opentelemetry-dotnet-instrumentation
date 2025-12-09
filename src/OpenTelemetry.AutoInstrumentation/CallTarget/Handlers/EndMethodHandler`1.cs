// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using OpenTelemetry.AutoInstrumentation.CallTarget.Handlers.Continuations;

#pragma warning disable SA1649 // File name must match first type name

namespace OpenTelemetry.AutoInstrumentation.CallTarget.Handlers;

internal static class EndMethodHandler<TIntegration, TTarget, TReturn>
{
    private static readonly InvokeDelegate? _invokeDelegate;
    private static readonly ContinuationGenerator<TTarget, TReturn>? _continuationGenerator;

    static EndMethodHandler()
    {
        Type returnType = typeof(TReturn);
        try
        {
            DynamicMethod? dynMethod = IntegrationMapper.CreateEndMethodDelegate(typeof(TIntegration), typeof(TTarget), returnType);
            if (dynMethod != null)
            {
                _invokeDelegate = (InvokeDelegate)dynMethod.CreateDelegate(typeof(InvokeDelegate));
            }
        }
        catch (Exception ex)
        {
            throw new CallTargetInvokerException(ex);
        }

        if (returnType.IsGenericType)
        {
            if (typeof(Task).IsAssignableFrom(returnType))
            {
                // The type is a Task<>
                _continuationGenerator = (ContinuationGenerator<TTarget, TReturn>?)Activator.CreateInstance(typeof(TaskContinuationGenerator<,,,>).MakeGenericType(typeof(TIntegration), typeof(TTarget), returnType, ContinuationsHelper.GetResultType(returnType)));
            }
#if NET
            else
            {
                var genericReturnType = returnType.GetGenericTypeDefinition();
                if (genericReturnType == typeof(ValueTask<>))
                {
                    // The type is a ValueTask<>
                    _continuationGenerator = (ContinuationGenerator<TTarget, TReturn>?)Activator.CreateInstance(typeof(ValueTaskContinuationGenerator<,,,>).MakeGenericType(typeof(TIntegration), typeof(TTarget), returnType, ContinuationsHelper.GetResultType(returnType)));
                }
            }
#endif
        }
        else
        {
            if (returnType == typeof(Task))
            {
                // The type is a Task
                _continuationGenerator = new TaskContinuationGenerator<TIntegration, TTarget, TReturn>();
            }
#if NET
            else if (returnType == typeof(ValueTask))
            {
                // The type is a ValueTask
                _continuationGenerator = new ValueTaskContinuationGenerator<TIntegration, TTarget, TReturn>();
            }
#endif
        }
    }

    internal delegate CallTargetReturn<TReturn?> InvokeDelegate(TTarget instance, TReturn? returnValue, Exception exception, in CallTargetState state);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static CallTargetReturn<TReturn?> Invoke(TTarget instance, TReturn? returnValue, Exception exception, in CallTargetState state)
    {
        if (_continuationGenerator != null)
        {
            returnValue = _continuationGenerator.SetContinuation(instance, returnValue, exception, in state);

            // Restore previous scope if there is a continuation
            // This is used to mimic the ExecutionContext copy from the StateMachine
            Activity.Current = state.PreviousActivity;
        }

        if (_invokeDelegate != null)
        {
            CallTargetReturn<TReturn?> returnWrap = _invokeDelegate(instance, returnValue, exception, in state);
            returnValue = returnWrap.GetReturnValue();
        }

        return new CallTargetReturn<TReturn?>(returnValue);
    }
}
