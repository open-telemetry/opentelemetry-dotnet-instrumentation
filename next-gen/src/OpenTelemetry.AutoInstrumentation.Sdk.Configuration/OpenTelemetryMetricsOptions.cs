// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Configuration;

namespace OpenTelemetry.Configuration;

/// <summary>
/// Contains OpenTelemetry metrics options.
/// </summary>
public sealed class OpenTelemetryMetricsOptions
{
    internal static OpenTelemetryMetricsOptions ParseFromConfig(IConfigurationSection config)
    {
        Debug.Assert(config != null);

        List<OpenTelemetryMetricsMeterOptions> meters = new();

        if (!config.TryParseValue(nameof(MaxHistograms), out int maxHistograms))
        {
            maxHistograms = DefaultMaxHistograms;
        }

        if (!config.TryParseValue(nameof(MaxTimeSeries), out int maxTimeSeries))
        {
            maxTimeSeries = DefaultMaxTimeSeries;
        }

        foreach (IConfigurationSection meter in config.GetSection("Meters").GetChildren())
        {
            if (string.IsNullOrEmpty(meter.Key))
            {
                continue;
            }

            List<string> instruments = new();

            foreach (IConfigurationSection instrument in meter.GetChildren())
            {
                if (string.IsNullOrEmpty(instrument.Value))
                {
                    continue;
                }

                instruments.Add(instrument.Value);
            }

            meters.Add(new(meter.Key, instruments));
        }

        return new(
            maxHistograms,
            maxTimeSeries,
            meters,
            OpenTelemetryPeriodicExportingOptions.ParseFromConfig(config.GetSection("PeriodicExporting")));
    }

    internal const int DefaultMaxHistograms = 10;
    internal const int DefaultMaxTimeSeries = 1000;

    internal OpenTelemetryMetricsOptions(
        int maxHistograms,
        int maxTimeSeries,
        IReadOnlyCollection<OpenTelemetryMetricsMeterOptions> meterOptions,
        OpenTelemetryPeriodicExportingOptions periodicExportingOptions)
    {
        Debug.Assert(meterOptions != null);
        Debug.Assert(periodicExportingOptions != null);

        MaxHistograms = maxHistograms;
        MaxTimeSeries = maxTimeSeries;
        MeterOptions = meterOptions;
        PeriodicExportingOptions = periodicExportingOptions;
    }

    /// <summary>
    /// Gets the metrics max histogram limit value.
    /// </summary>
    public int MaxHistograms { get; }

    /// <summary>
    /// Gets the metrics max time series limit value.
    /// </summary>
    public int MaxTimeSeries { get; }

    /// <summary>
    /// Gets the metrics meter options.
    /// </summary>
    public IReadOnlyCollection<OpenTelemetryMetricsMeterOptions> MeterOptions { get; }

    /// <summary>
    /// Gets the metrics periodic exporting options.
    /// </summary>
    public OpenTelemetryPeriodicExportingOptions PeriodicExportingOptions { get; }
}
