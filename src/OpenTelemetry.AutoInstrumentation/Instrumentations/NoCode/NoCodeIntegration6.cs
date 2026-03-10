// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.CallTarget;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.NoCode;

/// <summary>
/// NoCode calltarget instrumentation
/// </summary>
public static class NoCodeIntegration6
{
    internal static CallTargetState OnMethodBegin<TTarget, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(TTarget instance, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6)
    {
        return NoCodeIntegrationHelper.OnMethodBegin();
    }

    internal static CallTargetReturn OnMethodEnd<TTarget>(TTarget instance, Exception? exception, in CallTargetState state)
    {
        // handles void methods
        return NoCodeIntegrationHelper.OnMethodEnd(exception, in state);
    }

    internal static CallTargetReturn<TReturn> OnMethodEnd<TTarget, TReturn>(TTarget instance, TReturn returnValue, Exception? exception, in CallTargetState state)
    {
        // handles non-void, sync methods
        return NoCodeIntegrationHelper.OnMethodEnd(returnValue, exception, in state);
    }

    internal static TReturn OnAsyncMethodEnd<TTarget, TReturn>(TTarget instance, TReturn returnValue, Exception exception, in CallTargetState state)
    {
        // handles non-void, async methods
        return NoCodeIntegrationHelper.OnAsyncMethodEnd(returnValue, exception, in state);
    }
}
