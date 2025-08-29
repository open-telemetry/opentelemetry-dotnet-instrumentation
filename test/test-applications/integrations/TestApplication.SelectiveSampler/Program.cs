// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using System.Reflection;

namespace TestApplication.SelectiveSampler;

internal static class Program
{
    private static readonly ActivitySource ActivitySource = new("TestApplication.SelectiveSampler", "1.0.0");

    public static async Task Main(string[] args)
    {
        Thread.CurrentThread.Name = "Main";

        using (ActivitySource.StartActivity())
        {
            await SimpleAsyncCase();
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
