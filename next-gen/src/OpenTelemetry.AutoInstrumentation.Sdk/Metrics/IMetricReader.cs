// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.Metrics;

/// <summary>
/// Describes the contract for a metric reader.
/// </summary>
public interface IMetricReader : IDisposable
{
    /// <summary>
    /// Collect metrics asynchronously.
    /// </summary>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
    /// <returns><see cref="Task"/> to await the operation.</returns>
    Task CollectAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Flush any metrics held by the metric reader asynchronously.
    /// </summary>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
    /// <returns><see cref="Task"/> to await the operation.</returns>
    Task FlushAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Shutdown the metric reader asynchronously.
    /// </summary>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
    /// <returns><see cref="Task"/> to await the operation.</returns>
    Task ShutdownAsync(CancellationToken cancellationToken);
}
