// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.PluginApi.ContinuousProfiling;
using OpenTelemetry.AutoInstrumentation.PluginApi.SelectiveSampling;
using OpenTelemetry.Trace;
using TestApplication.ContinuousProfiler;

namespace TestApplication.SelectiveSampler.Plugins;

#pragma warning disable CA1515 // Consider making public types internal. Needed for AutoInstrumentation plugin loading.
public class RuntimeCpuSamplingPlugin : BasePlugin, IContinuousProfilerPlugin, ISelectiveSamplerPlugin
#pragma warning restore CA1515 // Consider making public types internal. Needed for AutoInstrumentation plugin loading.
{
    public ContinuousProfilerConfiguration GetFirstContinuousProfilerConfiguration()
    {
        return new ContinuousProfilerConfiguration
        {
            ThreadSamplingEnabled = false,
            ThreadSamplingInterval = 200,
            AllocationSamplingEnabled = false,
            ExportInterval = TimeSpan.FromMilliseconds(50),
            ExportTimeout = TimeSpan.FromSeconds(5),
            Exporter = new CpuExporter(),
        };
    }

    public SelectiveSamplerConfiguration GetFirstSelectiveSamplingConfiguration()
    {
        return new SelectiveSamplerConfiguration
        {
            SamplingInterval = 50,
            ExportInterval = TimeSpan.FromMilliseconds(75),
            ExportTimeout = TimeSpan.FromSeconds(2),
            Exporter = new SelectiveExporter(),
        };
    }

    public override TracerProviderBuilder BeforeConfigureTracerProvider(TracerProviderBuilder builder)
    {
        return builder.AddProcessor<FrequentSamplingProcessor>();
    }

    private sealed class CpuExporter : IContinuousProfilerExporter
    {
        private readonly SampleNativeFormatParser _parser = new();

        public void ExportThreadSamples(byte[] buffer, int read, uint samplingInterval, CancellationToken cancellationToken)
        {
            var samples = _parser.ParseThreadSamples(buffer, read) ?? [];
            JsonConsoleExporter.WriteToConsole(samples);
            if (samples.Any(sample => sample.SelectedForFrequentSampling))
            {
                RuntimeCpuSamplingScenario.CpuBatches.Release();
            }
        }

        public void ExportAllocationSamples(byte[] buffer, int read, CancellationToken cancellationToken)
        {
        }
    }

    private sealed class SelectiveExporter : ISelectiveSamplerExporter
    {
        private readonly SampleNativeFormatParser _parser = new();

        public void ExportSelectedThreadSamples(byte[] buffer, int read, CancellationToken cancellationToken)
        {
            var samples = _parser.ParseSelectiveSamplerSamples(buffer, read);
            JsonConsoleExporter.WriteToConsole(samples);
            if (samples.Count > 0)
            {
                RuntimeCpuSamplingScenario.SelectiveBatches.Release();
            }
        }
    }
}
