// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.CompilerServices;
using My.Custom.Test.Namespace;

namespace TestApplication.ContinuousProfiler;

internal static class DeepStack
{
    // Keep recursion intact on both .NET and .NET Framework so the normal sampling workload exceeds the depth limit.
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static int Capture(int depth)
    {
        if (depth == 0)
        {
            ClassA.MethodA();
            return 0;
        }

        return Capture(depth - 1) + 1;
    }
}
