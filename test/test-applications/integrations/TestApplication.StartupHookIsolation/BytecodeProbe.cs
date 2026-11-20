// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.CompilerServices;

namespace TestApplication.StartupHookIsolation;

internal static class BytecodeProbe
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Run()
    {
        Console.WriteLine("[BYTECODE_PROBE] Executed.");
    }
}
