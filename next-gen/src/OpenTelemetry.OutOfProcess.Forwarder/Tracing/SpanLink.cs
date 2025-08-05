// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.Tracing;

/// <summary>
/// Stores details about a span link.
/// </summary>
public readonly record struct SpanLink
{
    /// <summary>
    /// Gets a reference to the <see cref="ActivityContext"/> for a span link.
    /// </summary>
    /// <param name="spanLink"><see cref="SpanLink"/>.</param>
    /// <returns><see cref="ActivityContext"/>.</returns>
    public static ref readonly ActivityContext GetSpanContextReference(in SpanLink spanLink)
        => ref spanLink._SpanContext;

    /// <summary>
    /// Gets a reference to the attributes for a span link.
    /// </summary>
    /// <param name="spanLink"><see cref="SpanLink"/>.</param>
    /// <returns><see cref="TagList"/>.</returns>
    public static ref readonly TagList GetAttributesReference(in SpanLink spanLink)
        => ref spanLink._Attributes;

    private readonly ActivityContext _SpanContext;
    private readonly TagList _Attributes;

    /// <summary>
    /// Initializes a new instance of the <see cref="SpanLink"/> struct.
    /// </summary>
    /// <param name="spanContext"><see cref="ActivityContext"/>.</param>
    /// <param name="attributes">Attributes.</param>
    public SpanLink(
        in ActivityContext spanContext,
        params ReadOnlySpan<KeyValuePair<string, object?>> attributes)
    {
        _SpanContext = spanContext;
        _Attributes = new TagList(attributes);
    }
}
