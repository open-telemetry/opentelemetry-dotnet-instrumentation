// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using TestApplication.Shared;

namespace TestApplication.NoCode;

public class Program
{
    public static void Main(string[] args)
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
    }
}
