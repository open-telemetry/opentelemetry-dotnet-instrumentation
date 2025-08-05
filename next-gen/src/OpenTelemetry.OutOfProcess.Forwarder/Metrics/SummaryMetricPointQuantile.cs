// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.Metrics;

/// <summary>
/// Stores details about a summary metric point quantile.
/// </summary>
public readonly record struct SummaryMetricPointQuantile
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SummaryMetricPointQuantile"/> struct.
    /// </summary>
    /// <param name="quantile">Quantile.</param>
    /// <param name="value">Value.</param>
    public SummaryMetricPointQuantile(
        double quantile,
        double value)
    {
        Quantile = quantile;
        Value = value;
    }

    /// <summary>
    /// Gets the summary metric point quantile.
    /// </summary>
    public double Quantile { get; }

    /// <summary>
    /// Gets the summary metric point quantile value.
    /// </summary>
    public double Value { get; }
}
