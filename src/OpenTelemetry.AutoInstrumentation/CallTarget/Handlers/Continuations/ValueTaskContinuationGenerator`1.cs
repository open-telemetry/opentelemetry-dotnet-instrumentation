// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#pragma warning disable SA1649 // File name must match first type name

namespace OpenTelemetry.AutoInstrumentation.CallTarget.Handlers.Continuations;

#if NET
internal class ValueTaskContinuationGenerator<TIntegration, TTarget, TReturn, TResult> : ContinuationGenerator<TTarget, TReturn>
{
    private static readonly ContinuationMethodDelegate? _continuation;
    private static readonly bool _preserveContext;

    static ValueTaskContinuationGenerator()
    {
        var result = IntegrationMapper.CreateAsyncEndMethodDelegate(typeof(TIntegration), typeof(TTarget), typeof(TResult));
        if (result.Method != null)
        {
            _continuation = (ContinuationMethodDelegate)result.Method.CreateDelegate(typeof(ContinuationMethodDelegate));
            _preserveContext = result.PreserveContext;
        }
    }

    internal delegate TResult ContinuationMethodDelegate(TTarget target, TResult? returnValue, Exception? exception, in CallTargetState state);

    public override TReturn? SetContinuation(TTarget instance, TReturn? returnValue, Exception? exception, in CallTargetState state)
    {
        if (_continuation is null)
        {
            return returnValue;
        }

        if (exception != null)
        {
            _continuation(instance, default, exception, in state);
            return returnValue;
        }

        var previousValueTask = FromTReturn<ValueTask<TResult>>(returnValue);
        return ToTReturn(InnerSetValueTaskContinuation(instance, previousValueTask, state));

        static async ValueTask<TResult?> InnerSetValueTaskContinuation(TTarget instance, ValueTask<TResult> previousValueTask, CallTargetState state)
        {
            TResult? result = default;
            try
            {
                result = await previousValueTask.ConfigureAwait(_preserveContext);
            }
            catch (Exception ex)
            {
                try
                {
                    // *
                    // Calls the CallTarget integration continuation, exceptions here should never bubble up to the application
                    // *
                    _continuation!(instance, result, ex, in state);
                }
#pragma warning disable CA1031 // Do not catch general exception types. Ignored to handle continuation task.
                catch (Exception contEx)
#pragma warning restore CA1031 // Do not catch general exception types. Ignored to handle continuation task.
                {
                    IntegrationOptions<TIntegration, TTarget>.LogException(contEx, "Exception occurred when calling the CallTarget integration continuation.");
                }

                throw;
            }

            try
            {
                // *
                // Calls the CallTarget integration continuation, exceptions here should never bubble up to the application
                // *
                return _continuation!(instance, result, null, in state);
            }
#pragma warning disable CA1031 // Do not catch general exception types. Ignored to handle continuation task.
            catch (Exception contEx)
#pragma warning restore CA1031 // Do not catch general exception types. Ignored to handle continuation task.
            {
                IntegrationOptions<TIntegration, TTarget>.LogException(contEx, "Exception occurred when calling the CallTarget integration continuation.");
            }

            return result;
        }
    }
}
#endif
