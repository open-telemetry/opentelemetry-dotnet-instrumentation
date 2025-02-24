// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry;

/// <summary>
/// Describes the contract for a telemetry processor.
/// </summary>
public interface IProcessor : IDisposable
{
    /// <summary>
    /// Flush any telemetry held by the processor asynchronously.
    /// </summary>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
    /// <returns><see cref="Task"/> to await the operation.</returns>
    Task FlushAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Shutdown the processor asynchronously.
    /// </summary>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
    /// <returns><see cref="Task"/> to await the operation.</returns>
    Task ShutdownAsync(CancellationToken cancellationToken);
}
