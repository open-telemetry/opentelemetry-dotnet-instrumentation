// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.Tracing;

/// <summary>
/// Stores details about an ended span.
/// </summary>
public readonly record struct SpanInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SpanInfo"/> struct.
    /// </summary>
    /// <param name="scope"><see cref="InstrumentationScope"/>.</param>
    /// <param name="name">Span name.</param>
    public SpanInfo(
        InstrumentationScope scope,
        string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Scope = scope ?? throw new ArgumentNullException(nameof(scope));
        Name = name;
    }

    /// <summary>
    /// Gets the <see cref="InstrumentationScope"/> associated with the span.
    /// </summary>
    public InstrumentationScope Scope { get; }

    /// <summary>
    /// Gets the span name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the span kind.
    /// </summary>
    public ActivityKind? Kind { get; init; }

    /// <summary>
    /// Gets the span trace identifier.
    /// </summary>
    public required ActivityTraceId TraceId { get; init; }

    /// <summary>
    /// Gets the the span identifier.
    /// </summary>
    public required ActivitySpanId SpanId { get; init; }

    /// <summary>
    /// Gets the span trace flags.
    /// </summary>
    public required ActivityTraceFlags TraceFlags { get; init; }

    /// <summary>
    /// Gets the span trace state.
    /// </summary>
    public string? TraceState { get; init; }

    /// <summary>
    /// Gets the parent span identifier.
    /// </summary>
    public ActivitySpanId ParentSpanId { get; init; }

    /// <summary>
    /// Gets the span start time.
    /// </summary>
    public required DateTime StartTimestampUtc
    {
        get => field;
        init => field = value.ToUniversalTime();
    }

    /// <summary>
    /// Gets the span end time.
    /// </summary>
    public required DateTime EndTimestampUtc
    {
        get => field;
        init => field = value.ToUniversalTime();
    }

    /// <summary>
    /// Gets the span status code.
    /// </summary>
    public ActivityStatusCode StatusCode { get; init; } = ActivityStatusCode.Unset;

    /// <summary>
    /// Gets the span description.
    /// </summary>
    public string? StatusDescription { get; init; }
}
