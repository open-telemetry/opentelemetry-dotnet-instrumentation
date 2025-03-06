// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.Resources;

namespace OpenTelemetry.Metrics;

/// <summary>
/// Describes a <see cref="IBatchWriter"/> for writing batches of metrics.
/// </summary>
public abstract class MetricBatchWriter : MetricWriter, IBatchWriter
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MetricBatchWriter"/> class.
    /// </summary>
    protected MetricBatchWriter()
    {
    }

    /// <inheritdoc/>
    public virtual void BeginBatch(
        Resource resource)
    {
    }

    /// <inheritdoc/>
    public virtual void EndBatch()
    {
    }
}
