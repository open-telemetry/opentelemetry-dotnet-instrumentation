// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.InteropServices;

namespace OpenTelemetry.Metrics;

/// <summary>
/// Stores details about an exemplar.
/// </summary>
public readonly record struct Exemplar
{
    /// <summary>
    /// Gets a reference to the filtered attributes associated with the exemplar.
    /// </summary>
    /// <param name="exemplar"><see cref="Exemplar"/>.</param>
    /// <returns><see cref="TagList"/>.</returns>
    public static ref readonly TagList GetFilteredAttributesReference(in Exemplar exemplar)
        => ref exemplar._FilteredAttributes;

    private readonly ExemplarValueStorage _ValueStorage;
    private readonly TagList _FilteredAttributes;

    /// <summary>
    /// Initializes a new instance of the <see cref="Exemplar"/> struct.
    /// </summary>
    /// <param name="timestampUtc">Timestamp.</param>
    /// <param name="traceId"><see cref="ActivityTraceId"/>.</param>
    /// <param name="spanId"><see cref="ActivitySpanId"/>.</param>
    /// <param name="value">Value.</param>
    /// <param name="filteredAttributes">Filtered attributes.</param>
    public Exemplar(
        DateTime timestampUtc,
        ActivityTraceId traceId,
        ActivitySpanId spanId,
        long value,
        params ReadOnlySpan<KeyValuePair<string, object?>> filteredAttributes)
        : this(timestampUtc, traceId, spanId, filteredAttributes)
    {
        _ValueStorage = new() { ValueAsLong = value };
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Exemplar"/> struct.
    /// </summary>
    /// <param name="timestampUtc">Timestamp.</param>
    /// <param name="traceId"><see cref="ActivityTraceId"/>.</param>
    /// <param name="spanId"><see cref="ActivitySpanId"/>.</param>
    /// <param name="value">Value.</param>
    /// <param name="filteredAttributes">Filtered attributes.</param>
    public Exemplar(
        DateTime timestampUtc,
        ActivityTraceId traceId,
        ActivitySpanId spanId,
        double value,
        params ReadOnlySpan<KeyValuePair<string, object?>> filteredAttributes)
        : this(timestampUtc, traceId, spanId, filteredAttributes)
    {
        _ValueStorage = new() { ValueAsDouble = value };
    }

    private Exemplar(
        DateTime timestampUtc,
        ActivityTraceId traceId,
        ActivitySpanId spanId,
        params ReadOnlySpan<KeyValuePair<string, object?>> filteredAttributes)
    {
        TimestampUtc = timestampUtc;
        TraceId = traceId;
        SpanId = spanId;
        _FilteredAttributes = new TagList(filteredAttributes);
    }

    /// <summary>
    /// Gets the exemplar timestamp .
    /// </summary>
    public DateTime TimestampUtc { get; }

    /// <summary>
    /// Gets the exemplar trace identifier.
    /// </summary>
    public ActivityTraceId TraceId { get; }

    /// <summary>
    /// Gets the exemplar span identifier.
    /// </summary>
    public ActivitySpanId SpanId { get; }

    /// <summary>
    /// Gets the exemplar value as a <see langword="long"/>.
    /// </summary>
    public long ValueAsLong => _ValueStorage.ValueAsLong;

    /// <summary>
    /// Gets the exemplar value as a <see langword="double"/>.
    /// </summary>
    public double ValueAsDouble => _ValueStorage.ValueAsDouble;

    [StructLayout(LayoutKind.Explicit)]
    private struct ExemplarValueStorage
    {
        [FieldOffset(0)]
        public long ValueAsLong;

        [FieldOffset(0)]
        public double ValueAsDouble;
    }
}
