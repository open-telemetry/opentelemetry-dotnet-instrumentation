// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using OpenTelemetry;

namespace TestApplication.SelectiveSampler.Plugins;

// Custom processor that selects all spans for frequent sampling.
public class FrequentSamplingProcessor : BaseProcessor<Activity>
{
    private static Action<Activity>? _startSamplingDelegate;
    private static Action<Activity>? _stopSamplingDelegate;

    public FrequentSamplingProcessor()
    {
        var nativeMethodsType = Type.GetType("OpenTelemetry.AutoInstrumentation.NativeMethods, OpenTelemetry.AutoInstrumentation");
        if (nativeMethodsType == null)
        {
            throw new Exception("OpenTelemetry.AutoInstrumentation.NativeMethods could not be found.");
        }

        var startMethod = nativeMethodsType.GetMethod("SelectiveSamplingStart", BindingFlags.Static | BindingFlags.Public, null, [typeof(Activity)], null);
        var stopMethod = nativeMethodsType!.GetMethod("SelectiveSamplingStop", BindingFlags.Static | BindingFlags.Public, null, [typeof(Activity)], null);

        _startSamplingDelegate = (Action<Activity>)Delegate.CreateDelegate(typeof(Action<Activity>), startMethod!);
        _stopSamplingDelegate = (Action<Activity>)Delegate.CreateDelegate(typeof(Action<Activity>), stopMethod!);
    }

    public override void OnStart(Activity data)
    {
        _startSamplingDelegate?.Invoke(data);
    }

    public override void OnEnd(Activity data)
    {
        _stopSamplingDelegate?.Invoke(data);
    }
}
