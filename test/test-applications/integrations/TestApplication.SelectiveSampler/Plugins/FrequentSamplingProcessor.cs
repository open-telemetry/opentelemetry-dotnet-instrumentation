// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using System.Reflection;
using OpenTelemetry;

namespace TestApplication.SelectiveSampler.Plugins;

// Custom processor that selects all spans for frequent sampling.
#pragma warning disable CA1812 // Avoid uninstantiated internal classes. This class is instantiated by reflection.
internal sealed class FrequentSamplingProcessor : BaseProcessor<Activity>
#pragma warning disable CA1812 // Avoid uninstantiated internal classes. This class is instantiated by reflection.
{
    private static Action<ActivityTraceId>? _startSamplingDelegate;
    private static Action<ActivityTraceId>? _stopSamplingDelegate;

    public FrequentSamplingProcessor()
    {
        var nativeMethodsType = Type.GetType("OpenTelemetry.AutoInstrumentation.NativeMethods, OpenTelemetry.AutoInstrumentation") ?? throw new InvalidOperationException("OpenTelemetry.AutoInstrumentation.NativeMethods could not be found.");
        var startMethod = nativeMethodsType.GetMethod("SelectiveSamplingStart", BindingFlags.Static | BindingFlags.Public, null, [typeof(ActivityTraceId)], null);
        var stopMethod = nativeMethodsType!.GetMethod("SelectiveSamplingStop", BindingFlags.Static | BindingFlags.Public, null, [typeof(ActivityTraceId)], null);

        _startSamplingDelegate = (Action<ActivityTraceId>)Delegate.CreateDelegate(typeof(Action<ActivityTraceId>), startMethod!);
        _stopSamplingDelegate = (Action<ActivityTraceId>)Delegate.CreateDelegate(typeof(Action<ActivityTraceId>), stopMethod!);
    }

    public override void OnStart(Activity data)
    {
        // Native side API is trace-based, notifying only of root span is sufficient.
        if (IsLocalRoot(data))
        {
            _startSamplingDelegate?.Invoke(data.TraceId);
        }
    }

    public override void OnEnd(Activity data)
    {
        if (IsLocalRoot(data))
        {
            _stopSamplingDelegate?.Invoke(data.TraceId);
        }
    }

    private static bool IsLocalRoot(Activity data)
    {
        return data.Parent == null || data.HasRemoteParent;
    }
}
