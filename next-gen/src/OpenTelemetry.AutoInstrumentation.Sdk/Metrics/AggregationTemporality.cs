// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.Metrics;

/// <summary>
/// Enumeration used to define the aggregation temporality for a <see
/// cref="Metric"/>.
/// </summary>
public enum AggregationTemporality
{
    /// <summary>
    /// Unspecified.
    /// </summary>
    Unspecified = 0,

    /// <summary>
    /// Delta.
    /// </summary>
    Delta = 1,

    /// <summary>
    /// Cumulative.
    /// </summary>
    Cumulative = 2,
}
