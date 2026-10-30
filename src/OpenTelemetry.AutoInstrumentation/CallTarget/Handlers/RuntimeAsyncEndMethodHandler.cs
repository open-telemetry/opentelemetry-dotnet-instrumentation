// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET

using System.Runtime.CompilerServices;
using OpenTelemetry.AutoInstrumentation.CallTarget.Handlers.Continuations;

namespace OpenTelemetry.AutoInstrumentation.CallTarget.Handlers;

internal static class RuntimeAsyncEndMethodHandler<TIntegration, TTarget, TDeclaredReturn>
{
    private static readonly ContinuationGenerator<TTarget, object>.ObjectContinuationMethodDelegate? OnAsyncMethodEnd;
    private static readonly EndMethodHandler<TIntegration, TTarget, TDeclaredReturn>.InvokeDelegate? OnMethodEnd;
    private static readonly Exception? IncompatibleTaskReturningCallback;
    private static readonly TDeclaredReturn? CompletedValue;

    private static int _reportedIncompatibleCallback;
    private static bool _reportedUnsupportedSubstitution;

#pragma warning disable CA1810 // Static initialization may throw and must be wrapped for CallTarget.
    static RuntimeAsyncEndMethodHandler()
#pragma warning restore CA1810 // Static initialization may throw and must be wrapped for CallTarget.
    {
        try
        {
            var asyncResult = IntegrationMapper.CreateAsyncEndMethodDelegate(typeof(TIntegration), typeof(TTarget), typeof(object));
            if (asyncResult.Method is { } asyncMethod)
            {
                if (asyncResult.IsTaskReturn)
                {
                    IncompatibleTaskReturningCallback = new NotSupportedException(
                        $"Task-returning OnAsyncMethodEnd on integration '{typeof(TIntegration).FullName}' is incompatible with runtime-async instrumentation for target '{typeof(TTarget).FullName}': the injected finally block cannot await the callback. The integration will be disabled for this target.");
                }
                else
                {
                    OnAsyncMethodEnd = (ContinuationGenerator<TTarget, object>.ObjectContinuationMethodDelegate)asyncMethod.CreateDelegate(typeof(ContinuationGenerator<TTarget, object>.ObjectContinuationMethodDelegate));
                }
            }

            if (IntegrationMapper.CreateEndMethodDelegate(typeof(TIntegration), typeof(TTarget), typeof(TDeclaredReturn)) is { } endMethod)
            {
                OnMethodEnd = (EndMethodHandler<TIntegration, TTarget, TDeclaredReturn>.InvokeDelegate)endMethod.CreateDelegate(typeof(EndMethodHandler<TIntegration, TTarget, TDeclaredReturn>.InvokeDelegate));
                CompletedValue = RuntimeAsyncHelper.CreateCompleted<TDeclaredReturn>();
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
    internal static bool EnsureCallbacksSupported()
    {
        if (IncompatibleTaskReturningCallback is not { } incompatible)
        {
            return true;
        }

        IntegrationOptions<TIntegration, TTarget>.DisableIntegration();
        if (System.Threading.Interlocked.Exchange(ref _reportedIncompatibleCallback, 1) == 0)
        {
            IntegrationOptions<TIntegration, TTarget>.LogException(new CallTargetInvokerException(incompatible));
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static CallTargetReturn Invoke(TTarget? instance, Exception? exception, in CallTargetState state)
    {
        // Unlike compiler-lowered async, runtime async has no state-machine stub whose prologue
        // runs outside the runtime's ExecutionContext save/restore region. Restoring the Activity
        // here would therefore overwrite context legitimately established by the method body.
        if (!EnsureCallbacksSupported())
        {
            return CallTargetReturn.GetDefault();
        }

        OnAsyncMethodEnd?.Invoke(instance, null, exception, in state);

        if (OnMethodEnd is not null)
        {
            // Match the declared async contract seen by conventional CallTarget instrumentation:
            // OnMethodEnd receives an awaitable carrying the completion state, while failures are
            // observed through OnAsyncMethodEnd rather than the synchronous exception argument.
            var handed = exception is null
                ? CompletedValue
                : RuntimeAsyncHelper.CreateFromException<TDeclaredReturn>(exception);
            var returned = OnMethodEnd(instance, handed, null, in state).GetReturnValue();
            if (exception is null && !RuntimeAsyncHelper.IsUnchanged(returned, handed))
            {
                ReportUnsupportedSubstitution();
            }
        }

        return CallTargetReturn.GetDefault();
    }

    private static void ReportUnsupportedSubstitution()
    {
        if (_reportedUnsupportedSubstitution)
        {
            return;
        }

        _reportedUnsupportedSubstitution = true;
        IntegrationOptions<TIntegration, TTarget>.LogException(new NotSupportedException(
            $"Integration '{typeof(TIntegration).FullName}' returned a replacement {typeof(TDeclaredReturn).FullName} from OnMethodEnd, but the non-generic runtime-async target '{typeof(TTarget).FullName}' has no return slot in which to apply it. The original result is used."));
    }
}

#endif
