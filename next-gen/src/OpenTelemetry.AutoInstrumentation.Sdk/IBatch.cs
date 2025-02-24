// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry;

/// <summary>
/// Describes a batch of telemetry.
/// </summary>
/// <typeparam name="TBatchWriter"><see cref="IBatchWriter"/> type.</typeparam>
public interface IBatch<TBatchWriter>
    where TBatchWriter : IBatchWriter
{
    /// <summary>
    /// Write the batch to an <see cref="IBatchWriter"/> instance.
    /// </summary>
    /// <param name="writer"><see cref="IBatchWriter"/>.</param>
    /// <returns><see langword="true" /> if the operation succeeded.</returns>
    bool WriteTo(TBatchWriter writer);
}
