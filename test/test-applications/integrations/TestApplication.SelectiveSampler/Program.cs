// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using TestApplication.Shared;

namespace TestApplication.SelectiveSampler;

internal static class Program
{
    private static readonly ActivitySource MyActivitySource = new("TestApplication.SelectiveSampler", "1.0.0");

    public static async Task Main(string[] args)
    {
        ConsoleHelper.WriteSplashScreen(args);
        Thread.CurrentThread.Name = "Main";

        // Trace with nested activities
        using (MyActivitySource.StartActivity("outer"))
        {
#pragma warning disable CA1849 // Call async methods when in an async method. Intentional sync wait for testing purposes.
            Thread.Sleep(100);
#pragma warning restore CA1849 // Call async methods when in an async method. Intentional sync wait for testing purposes.
            using (MyActivitySource.StartActivity("inner"))
            {
                await SimpleAsyncCase().ConfigureAwait(false);
            }
        }
    }

    private static async Task SimpleAsyncCase()
    {
        WaitShortSync();
        await Task.Yield();
        WaitLongSync();
    }

    private static void WaitShortSync()
    {
        Thread.Sleep(200);
    }

    private static void WaitLongSync()
    {
        Thread.Sleep(300);
    }
}
