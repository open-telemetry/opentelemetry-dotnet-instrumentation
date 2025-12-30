// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using TestApplication.Shared;

namespace TestApplication.ContinuousProfiler.ContextTracking;

internal static class Program
{
    private static readonly ActivitySource Source = new("TestApplication.ContinuousProfiler.ContextTracking");

    public static async Task Main(string[] args)
    {
        ConsoleHelper.WriteSplashScreen(args);
        Thread.CurrentThread.Name = "TestName";
        // Start an activity that remains active until async operation completes,
        // and verify that trace context flows properly between threads that carry out parts of the async operation.
        using var activity = Source.StartActivity();

        await DoSomethingAsync().ConfigureAwait(false);
    }

    private static async Task DoSomethingAsync()
    {
        // timeout aligned with thread sampling interval
        var timeout = TimeSpan.FromSeconds(1);
        Thread.Sleep(timeout);
        // continue on thread pool thread
        await Task.Yield();
        Thread.Sleep(timeout);
        // switch to different thread pool thread
        await Task.Yield();
        Thread.Sleep(timeout);
        await Task.Yield();
        Thread.Sleep(timeout);
        await Task.Yield();
        Thread.Sleep(timeout);
    }
}
