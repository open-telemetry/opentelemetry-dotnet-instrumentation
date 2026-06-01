// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.PluginApi.ContinuousProfiling;

/// <summary>
/// Provides interface for continuous profiler exporter.
/// </summary>
public interface IContinuousProfilerExporter
{
    /// <summary>
    /// Export thread samples.
    /// </summary>
    /// <param name="buffer">data buffer</param>
    /// <param name="read">read count</param>
    /// <param name="cancellationToken">cancellation token</param>
    void ExportThreadSamples(byte[] buffer, int read, CancellationToken cancellationToken);

    /// <summary>
    /// Export allocation samples.
    /// </summary>
    /// <param name="buffer">data buffer</param>
    /// <param name="read">read count</param>
    /// <param name="cancellationToken">cancellation token</param>
    void ExportAllocationSamples(byte[] buffer, int read, CancellationToken cancellationToken);
}
