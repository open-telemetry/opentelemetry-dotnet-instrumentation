// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.ComponentModel.DataAnnotations;

using Microsoft.Extensions.Configuration;

namespace OpenTelemetry.Configuration;

/// <summary>
/// Contains OpenTelemetry traceId ratio-based sampler options.
/// </summary>
public sealed class OpenTelemetryTraceIdRatioBasedSamplerOptions
{
    internal static OpenTelemetryTraceIdRatioBasedSamplerOptions ParseFromConfig(IConfigurationSection config)
    {
        Debug.Assert(config != null);

        return new(
            config.GetValueOrUseDefault("SamplingRatio", 1.0D));
    }

    internal OpenTelemetryTraceIdRatioBasedSamplerOptions(
        double samplingRatio)
    {
        SamplingRatio = samplingRatio;
    }

    /// <summary>
    /// Gets the sampling ratio.
    /// </summary>
    [Range(0D, 1D)]
    public double SamplingRatio { get; }
}
