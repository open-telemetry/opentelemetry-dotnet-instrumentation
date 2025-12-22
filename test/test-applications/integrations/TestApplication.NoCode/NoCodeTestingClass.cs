// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.CompilerServices;

namespace TestApplication.NoCode;

internal class NoCodeTestingClass
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void TestMethodStatic()
    {
        // This method is intentionally left empty.
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
#pragma warning disable CA1822 // Mark members as static
    public void TestMethod()
    {
        // This method is intentionally left empty.
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void TestMethodA()
    {
        // This method is intentionally left empty.
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void TestMethod(string param1)
    {
        // This method is intentionally left empty.
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void TestMethod(int param1)
    {
        // This method is intentionally left empty.
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void TestMethod(string param1, string param2)
    {
        // This method is intentionally left empty.
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void TestMethod(string param1, string param2, string param3)
    {
        // This method is intentionally left empty.
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void TestMethod(string param1, string param2, string param3, string param4)
    {
        // This method is intentionally left empty.
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void TestMethod(string param1, string param2, string param3, string param4, string param5)
    {
        // This method is intentionally left empty.
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void TestMethod(string param1, string param2, string param3, string param4, string param5, string param6)
    {
        // This method is intentionally left empty.
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void TestMethod(string param1, string param2, string param3, string param4, string param5, string param6, string param7)
    {
        // This method is intentionally left empty.
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void TestMethod(string param1, string param2, string param3, string param4, string param5, string param6, string param7, string param8)
    {
        // This method is intentionally left empty.
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void TestMethod(string param1, string param2, string param3, string param4, string param5, string param6, string param7, string param8, string param9)
    {
        // This method is intentionally left empty.
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
#pragma warning disable SA1204 // Static elements should appear before instance elements
    public static int ReturningTestMethodStatic()
#pragma warning restore SA1204 // Static elements should appear before instance elements
    {
        return 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public int ReturningTestMethod()
    {
        return 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public string ReturningStringTestMethod()
    {
        return string.Empty;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public TestClass ReturningCustomClassTestMethod()
    {
        return new TestClass();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public int ReturningTestMethod(string param1)
    {
        return 1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public int ReturningTestMethod(int param1)
    {
        return 1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public int ReturningTestMethod(string param1, string param2)
    {
        return 2;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public int ReturningTestMethod(string param1, string param2, string param3)
    {
        return 3;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public int ReturningTestMethod(string param1, string param2, string param3, string param4)
    {
        return 4;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public int ReturningTestMethod(string param1, string param2, string param3, string param4, string param5)
    {
        return 5;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public int ReturningTestMethod(string param1, string param2, string param3, string param4, string param5, string param6)
    {
        return 6;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public int ReturningTestMethod(string param1, string param2, string param3, string param4, string param5, string param6, string param7)
    {
        return 7;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public int ReturningTestMethod(string param1, string param2, string param3, string param4, string param5, string param6, string param7, string param8)
    {
        return 8;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public int ReturningTestMethod(string param1, string param2, string param3, string param4, string param5, string param6, string param7, string param8, string param9)
    {
        return 9;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
#pragma warning disable SA1204 // Static elements should appear before instance elements
    public static async Task TestMethodStaticAsync()
#pragma warning restore SA1204 // Static elements should appear before instance elements
    {
        await Task.Delay(TimeSpan.FromMilliseconds(100)).ConfigureAwait(false);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task TestMethodAsync()
    {
        await Task.Delay(TimeSpan.FromMilliseconds(100)).ConfigureAwait(false);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task TestMethodAAsync()
    {
        await Task.Delay(TimeSpan.FromMilliseconds(100)).ConfigureAwait(false);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task TestMethodAsync(string param1)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(100)).ConfigureAwait(false);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task TestMethodAsync(int param1)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(100)).ConfigureAwait(false);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task TestMethodAsync(string param1, string param2)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(100)).ConfigureAwait(false);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task TestMethodAsync(string param1, string param2, string param3)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(100)).ConfigureAwait(false);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task TestMethodAsync(string param1, string param2, string param3, string param4)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(100)).ConfigureAwait(false);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task TestMethodAsync(string param1, string param2, string param3, string param4, string param5)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(100)).ConfigureAwait(false);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task TestMethodAsync(string param1, string param2, string param3, string param4, string param5, string param6)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(100)).ConfigureAwait(false);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task TestMethodAsync(string param1, string param2, string param3, string param4, string param5, string param6, string param7)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(100)).ConfigureAwait(false);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task TestMethodAsync(string param1, string param2, string param3, string param4, string param5, string param6, string param7, string param8)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(100)).ConfigureAwait(false);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task TestMethodAsync(string param1, string param2, string param3, string param4, string param5, string param6, string param7, string param8, string param9)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(100)).ConfigureAwait(false);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task<int> IntTaskTestMethodAsync()
    {
        await Task.Delay(TimeSpan.FromMilliseconds(100)).ConfigureAwait(false);
        return 0;
    }

#if NET
    [MethodImpl(MethodImplOptions.NoInlining)]
    public async ValueTask ValueTaskTestMethodAsync()
    {
        await Task.Delay(TimeSpan.FromMilliseconds(100)).ConfigureAwait(false);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async ValueTask<int> IntValueTaskTestMethodAsync()
    {
        await Task.Delay(TimeSpan.FromMilliseconds(100)).ConfigureAwait(false);
        return 0;
    }
#endif

    [MethodImpl(MethodImplOptions.NoInlining)]
    public T? GenericTestMethod<T>()
    {
        return default;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task<T?> GenericTestMethodAsync<T>()
    {
        await Task.Delay(TimeSpan.FromMilliseconds(100)).ConfigureAwait(false);
        return default;
    }
#pragma warning restore CA1822 // Mark members as static
}
