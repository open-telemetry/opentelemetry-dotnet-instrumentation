using System;
using System.Reflection.Emit;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace Datadog.Trace.ClrProfiler.CallTarget.Handlers.Continuations
{
    internal class TaskContinuationGenerator<TIntegration, TTarget, TReturn> : ContinuationGenerator<TTarget, TReturn>
    {
        private static readonly Func<TTarget, object, Exception, CallTargetState, object> _continuation;
        private static readonly Action<Task, object> _continuationAction;

        static TaskContinuationGenerator()
        {
            DynamicMethod continuationMethod = IntegrationMapper.CreateAsyncEndMethodDelegate(typeof(TIntegration), typeof(TTarget), typeof(object));
            if (continuationMethod != null)
            {
                _continuation = (Func<TTarget, object, Exception, CallTargetState, object>)continuationMethod.CreateDelegate(typeof(Func<TTarget, object, Exception, CallTargetState, object>));
            }

            _continuationAction = new Action<Task, object>(ContinuationAction);
        }

        public override TReturn SetContinuation(TTarget instance, TReturn returnValue, Exception exception, CallTargetState state)
        {
            if (_continuation == null)
            {
                return returnValue;
            }

            if (exception != null || returnValue == null)
            {
                _continuation(instance, default, exception, state);
                return returnValue;
            }

            Task previousTask = FromTReturn<Task>(returnValue);
            if (previousTask.Status == TaskStatus.RanToCompletion)
            {
                _continuation(instance, default, null, state);
                return returnValue;
            }

            var continuationState = new ContinuationGeneratorState<TTarget>(instance, state);
            return ToTReturn(previousTask.ContinueWith(
                _continuationAction,
                continuationState,
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Current));
        }

        private static void ContinuationAction(Task previousTask, object state)
        {
            try
            {
                ContinuationGeneratorState<TTarget> contState = (ContinuationGeneratorState<TTarget>)state;

                // *
                // Calls the CallTarget integration continuation, exceptions here should never bubble up to the application
                // *
                _continuation(contState.Target, null, previousTask.Exception, contState.State);
            }
            catch (Exception ex)
            {
                IntegrationOptions<TIntegration, TTarget>.LogException(ex, "Exception occurred when calling the CallTarget integration continuation.");
            }

            if (previousTask.Exception is not null)
            {
                // *
                // If the original task throws an exception we rethrow it here.
                // *
                ExceptionDispatchInfo.Capture(previousTask.Exception.GetBaseException()).Throw();
            }
        }
    }
}
