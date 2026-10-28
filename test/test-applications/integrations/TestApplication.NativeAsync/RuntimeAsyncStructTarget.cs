// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace TestApplication.NativeAsync;

internal readonly struct RuntimeAsyncStructTarget
{
    private static readonly TimeSpan AsyncDelay = TimeSpan.FromMilliseconds(100);
    private readonly int _seed;

    public RuntimeAsyncStructTarget(int seed)
    {
        _seed = seed;
    }

    internal static bool WasInstrumented { get; private set; }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task<int> TaskOfTAsync()
    {
        WasInstrumented = Activity.Current is { DisplayName: "RuntimeAsync-StructTaskOfT" } activity &&
                          activity.Source.Name == "OpenTelemetry.AutoInstrumentation.NoCode";

        // Copy the field before suspension so the target method itself does not read its byref `this` afterwards.
        var seed = _seed;
        await Task.Delay(AsyncDelay).ConfigureAwait(false);
        return seed + 1;
    }
}
