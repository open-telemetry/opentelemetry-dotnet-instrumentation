// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.PluginApi.ContinuousProfiling;

/// <summary>
/// Continuous profiler configuration.
/// </summary>
public class ContinuousProfilerConfiguration
{
    /// <summary>
    /// Gets the default state of the Continuous profiler.
    /// </summary>
    public static ContinuousProfilerConfiguration Default { get; } = new ContinuousProfilerConfiguration()
    {
        ThreadSamplingEnabled = false,
        AllocationSamplingEnabled = false,
        ExportInterval = TimeSpan.Zero,
        ExportTimeout = TimeSpan.Zero
    };

    /// <summary>
    /// Gets or sets a value indicating whether thread sampling is enabled.
    /// </summary>
    public bool ThreadSamplingEnabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating sampling interval.
    /// </summary>
    public uint ThreadSamplingInterval { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether allocation sampling is enabled.
    /// </summary>
    public bool AllocationSamplingEnabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating maximum memory samples per minute.
    /// </summary>
    public uint MaxMemorySamplesPerMinute { get; set; }

    /// <summary>
    /// Gets or sets export interval.
    /// </summary>
    public TimeSpan ExportInterval { get; set; }

    /// <summary>
    /// Gets or sets export timeout.
    /// </summary>
    public TimeSpan ExportTimeout { get; set; }

    /// <summary>
    /// Gets or sets continous profiler exporter.
    /// </summary>
    public IContinuousProfilerExporter? Exporter { get; set; }
}
