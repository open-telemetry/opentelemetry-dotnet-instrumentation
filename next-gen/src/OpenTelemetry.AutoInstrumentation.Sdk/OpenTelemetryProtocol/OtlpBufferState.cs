// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.OpenTelemetryProtocol;

/// <summary>
/// Represents the state of an OTLP buffer.
/// </summary>
internal sealed class OtlpBufferState
{
    /// <summary>
    /// The size to reserve for length fields.
    /// </summary>
    public const int ReserveSizeForLength = 4;

    /// <summary>
    /// Initializes a new instance of the <see cref="OtlpBufferState"/> class.
    /// </summary>
    /// <param name="initialCapacity">The initial capacity of the buffer.</param>
    public OtlpBufferState(int initialCapacity = 750000)
    {
        Buffer = new byte[initialCapacity];
        Reset();
    }

    /// <summary>
    /// Gets the underlying request buffer.
    /// </summary>
    public byte[] Buffer { get; private set; }

    /// <summary>
    /// Gets or sets the current write position in the buffer.
    /// </summary>
    public int WritePosition { get; set; }

    /// <summary>
    /// Resets the buffer state.
    /// </summary>
    public void Reset()
    {
        WritePosition = 0;
    }
}
