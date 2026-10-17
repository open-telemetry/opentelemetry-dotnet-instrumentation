// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET
using OpenTelemetry.AutoInstrumentation.CallTarget;

namespace TestLibrary.InstrumentationTarget.StrongNamedValidation;

public static class RefStructValidator
{
    internal static CallTargetState OnMethodBegin<TTarget>(TTarget instance, ref CallTargetRefStruct value)
    {
        ref readonly var span = ref value.GetSpan<int>(out var isSpan);
        if (isSpan)
        {
            span[0]++;
            Console.WriteLine("CallTarget Span<int> validated.");
            return CallTargetState.GetDefault();
        }

        ref readonly var readOnlySpan = ref value.GetReadOnlySpan<int>(out var isReadOnlySpan);
        if (isReadOnlySpan && readOnlySpan[0] == 42)
        {
            Console.WriteLine("CallTarget ReadOnlySpan<int> validated.");
        }

        return CallTargetState.GetDefault();
    }
}
#endif
