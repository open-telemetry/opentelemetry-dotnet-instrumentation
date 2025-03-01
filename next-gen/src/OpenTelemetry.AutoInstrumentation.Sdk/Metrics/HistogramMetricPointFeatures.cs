// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.Metrics;

/// <summary>
/// Describes the enabled features of a histogram or exponential histogram metric point.
/// </summary>
[Flags]
public enum HistogramMetricPointFeatures
{
    /// <summary>
    /// None.
    /// </summary>
    None = 0,

    /// <summary>
    /// Min and max values recorded.
    /// </summary>
    MinAndMaxRecorded = 0b1,

    /// <summary>
    /// Bucket values recorded.
    /// </summary>
    BucketsRecorded = 0b10
}
