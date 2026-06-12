// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.PluginApi.ContinuousProfiling;
using TestApplication.ContinuousProfiler.Plugins;

namespace TestApplication.ContinuousProfiler.ContextTracking;

#pragma warning disable CA1515 // Consider making public types internal. Needed for AutoInstrumentation plugin loading.
public class TestPlugin : BasePlugin, IContinuousProfilerPlugin
#pragma warning restore CA1515 // Consider making public types internal Needed for AutoInstrumentation plugin loading.
{
    public ContinuousProfilerConfiguration GetFirstContinuousProfilerConfiguration()
    {
        var threadSamplingInterval = 1000u;

        return new ContinuousProfilerConfiguration()
        {
            ThreadSamplingEnabled = true,
            ThreadSamplingInterval = threadSamplingInterval,
            AllocationSamplingEnabled = false,
            MaxMemorySamplesPerMinute = 200u,
            ExportInterval = TimeSpan.FromMilliseconds(500),
            ExportTimeout = TimeSpan.FromSeconds(5),
            Exporter = new OtlpOverHttpExporter(TimeSpan.FromMilliseconds(threadSamplingInterval), new SampleNativeFormatParser())
        };
    }
}
