// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.PluginApi.SelectiveSampling;

/// <summary>
/// Selective sampler configuration.
/// </summary>
public sealed class SelectiveSamplerConfiguration
{
    /// <summary>
    /// Gets or sets sampling interval.
    /// </summary>
    public uint SamplingInterval { get; set; }

    /// <summary>
    /// Gets or sets sampling export interval.
    /// </summary>
    public TimeSpan ExportInterval { get; set; }

    /// <summary>
    /// Gets or sets sampling export timeout.
    /// </summary>
    public TimeSpan ExportTimeout { get; set; }

    /// <summary>
    /// Gets or sets sampling exporter.
    /// </summary>
    public ISelectiveSamplerExporter? Exporter { get; set; }
}
