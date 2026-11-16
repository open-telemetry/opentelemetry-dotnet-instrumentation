// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.CompilerServices;

namespace TestLibrary.NativeAsync;

public sealed class ConventionalAsyncTarget
{
    private static readonly TimeSpan StaticAsyncDelay = TimeSpan.FromMilliseconds(100);
    private readonly TimeSpan _asyncDelay = TimeSpan.FromMilliseconds(100);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task TaskAsync()
    {
        await Task.Delay(StaticAsyncDelay).ConfigureAwait(false);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task<int> TaskOfTAsync()
    {
        await Task.Delay(_asyncDelay).ConfigureAwait(false);
        return 24;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async ValueTask ValueTaskAsync()
    {
        await Task.Delay(_asyncDelay).ConfigureAwait(false);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async ValueTask<string> ValueTaskOfTAsync()
    {
        await Task.Delay(_asyncDelay).ConfigureAwait(false);
        return "conventional";
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task FaultedTaskAsync()
    {
        await Task.Delay(_asyncDelay).ConfigureAwait(false);
        throw new InvalidOperationException("Conventional Task failure");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async ValueTask<string> FaultedValueTaskOfTAsync()
    {
        await Task.Delay(_asyncDelay).ConfigureAwait(false);
        throw new InvalidOperationException("Conventional ValueTask<T> failure");
    }
}
