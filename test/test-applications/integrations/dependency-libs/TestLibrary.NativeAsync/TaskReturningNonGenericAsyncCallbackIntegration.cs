// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.CallTarget;

namespace TestLibrary.NativeAsync;

public static class TaskReturningNonGenericAsyncCallbackIntegration
{
    public static int BeginCalls { get; private set; }

    public static int AsyncCalls { get; private set; }

    internal static CallTargetState OnMethodBegin<TTarget>(TTarget instance)
    {
        BeginCalls++;
        return CallTargetState.GetDefault();
    }

    internal static Task<object?> OnAsyncMethodEnd<TTarget>(TTarget instance, object? returnValue, Exception? exception, in CallTargetState state)
    {
        AsyncCalls++;
        return Task.FromResult(returnValue);
    }
}
