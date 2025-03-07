// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.Configuration;

/// <summary>
/// Contains OpenTelemetry OTLP exporter header options.
/// </summary>
public sealed class OpenTelemetryProtocolExporterHeaderOptions
{
    internal OpenTelemetryProtocolExporterHeaderOptions(
        string key,
        string value)
    {
        Debug.Assert(!string.IsNullOrEmpty(key));
        Debug.Assert(!string.IsNullOrEmpty(value));

        Key = key;
        Value = value;
    }

    /// <summary>
    /// Gets the header key.
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// Gets the header value.
    /// </summary>
    public string Value { get; }
}
