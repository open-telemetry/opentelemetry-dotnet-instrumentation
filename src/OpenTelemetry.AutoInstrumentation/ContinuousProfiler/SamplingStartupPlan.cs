// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.PluginApi.ContinuousProfiling;
using OpenTelemetry.AutoInstrumentation.PluginApi.SelectiveSampling;

namespace OpenTelemetry.AutoInstrumentation.ContinuousProfiler;

internal sealed class SamplingStartupPlan
{
    public SamplingStartupPlan(
        RuntimeSamplerConfiguration seed,
        bool cpuExportPrepared,
        bool selectiveExportPrepared,
        bool allocationExportPrepared,
        ContinuousProfilerConfiguration continuousConfiguration,
        SelectiveSamplerConfiguration? selectiveConfiguration)
    {
        Seed = seed;
        CpuExportPrepared = cpuExportPrepared;
        SelectiveExportPrepared = selectiveExportPrepared;
        AllocationExportPrepared = allocationExportPrepared;
        ContinuousExporter = continuousConfiguration.Exporter;
        ContinuousExportInterval = continuousConfiguration.ExportInterval;
        ContinuousExportTimeout = continuousConfiguration.ExportTimeout;
        SelectiveExporter = selectiveConfiguration?.Exporter;
        SelectiveExportInterval = selectiveConfiguration?.ExportInterval ?? TimeSpan.Zero;
        SelectiveExportTimeout = selectiveConfiguration?.ExportTimeout ?? TimeSpan.Zero;
    }

    public RuntimeSamplerConfiguration Seed { get; }

    public bool CpuExportPrepared { get; }

    public bool SelectiveExportPrepared { get; }

    public bool AllocationExportPrepared { get; }

    public IContinuousProfilerExporter? ContinuousExporter { get; }

    public TimeSpan ContinuousExportInterval { get; }

    public TimeSpan ContinuousExportTimeout { get; }

    public ISelectiveSamplerExporter? SelectiveExporter { get; }

    public TimeSpan SelectiveExportInterval { get; }

    public TimeSpan SelectiveExportTimeout { get; }

    public bool HasPreparedExports => CpuExportPrepared || SelectiveExportPrepared || AllocationExportPrepared;

    public bool CanExport(RuntimeSamplerConfiguration configuration)
    {
        return ContinuousProfilerManager.CanExportCommittedConfiguration(configuration, CpuExportPrepared, SelectiveExportPrepared, AllocationExportPrepared);
    }
}
