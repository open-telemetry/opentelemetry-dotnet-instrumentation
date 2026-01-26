// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using TestApplication.Shared;

namespace TestApplication.NoCode;

internal static class Program
{
    public static async Task Main(string[] args)
    {
        ConsoleHelper.WriteSplashScreen(args);

        var noCodeTestingClass = new NoCodeTestingClass();
        var genericNoCodeTestingClass = new GenericNoCodeTestingClass<int, long>();

#pragma warning disable CA1849 // Call async methods when in an async method
        noCodeTestingClass.TestMethod();
        noCodeTestingClass.TestMethodA();
        noCodeTestingClass.TestMethod(string.Empty);
        noCodeTestingClass.TestMethod(int.MinValue);
        noCodeTestingClass.TestMethod(string.Empty, string.Empty);
        noCodeTestingClass.TestMethod(string.Empty, string.Empty, string.Empty);
        noCodeTestingClass.TestMethod(string.Empty, string.Empty, string.Empty, string.Empty);
        noCodeTestingClass.TestMethod(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
        noCodeTestingClass.TestMethod(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
        noCodeTestingClass.TestMethod(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
        noCodeTestingClass.TestMethod(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
        noCodeTestingClass.TestMethod(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);

        NoCodeTestingClass.TestMethodStatic();
#pragma warning restore CA1849 // Call async methods when in an async method

        _ = noCodeTestingClass.ReturningTestMethod();
        _ = noCodeTestingClass.ReturningStringTestMethod();
        _ = noCodeTestingClass.ReturningCustomClassTestMethod();
        _ = noCodeTestingClass.ReturningTestMethod(string.Empty);
        _ = noCodeTestingClass.ReturningTestMethod(int.MinValue);
        _ = noCodeTestingClass.ReturningTestMethod(string.Empty, string.Empty);
        _ = noCodeTestingClass.ReturningTestMethod(string.Empty, string.Empty, string.Empty);
        _ = noCodeTestingClass.ReturningTestMethod(string.Empty, string.Empty, string.Empty, string.Empty);
        _ = noCodeTestingClass.ReturningTestMethod(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
        _ = noCodeTestingClass.ReturningTestMethod(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
        _ = noCodeTestingClass.ReturningTestMethod(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
        _ = noCodeTestingClass.ReturningTestMethod(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
        _ = noCodeTestingClass.ReturningTestMethod(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);

        _ = NoCodeTestingClass.ReturningTestMethodStatic();

        await noCodeTestingClass.TestMethodAsync().ConfigureAwait(false);
        await noCodeTestingClass.TestMethodAAsync().ConfigureAwait(false);
        await noCodeTestingClass.TestMethodAsync(string.Empty).ConfigureAwait(false);
        await noCodeTestingClass.TestMethodAsync(int.MinValue).ConfigureAwait(false);
        await noCodeTestingClass.TestMethodAsync(string.Empty, string.Empty).ConfigureAwait(false);
        await noCodeTestingClass.TestMethodAsync(string.Empty, string.Empty, string.Empty).ConfigureAwait(false);
        await noCodeTestingClass.TestMethodAsync(string.Empty, string.Empty, string.Empty, string.Empty).ConfigureAwait(false);
        await noCodeTestingClass.TestMethodAsync(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty).ConfigureAwait(false);
        await noCodeTestingClass.TestMethodAsync(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty).ConfigureAwait(false);
        await noCodeTestingClass.TestMethodAsync(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty).ConfigureAwait(false);
        await noCodeTestingClass.TestMethodAsync(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty).ConfigureAwait(false);
        await noCodeTestingClass.TestMethodAsync(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty).ConfigureAwait(false);

        await NoCodeTestingClass.TestMethodStaticAsync().ConfigureAwait(false);

        _ = await noCodeTestingClass.IntTaskTestMethodAsync().ConfigureAwait(false);
#if NET
        await noCodeTestingClass.ValueTaskTestMethodAsync().ConfigureAwait(false);
        _ = await noCodeTestingClass.IntValueTaskTestMethodAsync().ConfigureAwait(false);
#endif

#pragma warning disable CA1849 // Call async methods when in an async method
        _ = noCodeTestingClass.GenericTestMethod<int>();
#pragma warning restore CA1849 // Call async methods when in an async method
        _ = await noCodeTestingClass.GenericTestMethodAsync<int>().ConfigureAwait(false);
        _ = genericNoCodeTestingClass.GenericTestMethod(string.Empty, new object(), 123, 456L);
    }
}
