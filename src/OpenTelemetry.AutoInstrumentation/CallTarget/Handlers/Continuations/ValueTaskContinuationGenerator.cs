// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Logging;

namespace OpenTelemetry.AutoInstrumentation.CallTarget.Handlers.Continuations;

#if NET
internal class ValueTaskContinuationGenerator<TIntegration, TTarget, TReturn> : ContinuationGenerator<TTarget, TReturn>
{
    private static readonly CallbackHandler Resolver;

#pragma warning disable CA1810 // Initialize reference type static fields inline
    static ValueTaskContinuationGenerator()
#pragma warning restore CA1810 // Initialize reference type static fields inline
    {
        var result = IntegrationMapper.CreateAsyncEndMethodDelegate(typeof(TIntegration), typeof(TTarget), typeof(object));
        if (result.Method is not null)
        {
            if (result.Method.ReturnType == typeof(Task) ||
                (result.Method.ReturnType.IsGenericType && typeof(Task).IsAssignableFrom(result.Method.ReturnType)))
            {
                var asyncContinuation = (AsyncObjectContinuationMethodDelegate)result.Method.CreateDelegate(typeof(AsyncObjectContinuationMethodDelegate));
                Resolver = new AsyncCallbackHandler(asyncContinuation, result.PreserveContext);
            }
            else
            {
                var continuation = (ObjectContinuationMethodDelegate)result.Method.CreateDelegate(typeof(ObjectContinuationMethodDelegate));
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
        private readonly ObjectContinuationMethodDelegate _continuation;
        private readonly bool _preserveContext;

        public SyncCallbackHandler(ObjectContinuationMethodDelegate continuation, bool preserveContext)
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

            var previousValueTask = FromTReturn<ValueTask>(returnValue);
#pragma warning disable CA2012 // ValueTask is intentionally wrapped back into the instrumented method's return type.
            return ToTReturn(ContinuationAction(previousValueTask, instance, state));
#pragma warning restore CA2012 // ValueTask is intentionally wrapped back into the instrumented method's return type.
        }

        private async ValueTask ContinuationAction(ValueTask previousValueTask, TTarget target, CallTargetState state)
        {
            try
            {
                await previousValueTask.ConfigureAwait(_preserveContext);
            }
            catch (Exception ex)
            {
                try
                {
                    // *
                    // Calls the CallTarget integration continuation, exceptions here should never bubble up to the application
                    // *
                    _continuation(target, default, ex, in state);
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
                _continuation(target, default, default, in state);
            }
            catch (Exception contEx)
            {
                IntegrationOptions<TIntegration, TTarget>.LogException(contEx, "Exception occurred when calling the CallTarget integration continuation.");
            }
        }
    }

    private class AsyncCallbackHandler : CallbackHandler
    {
        private readonly AsyncObjectContinuationMethodDelegate _asyncContinuation;
        private readonly bool _preserveContext;

        public AsyncCallbackHandler(AsyncObjectContinuationMethodDelegate asyncContinuation, bool preserveContext)
        {
            _asyncContinuation = asyncContinuation;
            _preserveContext = preserveContext;
        }

        public override TReturn? ExecuteCallback(TTarget instance, TReturn? returnValue, Exception? exception, in CallTargetState state)
        {
            var previousValueTask = returnValue == null ? default : FromTReturn<ValueTask>(returnValue);
#pragma warning disable CA2012 // ValueTask is intentionally wrapped back into the instrumented method's return type.
            return ToTReturn(ContinuationAction(previousValueTask, instance, state, exception));
#pragma warning restore CA2012 // ValueTask is intentionally wrapped back into the instrumented method's return type.
        }

        private async ValueTask ContinuationAction(ValueTask previousValueTask, TTarget target, CallTargetState state, Exception? exception)
        {
            if (exception != null)
            {
                await _asyncContinuation(target, default, exception, in state).ConfigureAwait(_preserveContext);
            }

            try
            {
                await previousValueTask.ConfigureAwait(_preserveContext);
            }
            catch (Exception ex)
            {
                try
                {
                    // *
                    // Calls the CallTarget integration continuation, exceptions here should never bubble up to the application
                    // *
                    await _asyncContinuation(target, default, ex, in state).ConfigureAwait(_preserveContext);
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
                await _asyncContinuation(target, default, default, in state).ConfigureAwait(_preserveContext);
            }
            catch (Exception contEx)
            {
                IntegrationOptions<TIntegration, TTarget>.LogException(contEx, "Exception occurred when calling the CallTarget integration continuation.");
            }
        }
    }
}
#endif
