// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using System.Reflection;

namespace TestApplication.SelectiveSampler;

internal static class Program
{
    private static readonly ActivitySource ActivitySource = new("TestApplication.SelectiveSampler", "1.0.0");
    private static Action? _startSamplingDelegate;
    private static Action? _stopSamplingDelegate;
    private static AsyncLocal<Activity?>? _supportingActivityAsyncLocal;

    public static async Task Main(string[] args)
    {
        Init();
        Thread.CurrentThread.Name = "Main";

        using (ActivitySource.StartActivity())
        {
            await SimpleAsyncCase();
        }
    }

    // TODO: requires profiler to be attached.
    private static void Init()
    {
        var nativeMethodsType = Type.GetType("OpenTelemetry.AutoInstrumentation.NativeMethods, OpenTelemetry.AutoInstrumentation");
        if (nativeMethodsType == null)
        {
            throw new Exception("OpenTelemetry.AutoInstrumentation.NativeMethods could not be found.");
        }

        var startMethod = nativeMethodsType.GetMethod("SelectiveSamplingStart", BindingFlags.Static | BindingFlags.Public, null, [], null);
        var stopMethod = nativeMethodsType!.GetMethod("SelectiveSamplingStop", BindingFlags.Static | BindingFlags.Public, null, [], null);

        _startSamplingDelegate = (Action)Delegate.CreateDelegate(typeof(Action), startMethod!);
        _stopSamplingDelegate = (Action)Delegate.CreateDelegate(typeof(Action), stopMethod!);

        _supportingActivityAsyncLocal = new AsyncLocal<Activity?>(ActivityChanged);

        Activity.CurrentChanged += ActivityCurrentChanged;
    }

    private static void ActivityChanged(AsyncLocalValueChangedArgs<Activity?> sender)
    {
        var currentActivity = sender.CurrentValue;
        if (currentActivity != null)
        {
            _startSamplingDelegate?.Invoke();
        }
        else
        {
            _stopSamplingDelegate?.Invoke();
        }
    }

    private static void ActivityCurrentChanged(object? sender, ActivityChangedEventArgs e)
    {
        if (_supportingActivityAsyncLocal != null)
        {
            _supportingActivityAsyncLocal.Value = e.Current;
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
