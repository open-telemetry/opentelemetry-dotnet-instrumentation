// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.CompilerServices;

namespace TestApplication.NativeAsync;

internal sealed class RuntimeAsyncTarget
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
        return 42;
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
        return "runtime";
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional. This shape exercises metadata parsing.
    public async Task<int[,]> MultidimensionalArrayTaskAsync()
    {
        await Task.Delay(_asyncDelay).ConfigureAwait(false);
        return new int[1, 1] { { 11 } };
    }
#pragma warning restore CA1814 // Prefer jagged arrays over multidimensional

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task<T> GenericTaskAsync<T>(T result)
    {
        await Task.Delay(_asyncDelay).ConfigureAwait(false);
        return result;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task FaultedTaskAsync()
    {
        await Task.Delay(_asyncDelay).ConfigureAwait(false);
        throw new InvalidOperationException("Runtime Task failure");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task<int> FaultedTaskOfTAsync()
    {
        await Task.Delay(_asyncDelay).ConfigureAwait(false);
        throw new InvalidOperationException("Runtime Task<T> failure");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async ValueTask FaultedValueTaskAsync()
    {
        await Task.Delay(_asyncDelay).ConfigureAwait(false);
        throw new InvalidOperationException("Runtime ValueTask failure");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async ValueTask<string> FaultedValueTaskOfTAsync()
    {
        await Task.Delay(_asyncDelay).ConfigureAwait(false);
        throw new InvalidOperationException("Runtime ValueTask<T> failure");
    }
}
