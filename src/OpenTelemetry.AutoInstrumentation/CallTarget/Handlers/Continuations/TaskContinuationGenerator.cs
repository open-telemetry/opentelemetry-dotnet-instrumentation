// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.ExceptionServices;

namespace OpenTelemetry.AutoInstrumentation.CallTarget.Handlers.Continuations;

internal class TaskContinuationGenerator<TIntegration, TTarget, TReturn> : ContinuationGenerator<TTarget, TReturn>
{
    private static readonly ContinuationMethodDelegate? _continuation;
    private static readonly bool _preserveContext;

    static TaskContinuationGenerator()
    {
        var result = IntegrationMapper.CreateAsyncEndMethodDelegate(typeof(TIntegration), typeof(TTarget), typeof(object));
        if (result.Method != null)
        {
            _continuation = (ContinuationMethodDelegate)result.Method.CreateDelegate(typeof(ContinuationMethodDelegate));
            _preserveContext = result.PreserveContext;
        }
    }

    internal delegate object? ContinuationMethodDelegate(TTarget target, object? returnValue, Exception? exception, in CallTargetState state);

    public override TReturn? SetContinuation(TTarget instance, TReturn? returnValue, Exception? exception, in CallTargetState state)
    {
        if (_continuation == null)
        {
            return returnValue;
        }

        if (exception != null || returnValue == null)
        {
            _continuation(instance, default, exception, in state);
            return returnValue;
        }

        var previousTask = FromTReturn<Task>(returnValue);
        if (previousTask.Status == TaskStatus.RanToCompletion)
        {
            _continuation(instance, default, null, in state);
            return returnValue;
        }

        return ToTReturn(ContinuationAction(previousTask, instance, state));
    }

    private static async Task ContinuationAction(Task previousTask, TTarget target, CallTargetState state)
    {
        if (!previousTask.IsCompleted)
        {
            await new NoThrowAwaiter(previousTask, _preserveContext);
        }

        Exception? exception = null;

        if (previousTask.Status == TaskStatus.Faulted)
        {
            exception = previousTask.Exception!.GetBaseException();
        }
        else if (previousTask.Status == TaskStatus.Canceled)
        {
            try
            {
                // The only supported way to extract the cancellation exception is to await the task
                await previousTask.ConfigureAwait(_preserveContext);
            }
#pragma warning disable CA1031 // Do not catch general exception types. Ignored to handle continuation task.
            catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types. Ignored to handle continuation task.
            {
                exception = ex;
            }
        }

        try
        {
            // *
            // Calls the CallTarget integration continuation, exceptions here should never bubble up to the application
            // *
            _continuation!(target, null, exception, in state);
        }
#pragma warning disable CA1031 // Do not catch general exception types. Ignored to handle continuation task.
        catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types. Ignored to handle continuation task.
        {
            IntegrationOptions<TIntegration, TTarget>.LogException(ex, "Exception occurred when calling the CallTarget integration continuation.");
        }

        // *
        // If the original task throws an exception we rethrow it here.
        // *
        if (exception != null)
        {
            ExceptionDispatchInfo.Capture(exception).Throw();
        }
    }
}
