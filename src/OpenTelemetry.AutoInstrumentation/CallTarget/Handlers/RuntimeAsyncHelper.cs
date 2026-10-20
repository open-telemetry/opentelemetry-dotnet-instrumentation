// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET

using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace OpenTelemetry.AutoInstrumentation.CallTarget.Handlers;

internal static class RuntimeAsyncHelper
{
    internal static TDeclaredReturn? CreateCompleted<TDeclaredReturn>()
    {
        if (typeof(TDeclaredReturn) == typeof(Task))
        {
            var completed = Task.CompletedTask;
            return Unsafe.As<Task, TDeclaredReturn>(ref completed);
        }

        // default(ValueTask) is already completed successfully.
        return default;
    }

    internal static TDeclaredReturn? CreateCompletedFromResult<TResult, TDeclaredReturn>(TResult? result)
    {
        if (typeof(TDeclaredReturn) == typeof(Task<TResult>))
        {
            var completed = Task.FromResult(result!);
            return Unsafe.As<Task<TResult>, TDeclaredReturn>(ref completed);
        }

        if (typeof(TDeclaredReturn) == typeof(ValueTask<TResult>))
        {
            var completed = new ValueTask<TResult>(result!);
            return Unsafe.As<ValueTask<TResult>, TDeclaredReturn>(ref completed);
        }

        return default;
    }

    internal static bool TryGetCompletedResult<TResult, TDeclaredReturn>(TDeclaredReturn? declared, out TResult? result)
    {
        if (typeof(TDeclaredReturn) == typeof(Task<TResult>))
        {
            if (declared is not null)
            {
                var task = Unsafe.As<TDeclaredReturn, Task<TResult>>(ref declared);
                if (task.IsCompletedSuccessfully)
                {
                    result = task.Result;
                    return true;
                }
            }
        }
        else if (typeof(TDeclaredReturn) == typeof(ValueTask<TResult>))
        {
            var valueTask = Unsafe.As<TDeclaredReturn, ValueTask<TResult>>(ref declared!);
            if (valueTask.IsCompletedSuccessfully)
            {
                result = valueTask.Result;
                return true;
            }
        }

        result = default;
        return false;
    }

    internal static bool IsUnchanged<TDeclaredReturn>(TDeclaredReturn? returned, TDeclaredReturn? original)
        => EqualityComparer<TDeclaredReturn?>.Default.Equals(returned, original);
}

#endif
