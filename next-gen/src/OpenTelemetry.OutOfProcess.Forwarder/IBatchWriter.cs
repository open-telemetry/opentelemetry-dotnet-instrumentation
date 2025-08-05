// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.Resources;

namespace OpenTelemetry;

/// <summary>
/// Describes the contract for writing batches of telemetry data.
/// </summary>
public interface IBatchWriter
{
    /// <summary>
    /// Begin a batch.
    /// </summary>
    /// <param name="resource"><see cref="Resource"/>.</param>
    void BeginBatch(Resource resource);

    /// <summary>
    /// End a batch.
    /// </summary>
    void EndBatch();

    /// <summary>
    /// Begin an instrumentation scope.
    /// </summary>
    /// <param name="instrumentationScope"><see cref="InstrumentationScope"/>.</param>
    void BeginInstrumentationScope(InstrumentationScope instrumentationScope);

    /// <summary>
    /// End an instrumentation scope.
    /// </summary>
    void EndInstrumentationScope();
}
