// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry;

/// <summary>
/// Describes the contract for exporting batches of telemetry data asynchronously.
/// </summary>
/// <typeparam name="TBatchWriter"><see cref="IBatchWriter"/> type.</typeparam>
public interface IExporterAsync<TBatchWriter> : IDisposable
    where TBatchWriter : IBatchWriter
{
    /// <summary>
    /// Export a telemetry batch.
    /// </summary>
    /// <typeparam name="TBatch"><see cref="IBatch{TBatchWriter}"/> type.</typeparam>
    /// <param name="batch">Batch to export.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
    /// <returns><see langword="true"/> if the export was successful.</returns>
    Task<bool> ExportAsync<TBatch>(
        in TBatch batch,
        CancellationToken cancellationToken)
        where TBatch : IBatch<TBatchWriter>, allows ref struct;
}
