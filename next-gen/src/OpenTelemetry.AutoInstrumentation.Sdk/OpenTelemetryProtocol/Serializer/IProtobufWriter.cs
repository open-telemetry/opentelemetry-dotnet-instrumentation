// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.OpenTelemetryProtocol.Serializer;

/// <summary>
/// Interface for manual Protobuf serialization writers.
/// </summary>
internal interface IProtobufWriter
{
    /// <summary>
    /// Gets the buffer containing serialized protobuf data.
    /// </summary>
    byte[] Buffer { get; }

    /// <summary>
    /// Gets the current position in the buffer.
    /// </summary>
    int WritePosition { get; }

    /// <summary>
    /// Resets the writer state for reuse.
    /// </summary>
    void Reset();
}
