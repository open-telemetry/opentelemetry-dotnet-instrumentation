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
    public static int ReturningTestMethodStatic()
    {
        return 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
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
    public int ReturningTestMethod()
    {
        return 0;
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
        await Task.Yield();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task TestMethodAsync()
    {
        await Task.Yield();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task TestMethodAAsync()
    {
        await Task.Yield();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task TestMethodAsync(string param1)
    {
        await Task.Yield();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task TestMethodAsync(int param1)
    {
        await Task.Yield();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task TestMethodAsync(string param1, string param2)
    {
        await Task.Yield();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task TestMethodAsync(string param1, string param2, string param3)
    {
        await Task.Yield();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task TestMethodAsync(string param1, string param2, string param3, string param4)
    {
        await Task.Yield();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task TestMethodAsync(string param1, string param2, string param3, string param4, string param5)
    {
        await Task.Yield();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task TestMethodAsync(string param1, string param2, string param3, string param4, string param5, string param6)
    {
        await Task.Yield();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task TestMethodAsync(string param1, string param2, string param3, string param4, string param5, string param6, string param7)
    {
        await Task.Yield();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task TestMethodAsync(string param1, string param2, string param3, string param4, string param5, string param6, string param7, string param8)
    {
        await Task.Yield();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task TestMethodAsync(string param1, string param2, string param3, string param4, string param5, string param6, string param7, string param8, string param9)
    {
        await Task.Yield();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task<int> IntTaskTestMethodAsync()
    {
        await Task.Yield();
        return 0;
    }

#if NET
    [MethodImpl(MethodImplOptions.NoInlining)]
    public async ValueTask ValueTaskTestMethodAsync()
    {
        await Task.Yield();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async ValueTask<int> IntValueTaskTestMethodAsync()
    {
        await Task.Yield();
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
        await Task.Yield();
        return default;
    }
}
