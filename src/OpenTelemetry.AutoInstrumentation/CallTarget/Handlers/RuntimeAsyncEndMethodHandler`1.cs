// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET

using System.Runtime.CompilerServices;
using OpenTelemetry.AutoInstrumentation.CallTarget.Handlers.Continuations;

#pragma warning disable SA1649 // File name must match first type name

namespace OpenTelemetry.AutoInstrumentation.CallTarget.Handlers;

internal static class RuntimeAsyncEndMethodHandler<TIntegration, TTarget, TReturn, TDeclaredReturn>
{
    private static readonly ContinuationGenerator<TTarget, TReturn, TReturn>.ContinuationMethodDelegate? OnAsyncMethodEnd;
    private static readonly EndMethodHandler<TIntegration, TTarget, TDeclaredReturn>.InvokeDelegate? OnMethodEnd;
    private static readonly Exception? UnsupportedAsyncCallback;

    private static bool _reportedUnsupportedSubstitution;

#pragma warning disable CA1810 // Static initialization may throw and must be wrapped for CallTarget.
    static RuntimeAsyncEndMethodHandler()
#pragma warning restore CA1810 // Static initialization may throw and must be wrapped for CallTarget.
    {
        try
        {
            var asyncResult = IntegrationMapper.CreateAsyncEndMethodDelegate(typeof(TIntegration), typeof(TTarget), typeof(TReturn));
            if (asyncResult.Method is { } asyncMethod)
            {
                if (asyncResult.IsTaskReturn)
                {
                    UnsupportedAsyncCallback = new NotSupportedException(
                        $"Integration '{typeof(TIntegration).FullName}' has an asynchronous OnAsyncMethodEnd callback, which cannot run in the finally block of runtime-async target '{typeof(TTarget).FullName}'. The integration will be disabled for this target.");
                }
                else
                {
                    OnAsyncMethodEnd = (ContinuationGenerator<TTarget, TReturn, TReturn>.ContinuationMethodDelegate)asyncMethod.CreateDelegate(typeof(ContinuationGenerator<TTarget, TReturn, TReturn>.ContinuationMethodDelegate));
                }
            }

            if (IntegrationMapper.CreateEndMethodDelegate(typeof(TIntegration), typeof(TTarget), typeof(TDeclaredReturn)) is { } endMethod)
            {
                OnMethodEnd = (EndMethodHandler<TIntegration, TTarget, TDeclaredReturn>.InvokeDelegate)endMethod.CreateDelegate(typeof(EndMethodHandler<TIntegration, TTarget, TDeclaredReturn>.InvokeDelegate));
            }
        }
        catch (Exception ex)
        {
#pragma warning disable CA1065 // Needed so CallTarget can disable a faulty instrumentation safely.
            throw new CallTargetInvokerException(ex);
#pragma warning restore CA1065 // Needed so CallTarget can disable a faulty instrumentation safely.
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static CallTargetReturn<TReturn?> Invoke(TTarget? instance, TReturn? returnValue, Exception? exception, in CallTargetState state)
    {
        // Unlike compiler-lowered async, runtime async has no state-machine stub whose prologue
        // runs outside the runtime's ExecutionContext save/restore region. Restoring the Activity
        // here would therefore overwrite context legitimately established by the method body.
        if (UnsupportedAsyncCallback is { } unsupported)
        {
            IntegrationOptions<TIntegration, TTarget>.LogException(new CallTargetInvokerException(unsupported));
            return new CallTargetReturn<TReturn?>(returnValue);
        }

        if (OnAsyncMethodEnd is not null)
        {
            returnValue = OnAsyncMethodEnd(instance, returnValue, exception, in state);
        }

        if (OnMethodEnd is not null)
        {
            var declared = exception is null
                ? RuntimeAsyncHelper.CreateCompletedFromResult<TReturn, TDeclaredReturn>(returnValue)
                : default;
            var returned = OnMethodEnd(instance, declared, exception, in state).GetReturnValue();

            if (exception is null)
            {
                if (RuntimeAsyncHelper.TryGetCompletedResult<TReturn, TDeclaredReturn>(returned, out var unwrapped))
                {
                    returnValue = unwrapped;
                }
                else
                {
                    ReportUnsupportedSubstitution();
                }
            }
        }

        return new CallTargetReturn<TReturn?>(returnValue);
    }

    private static void ReportUnsupportedSubstitution()
    {
        if (_reportedUnsupportedSubstitution)
        {
            return;
        }

        _reportedUnsupportedSubstitution = true;
        IntegrationOptions<TIntegration, TTarget>.LogException(new NotSupportedException(
            $"Integration '{typeof(TIntegration).FullName}' returned a replacement {typeof(TDeclaredReturn).FullName} from OnMethodEnd that had not completed successfully. Runtime-async target '{typeof(TTarget).FullName}' cannot wait for that replacement, so the original result is used."));
    }
}

#endif
