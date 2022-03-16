// <copyright file="TaskContinuationGenerator.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

using System;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace OpenTelemetry.AutoInstrumentation.CallTarget.Handlers.Continuations;

internal class TaskContinuationGenerator<TIntegration, TTarget, TReturn> : ContinuationGenerator<TTarget, TReturn>
{
    private static readonly Func<TTarget, object, Exception, CallTargetState, object> _continuation;
    private static readonly bool _preserveContext;

    static TaskContinuationGenerator()
    {
        var result = IntegrationMapper.CreateAsyncEndMethodDelegate(typeof(TIntegration), typeof(TTarget), typeof(object));
        if (result.Method != null)
        {
            _continuation = (Func<TTarget, object, Exception, CallTargetState, object>)result.Method.CreateDelegate(typeof(Func<TTarget, object, Exception, CallTargetState, object>));
            _preserveContext = result.PreserveContext;
        }
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

        return ToTReturn(ContinuationAction(previousTask, instance, state));
    }

    private static async Task ContinuationAction(Task previousTask, TTarget target, CallTargetState state)
    {
        if (!previousTask.IsCompleted)
        {
            await new NoThrowAwaiter(previousTask, _preserveContext);
        }

        Exception exception = null;

        if (previousTask.Status == TaskStatus.Faulted)
        {
            exception = previousTask.Exception.GetBaseException();
        }
        else if (previousTask.Status == TaskStatus.Canceled)
        {
            try
            {
                // The only supported way to extract the cancellation exception is to await the task
                await previousTask;
            }
            catch (Exception ex)
            {
                exception = ex;
            }
        }

        try
        {
            // *
            // Calls the CallTarget integration continuation, exceptions here should never bubble up to the application
            // *
            _continuation(target, null, exception, state);
        }
        catch (Exception ex)
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
