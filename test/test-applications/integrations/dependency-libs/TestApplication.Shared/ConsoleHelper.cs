// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace TestApplication.Shared;

internal static class ConsoleHelper
{
    public static void WriteSplashScreen(string[] args)
    {
        Console.WriteLine($"Command line: {string.Join(" ", args)}");
        Console.WriteLine($"Platform: {(Environment.Is64BitProcess ? "x64" : "x86")}");

        Console.WriteLine("Environment variables:");
        foreach (var entry in ProfilerHelper.GetEnvironmentConfiguration())
        {
            Console.WriteLine($"\t{entry.Key} = {entry.Value}");
        }
    }
}
