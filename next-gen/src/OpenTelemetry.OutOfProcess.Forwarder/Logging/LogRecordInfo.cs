// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

// Note: StyleCop doesn't understand "field" syntax yet
#pragma warning disable SA1513 // Closing brace should be followed by blank line
#pragma warning disable SA1500 // Braces for multi-line statements should not share line

namespace OpenTelemetry.Logging;

/// <summary>
/// Stores details about an emitted log record.
/// </summary>
public readonly record struct LogRecordInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LogRecordInfo"/> struct.
    /// </summary>
    /// <param name="scope"><see cref="InstrumentationScope"/>.</param>
    public LogRecordInfo(InstrumentationScope scope)
    {
        Scope = scope ?? throw new ArgumentNullException(nameof(scope));
    }

    /// <summary>
    /// Gets the <see cref="InstrumentationScope"/> associated with the log record.
    /// </summary>
    public InstrumentationScope Scope { get; }

    /// <summary>
    /// Gets the log record timestamp.
    /// </summary>
    public DateTime TimestampUtc
    {
        get => field;
        init => field = value.ToUniversalTime();
    } = DateTime.UtcNow;

    /// <summary>
    /// Gets the log record observed timestamp.
    /// </summary>
    public DateTime ObservedTimestampUtc
    {
        get => field;
        init => field = value.ToUniversalTime();
    } = DateTime.UtcNow;

    /// <summary>
    /// Gets the log record severity.
    /// </summary>
    public LogRecordSeverity Severity { get; init; } = LogRecordSeverity.Unspecified;

    /// <summary>
    /// Gets the log record severity text.
    /// </summary>
    public string? SeverityText { get; init; }

    /// <summary>
    /// Gets the log record body.
    /// </summary>
    public string? Body { get; init; }
}
