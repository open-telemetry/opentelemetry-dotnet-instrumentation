// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Configuration;

namespace OpenTelemetry.Configuration;

/// <summary>
/// Contains OpenTelemetry parent-based sampler options.
/// </summary>
public sealed class OpenTelemetryParentBasedSamplerOptions
{
    internal static OpenTelemetryParentBasedSamplerOptions ParseFromConfig(IConfigurationSection config)
    {
        Debug.Assert(config != null);

        var rootSampler = config.GetSection("RootSampler");

        return new(OpenTelemetrySamplerOptions.ParseFromConfig(rootSampler));
    }

    internal OpenTelemetryParentBasedSamplerOptions(
        OpenTelemetrySamplerOptions rootSamplerOptions)
    {
        Debug.Assert(rootSamplerOptions != null);

        RootSamplerOptions = rootSamplerOptions;
    }

    /// <summary>
    /// Gets the root sampler options.
    /// </summary>
    public OpenTelemetrySamplerOptions RootSamplerOptions { get; }
}
