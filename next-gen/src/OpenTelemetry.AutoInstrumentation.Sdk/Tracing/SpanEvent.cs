// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.Tracing;

/// <summary>
/// Stores details about a span event.
/// </summary>
public readonly record struct SpanEvent
{
    /// <summary>
    /// Gets a reference to the <see cref="ActivityContext"/> for a span event.
    /// </summary>
    /// <param name="spanEvent"><see cref="SpanEvent"/>.</param>
    /// <returns><see cref="ActivityContext"/>.</returns>
    public static ref readonly TagList GetAttributesReference(in SpanEvent spanEvent)
        => ref spanEvent._Attributes;

    private readonly TagList _Attributes;

    /// <summary>
    /// Initializes a new instance of the <see cref="SpanEvent"/> struct.
    /// </summary>
    /// <param name="name">Event name.</param>
    public SpanEvent(
        string name)
        : this(name, DateTime.UtcNow)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SpanEvent"/> struct.
    /// </summary>
    /// <param name="name">Event name.</param>
    /// <param name="attributes">Event attributes.</param>
    public SpanEvent(
        string name,
        params ReadOnlySpan<KeyValuePair<string, object?>> attributes)
        : this(name, DateTime.UtcNow, attributes)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SpanEvent"/> struct.
    /// </summary>
    /// <param name="name">Event name.</param>
    /// <param name="timestampUtc">Event timestamp.</param>
    /// <param name="attributes">Event attributes.</param>
    public SpanEvent(
        string name,
        DateTime timestampUtc,
        params ReadOnlySpan<KeyValuePair<string, object?>> attributes)
    {
        ArgumentNullException.ThrowIfNull(name);

        if (timestampUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("TimestampUtc kind is invalid", nameof(timestampUtc));
        }

        Name = name;
        TimestampUtc = timestampUtc.ToUniversalTime();
        _Attributes = new TagList(attributes);
    }

    /// <summary>
    /// Gets the span event name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the span event timestamp.
    /// </summary>
    public DateTime TimestampUtc { get; }
}
