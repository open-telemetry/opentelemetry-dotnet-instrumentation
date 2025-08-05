// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.Logging;

/// <summary>
/// Stores details about an emitted log record.
/// </summary>
public readonly ref struct LogRecord
{
    private readonly ref readonly ActivityContext _SpanContext;
    private readonly ref readonly LogRecordInfo _Info;

    /// <summary>
    /// Initializes a new instance of the <see cref="LogRecord"/> struct.
    /// </summary>
    /// <param name="spanContext"><see cref="ActivityContext"/>.</param>
    /// <param name="info"><see cref="LogRecordInfo"/>.</param>
    public LogRecord(
        in ActivityContext spanContext,
        in LogRecordInfo info)
    {
        _SpanContext = ref spanContext;
        _Info = ref info;
    }

    /// <summary>
    /// Gets the span context for the log record.
    /// </summary>
    public readonly ref readonly ActivityContext SpanContext => ref _SpanContext;

    /// <summary>
    /// Gets the log record information.
    /// </summary>
    public readonly ref readonly LogRecordInfo Info => ref _Info;

    /// <summary>
    /// Gets the log record attributes.
    /// </summary>
    public ReadOnlySpan<KeyValuePair<string, object?>> Attributes { get; init; }
}
