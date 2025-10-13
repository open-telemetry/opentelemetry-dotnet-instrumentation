// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using TestApplication.Shared;

namespace TestApplication.NoCode;

public class Program
{
    public static async Task Main(string[] args)
    {
        ConsoleHelper.WriteSplashScreen(args);

        var noCodeTestingClass = new NoCodeTestingClass();

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

        await noCodeTestingClass.TestMethodAsync();
        await noCodeTestingClass.TestMethodAAsync();
        await noCodeTestingClass.TestMethodAsync(string.Empty);
        await noCodeTestingClass.TestMethodAsync(int.MinValue);
        await noCodeTestingClass.TestMethodAsync(string.Empty, string.Empty);
        await noCodeTestingClass.TestMethodAsync(string.Empty, string.Empty, string.Empty);
        await noCodeTestingClass.TestMethodAsync(string.Empty, string.Empty, string.Empty, string.Empty);
        await noCodeTestingClass.TestMethodAsync(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
        await noCodeTestingClass.TestMethodAsync(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
        await noCodeTestingClass.TestMethodAsync(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
        await noCodeTestingClass.TestMethodAsync(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
        await noCodeTestingClass.TestMethodAsync(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);

        await NoCodeTestingClass.TestMethodStaticAsync();

        _ = await noCodeTestingClass.IntTaskTestMethodAsync();
#if NET
        await noCodeTestingClass.ValueTaskTestMethodAsync();
        _ = await noCodeTestingClass.IntValueTaskTestMethodAsync();
#endif

        _ = noCodeTestingClass.GenericTestMethod<int>();
        _ = await noCodeTestingClass.GenericTestMethodAsync<int>();
    }
}
