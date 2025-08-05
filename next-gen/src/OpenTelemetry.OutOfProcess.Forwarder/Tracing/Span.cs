// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.Tracing;

/// <summary>
/// Stores details about an ended span.
/// </summary>
public readonly ref struct Span
{
    private readonly ref readonly SpanInfo _Info;

    /// <summary>
    /// Initializes a new instance of the <see cref="Span"/> struct.
    /// </summary>
    /// <param name="info"><see cref="SpanInfo"/>.</param>
    public Span(in SpanInfo info)
    {
        _Info = ref info;
    }

    /// <summary>
    /// Gets the span information.
    /// </summary>
    public readonly ref readonly SpanInfo Info => ref _Info;

    /// <summary>
    /// Gets the span attributes.
    /// </summary>
    public ReadOnlySpan<KeyValuePair<string, object?>> Attributes { get; init; }

    /// <summary>
    /// Gets the span links.
    /// </summary>
    public ReadOnlySpan<SpanLink> Links { get; init; }

    /// <summary>
    /// Gets the span events.
    /// </summary>
    public ReadOnlySpan<SpanEvent> Events { get; init; }
}
