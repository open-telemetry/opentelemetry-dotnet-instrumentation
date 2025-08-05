// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.InteropServices;

namespace OpenTelemetry.Metrics;

/// <summary>
/// Stores details about a number metric point.
/// </summary>
[StructLayout(LayoutKind.Explicit)]
public readonly ref struct NumberMetricPoint
{
    [FieldOffset(0)]
    private readonly double _ValueAsDouble;
    [FieldOffset(0)]
    private readonly long _ValueAsLong;

    [FieldOffset(8)]
    private readonly DateTime _StartTimeUtc;

    [FieldOffset(16)]
    private readonly DateTime _EndTimeUtc;

    /// <summary>
    /// Initializes a new instance of the <see cref="NumberMetricPoint"/> struct.
    /// </summary>
    /// <param name="startTimeUtc">Start time.</param>
    /// <param name="endTimeUtc">End time.</param>
    /// <param name="value">Value.</param>
    public NumberMetricPoint(
        DateTime startTimeUtc,
        DateTime endTimeUtc,
        double value)
        : this(startTimeUtc, endTimeUtc)
    {
        _ValueAsDouble = value;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NumberMetricPoint"/> struct.
    /// </summary>
    /// <param name="startTimeUtc">Start time.</param>
    /// <param name="endTimeUtc">End time.</param>
    /// <param name="value">Value.</param>
    public NumberMetricPoint(
        DateTime startTimeUtc,
        DateTime endTimeUtc,
        long value)
        : this(startTimeUtc, endTimeUtc)
    {
        _ValueAsLong = value;
    }

    private NumberMetricPoint(
        DateTime startTimeUtc,
        DateTime endTimeUtc)
    {
        _StartTimeUtc = startTimeUtc.ToUniversalTime();
        _EndTimeUtc = endTimeUtc.ToUniversalTime();
    }

    /// <summary>
    /// Gets the number metric point value as a <see langword="double"/>.
    /// </summary>
    public double ValueAsDouble => _ValueAsDouble;

    /// <summary>
    /// Gets the number metric point value as a <see langword="long"/>.
    /// </summary>
    public long ValueAsLong => _ValueAsLong;

    /// <summary>
    /// Gets the number metric point start time.
    /// </summary>
    public DateTime StartTimeUtc => _StartTimeUtc;

    /// <summary>
    /// Gets the number metric point end time.
    /// </summary>
    public DateTime EndTimeUtc => _EndTimeUtc;
}
