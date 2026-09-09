// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Logging;

#pragma warning disable SA1649 // File name must match first type name

namespace OpenTelemetry.AutoInstrumentation.CallTarget.Handlers.Continuations;

#if NET
internal class ValueTaskContinuationGenerator<TIntegration, TTarget, TReturn, TResult> : ContinuationGenerator<TTarget, TReturn, TResult>
{
    private static readonly CallbackHandler Resolver;

#pragma warning disable CA1810 // Initialize reference type static fields inline
    static ValueTaskContinuationGenerator()
#pragma warning restore CA1810 // Initialize reference type static fields inline
    {
        var result = IntegrationMapper.CreateAsyncEndMethodDelegate(typeof(TIntegration), typeof(TTarget), typeof(TResult));
        if (result.Method is not null)
        {
            if (result.Method.ReturnType == typeof(Task) ||
                (result.Method.ReturnType.IsGenericType && typeof(Task).IsAssignableFrom(result.Method.ReturnType)))
            {
                var asyncContinuation = (AsyncContinuationMethodDelegate)result.Method.CreateDelegate(typeof(AsyncContinuationMethodDelegate));
                Resolver = new AsyncCallbackHandler(asyncContinuation, result.PreserveContext);
            }
            else
            {
                var continuation = (ContinuationMethodDelegate)result.Method.CreateDelegate(typeof(ContinuationMethodDelegate));
                Resolver = new SyncCallbackHandler(continuation, result.PreserveContext);
            }
        }
        else
        {
            Resolver = new NoOpCallbackHandler();
        }

        if (Log.IsEnabled(LogLevel.Debug))
        {
            Log.Debug($"== TaskContinuationGenerator<{typeof(TIntegration).FullName}, {typeof(TTarget).FullName}, {typeof(TReturn).FullName}> using Resolver: {Resolver.GetType().FullName}");
        }
    }

    public override TReturn? SetContinuation(TTarget instance, TReturn? returnValue, Exception? exception, in CallTargetState state)
    {
        return Resolver.ExecuteCallback(instance, returnValue, exception, in state);
    }

    private class SyncCallbackHandler : CallbackHandler
    {
        private readonly ContinuationMethodDelegate _continuation;
        private readonly bool _preserveContext;

        public SyncCallbackHandler(ContinuationMethodDelegate continuation, bool preserveContext)
        {
            _continuation = continuation;
            _preserveContext = preserveContext;
        }

        public override TReturn? ExecuteCallback(TTarget instance, TReturn? returnValue, Exception? exception, in CallTargetState state)
        {
            if (exception != null)
            {
                _continuation(instance, default, exception, in state);
                return returnValue;
            }

            var previousValueTask = FromTReturn<ValueTask<TResult>>(returnValue);
#pragma warning disable CA2012 // ValueTask is intentionally wrapped back into the instrumented method's return type.
            return ToTReturn(ContinuationAction(previousValueTask, instance, state));
#pragma warning restore CA2012 // ValueTask is intentionally wrapped back into the instrumented method's return type.
        }

        private async ValueTask<TResult?> ContinuationAction(ValueTask<TResult> previousValueTask, TTarget target, CallTargetState state)
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
                    _continuation(target, result, ex, in state);
                }
                catch (Exception contEx)
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
                return _continuation(target, result, null, in state);
            }
            catch (Exception contEx)
            {
                IntegrationOptions<TIntegration, TTarget>.LogException(contEx, "Exception occurred when calling the CallTarget integration continuation.");
            }

            return result;
        }
    }

    private class AsyncCallbackHandler : CallbackHandler
    {
        private readonly AsyncContinuationMethodDelegate _asyncContinuation;
        private readonly bool _preserveContext;

        public AsyncCallbackHandler(AsyncContinuationMethodDelegate asyncContinuation, bool preserveContext)
        {
            _asyncContinuation = asyncContinuation;
            _preserveContext = preserveContext;
        }

        public override TReturn? ExecuteCallback(TTarget instance, TReturn? returnValue, Exception? exception, in CallTargetState state)
        {
            var previousValueTask = FromTReturn<ValueTask<TResult>>(returnValue);
#pragma warning disable CA2012 // ValueTask is intentionally wrapped back into the instrumented method's return type.
            return ToTReturn(ContinuationAction(previousValueTask, instance, state, exception));
#pragma warning restore CA2012 // ValueTask is intentionally wrapped back into the instrumented method's return type.
        }

        private async ValueTask<TResult?> ContinuationAction(ValueTask<TResult> previousValueTask, TTarget target, CallTargetState state, Exception? exception)
        {
            if (exception != null)
            {
                return await _asyncContinuation(target, default, exception, in state).ConfigureAwait(_preserveContext);
            }

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
                    await _asyncContinuation(target, result, ex, in state).ConfigureAwait(_preserveContext);
                }
                catch (Exception contEx)
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
                return await _asyncContinuation(target, result, null, in state).ConfigureAwait(_preserveContext);
            }
            catch (Exception contEx)
            {
                IntegrationOptions<TIntegration, TTarget>.LogException(contEx, "Exception occurred when calling the CallTarget integration continuation.");
            }

            return result;
        }
    }
}
#endif
