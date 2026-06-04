// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.Plugins;

internal partial class PluginManager
{
    private const string ContinuousProfilerConfigMethod = "GetContinuousProfilerConfiguration";
    private const string ContinuousProfilerExtensionsMethod = "GetContinuousProfilerConfigurationExtensions";
    private const string ProbeNativeExportResolutionEnabled = "GetNativeExportResolutionEnabled";

    public (bool ThreadSamplingEnabled, uint ThreadSamplingInterval, bool AllocationSamplingEnabled, uint
        MaxMemorySamplesPerMinute, TimeSpan ExportInterval, TimeSpan ExportTimeout, object ContinuousProfilerExporter)
        GetFirstContinuousConfiguration()
    {
        foreach (var plugin in _plugins)
        {
            var mi = plugin.Type.GetMethod(ContinuousProfilerConfigMethod);
            if (mi?.Invoke(plugin.Instance, null) is Tuple<bool, uint, bool, uint, TimeSpan, TimeSpan, object>
                continuousProfilerConfiguration)
            {
                return (continuousProfilerConfiguration.Item1, continuousProfilerConfiguration.Item2,
                    continuousProfilerConfiguration.Item3, continuousProfilerConfiguration.Item4,
                    continuousProfilerConfiguration.Item5, continuousProfilerConfiguration.Item6,
                    continuousProfilerConfiguration.Item7);
            }
        }

        return (false, 0u, false, 0u, TimeSpan.Zero, TimeSpan.Zero, new object());
    }

    /// <summary>
    /// Independent capability probe for the native export-table symbol resolution knob.
    /// Looks for an optional <c>GetContinuousProfilerConfigurationExtensions()</c> method
    /// on the first plugin that defines it, then for a <c>GetNativeExportResolutionEnabled()</c>
    /// bool-returning method on the returned object. Returns the supplied default when either
    /// is absent or the probe throws. Decoupled from <see cref="GetFirstContinuousConfiguration"/>
    /// so additional knobs can be added without reshaping the tuple contract.
    /// </summary>
    public bool GetNativeExportResolutionEnabled(bool defaultValue = true)
    {
        foreach (var plugin in _plugins)
        {
            try
            {
                var extMi = plugin.Type.GetMethod(ContinuousProfilerExtensionsMethod);
                var ext = extMi?.Invoke(plugin.Instance, null);
                if (ext is null)
                {
                    continue;
                }

                var probe = ext.GetType().GetMethod(ProbeNativeExportResolutionEnabled, Type.EmptyTypes);
                if (probe is null || probe.ReturnType != typeof(bool))
                {
                    continue;
                }

                return (bool)probe.Invoke(ext, null)!;
            }
            catch
            {
                // Never let a misbehaving plugin probe break profiler init.
                // Fall through to the next plugin / default.
            }
        }

        return defaultValue;
    }
}
