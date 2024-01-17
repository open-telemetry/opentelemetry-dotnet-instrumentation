// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;

namespace TestApplication.ContinuousProfiler.ContextTracking;

internal class Program
{
    private static readonly ActivitySource Source = new ActivitySource("TestApplication.ContinuousProfiler.ContextTracking");

    public static async Task Main(string[] args)
    {
        using var activity = Source.StartActivity();
        await DoSomethingAsync();
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
