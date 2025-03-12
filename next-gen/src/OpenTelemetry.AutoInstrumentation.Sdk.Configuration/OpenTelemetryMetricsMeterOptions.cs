// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.Configuration;

/// <summary>
/// Contains OpenTelemetry metrics meter options.
/// </summary>
public sealed class OpenTelemetryMetricsMeterOptions
{
    internal OpenTelemetryMetricsMeterOptions(
        string meterName,
        IReadOnlyCollection<string> instruments)
    {
        Debug.Assert(!string.IsNullOrEmpty(meterName));
        Debug.Assert(instruments != null);

        MeterName = meterName;
        Instruments = instruments;
    }

    /// <summary>
    /// Gets the meter name.
    /// </summary>
    public string MeterName { get; }

    /// <summary>
    /// Gets the instrument names.
    /// </summary>
    public IReadOnlyCollection<string> Instruments { get; }
}
