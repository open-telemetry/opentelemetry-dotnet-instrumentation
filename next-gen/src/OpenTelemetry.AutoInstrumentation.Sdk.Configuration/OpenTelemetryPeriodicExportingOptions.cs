// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.ComponentModel.DataAnnotations;

using Microsoft.Extensions.Configuration;
using OpenTelemetry.Metrics;

namespace OpenTelemetry.Configuration;

/// <summary>
/// Contains OpenTelemetry metrics periodic exporting options.
/// </summary>
public sealed class OpenTelemetryPeriodicExportingOptions
{
    internal static OpenTelemetryPeriodicExportingOptions ParseFromConfig(IConfigurationSection config)
    {
        Debug.Assert(config != null);

        AggregationTemporality aggregationTemporalityPreference = AggregationTemporality.Cumulative;
        string? tempAggregationTemporality = config[nameof(AggregationTemporalityPreference)];
        if (!string.IsNullOrEmpty(tempAggregationTemporality)
            && Enum.TryParse<AggregationTemporality>(tempAggregationTemporality, ignoreCase: true, out var parsedAggregationTemporality))
        {
            aggregationTemporalityPreference = parsedAggregationTemporality;
        }

        if (!config.TryParseValue(nameof(ExportIntervalMilliseconds), out int exportIntervalMilliseconds))
        {
            exportIntervalMilliseconds = DefaultExportIntervalMilliseconds;
        }

        if (!config.TryParseValue(nameof(ExportTimeoutMilliseconds), out int exportTimeoutMilliseconds))
        {
            exportTimeoutMilliseconds = DefaultExportTimeoutMilliseconds;
        }

        return new(aggregationTemporalityPreference, exportIntervalMilliseconds, exportTimeoutMilliseconds);
    }

    internal const int DefaultExportIntervalMilliseconds = 60000;
    internal const int DefaultExportTimeoutMilliseconds = 30000;

    internal OpenTelemetryPeriodicExportingOptions(
        AggregationTemporality aggregationTemporalityPreference,
        int exportIntervalMilliseconds,
        int exportTimeoutMilliseconds)
    {
        AggregationTemporalityPreference = aggregationTemporalityPreference;
        ExportIntervalMilliseconds = exportIntervalMilliseconds;
        ExportTimeoutMilliseconds = exportTimeoutMilliseconds;
    }

    /// <summary>
    /// Gets the aggregation temporality preference.
    /// </summary>
    public AggregationTemporality AggregationTemporalityPreference { get; }

    /// <summary>
    /// Gets the delay interval (in milliseconds) between two consecutive exports. The default value is 60000.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int ExportIntervalMilliseconds { get; }

    /// <summary>
    /// Gets the timeout (in milliseconds) after which the export is cancelled. The default value is 30000.
    /// </summary>
    /// <remarks>Note: Set to <c>-1</c> to disable timeout.</remarks>
    [Range(1, int.MaxValue)]
    public int ExportTimeoutMilliseconds { get; }

    internal PeriodicExportingMetricReaderOptions ToOTelPeriodicExportingOptions()
        => new(AggregationTemporalityPreference, ExportIntervalMilliseconds, ExportTimeoutMilliseconds);
}
