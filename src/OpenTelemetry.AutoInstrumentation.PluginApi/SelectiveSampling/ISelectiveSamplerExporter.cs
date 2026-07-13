// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.PluginApi.SelectiveSampling;

/// <summary>
/// Provides interface for selective sampler exporter.
/// </summary>
public interface ISelectiveSamplerExporter
{
    /// <summary>
    /// Export thread samples.
    /// </summary>
    /// <param name="buffer">data buffer</param>
    /// <param name="read">read count</param>
    /// <param name="cancellationToken">cancellation token</param>
    void ExportSelectedThreadSamples(byte[] buffer, int read, CancellationToken cancellationToken);
}
