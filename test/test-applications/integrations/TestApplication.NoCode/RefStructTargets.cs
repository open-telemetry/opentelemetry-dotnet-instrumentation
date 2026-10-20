// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.CompilerServices;

namespace TestApplication.NoCode;

internal static class RefStructTargets
{
    public static void Invoke()
    {
        var value = new RefStructValue(42);
        UseValue(value);
        UseByRefValue(ref value);
        UseMixedValues(value, 42, ref value);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void UseValue(RefStructValue value) => Console.WriteLine(value.Value);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void UseByRefValue(ref RefStructValue value) => Console.WriteLine(value.Value);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void UseMixedValues(RefStructValue first, int marker, ref RefStructValue second)
        => Console.WriteLine(first.Value + marker + second.Value);
}
