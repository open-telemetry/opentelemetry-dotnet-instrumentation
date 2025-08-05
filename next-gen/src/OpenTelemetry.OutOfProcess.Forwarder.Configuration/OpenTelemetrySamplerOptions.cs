// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Configuration;

namespace OpenTelemetry.Configuration;

/// <summary>
/// Contains OpenTelemetry sampler options.
/// </summary>
public sealed class OpenTelemetrySamplerOptions
{
    internal static OpenTelemetrySamplerOptions ParseFromConfig(IConfigurationSection config)
    {
        Debug.Assert(config != null);

        string? samplerTypeValue = config["Type"];

        string samplerType = "Unknown";
        OpenTelemetryParentBasedSamplerOptions? parentBasedOptions = null;
        OpenTelemetryTraceIdRatioBasedSamplerOptions? traceIdRatioBasedOptions = null;
        IConfigurationSection samplerConfiguration = config.GetSection("Settings");

        if (string.Equals(samplerTypeValue, "parentbased", StringComparison.OrdinalIgnoreCase))
        {
            samplerType = "ParentBased";
            parentBasedOptions = OpenTelemetryParentBasedSamplerOptions.ParseFromConfig(
                samplerConfiguration);
        }
        else if (string.Equals(samplerTypeValue, "traceidratio", StringComparison.OrdinalIgnoreCase))
        {
            samplerType = "TraceIdRatio";
            traceIdRatioBasedOptions = OpenTelemetryTraceIdRatioBasedSamplerOptions.ParseFromConfig(
                samplerConfiguration);
        }

        return new(samplerType, samplerConfiguration, parentBasedOptions, traceIdRatioBasedOptions);
    }

    internal OpenTelemetrySamplerOptions(
        string samplerType,
        IConfigurationSection samplerConfiguration,
        OpenTelemetryParentBasedSamplerOptions? parentBasedOptions,
        OpenTelemetryTraceIdRatioBasedSamplerOptions? traceIdRatioBasedOptions)
    {
        Debug.Assert(!string.IsNullOrEmpty(samplerType));
        Debug.Assert(samplerConfiguration != null);

        SamplerType = samplerType;
        SamplerConfiguration = samplerConfiguration;
        ParentBasedOptions = parentBasedOptions;
        TraceIdRatioBasedOptions = traceIdRatioBasedOptions;
    }

    /// <summary>
    /// Gets the sampler type.
    /// </summary>
    public string SamplerType { get; }

    /// <summary>
    /// Gets the sampler configuration.
    /// </summary>
    public IConfigurationSection SamplerConfiguration { get; }

    /// <summary>
    /// Gets the parent-based sampler options.
    /// </summary>
    public OpenTelemetryParentBasedSamplerOptions? ParentBasedOptions { get; }

    /// <summary>
    /// Gets the traceId ratio-based sampler options.
    /// </summary>
    public OpenTelemetryTraceIdRatioBasedSamplerOptions? TraceIdRatioBasedOptions { get; }
}
