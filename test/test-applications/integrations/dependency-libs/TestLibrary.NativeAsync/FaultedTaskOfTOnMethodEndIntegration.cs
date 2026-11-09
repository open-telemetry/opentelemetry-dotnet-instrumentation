// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.CallTarget;

namespace TestLibrary.NativeAsync;

public static class FaultedTaskOfTOnMethodEndIntegration
{
    private static Task<int>? _returnValue;

    public static int Calls { get; private set; }

    public static bool ExceptionWasNull { get; private set; }

    public static bool ReceivedTaskIsFaulted => _returnValue?.IsFaulted == true;

    internal static CallTargetReturn<Task<int>> OnMethodEnd<TTarget>(TTarget instance, Task<int> returnValue, Exception? exception, in CallTargetState state)
    {
        Calls++;
        ExceptionWasNull = exception is null;
        _returnValue = returnValue;
        return new CallTargetReturn<Task<int>>(returnValue);
    }
}
