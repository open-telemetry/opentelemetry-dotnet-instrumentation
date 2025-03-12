// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.ComponentModel.DataAnnotations;

using Microsoft.Extensions.Configuration;

namespace OpenTelemetry.Configuration;

/// <summary>
/// Contains OpenTelemetry tracing options.
/// </summary>
public sealed class OpenTelemetryTracingOptions
{
    internal static OpenTelemetryTracingOptions ParseFromConfig(IConfigurationSection config)
    {
        Debug.Assert(config != null);

        List<string> sources = new();

        foreach (var source in config.GetSection("Sources").GetChildren())
        {
            if (string.IsNullOrEmpty(source.Value))
            {
                continue;
            }

            sources.Add(source.Value);
        }

        OpenTelemetrySamplerOptions? sampler;

        var samplerConfig = config.GetSection("Sampler");
        if (samplerConfig.Value != null && double.TryParse(samplerConfig.Value, out double samplerDoubleValue))
        {
            sampler = new OpenTelemetrySamplerOptions(
                "ParentBased",
                samplerConfig,
                new OpenTelemetryParentBasedSamplerOptions(
                    new OpenTelemetrySamplerOptions(
                        "TraceIdRatio",
                        samplerConfig,
                        parentBasedOptions: null,
                        traceIdRatioBasedOptions: new OpenTelemetryTraceIdRatioBasedSamplerOptions(samplerDoubleValue))),
                traceIdRatioBasedOptions: null);
        }
        else
        {
            sampler = OpenTelemetrySamplerOptions.ParseFromConfig(samplerConfig);
        }

        return new(
            sources,
            sampler,
            OpenTelemetryBatchOptions.ParseFromConfig(config.GetSection("Batch")));
    }

    internal OpenTelemetryTracingOptions(
        IReadOnlyCollection<string> sources,
        OpenTelemetrySamplerOptions samplerOptions,
        OpenTelemetryBatchOptions batchOptions)
    {
        Debug.Assert(sources != null);
        Debug.Assert(samplerOptions != null);
        Debug.Assert(batchOptions != null);

        Sources = sources;
        SamplerOptions = samplerOptions;
        BatchOptions = batchOptions;
    }

    /// <summary>
    /// Gets the tracing sources.
    /// </summary>
    public IReadOnlyCollection<string> Sources { get; }

    /// <summary>
    /// Gets the tracing sampler options.
    /// </summary>
    public OpenTelemetrySamplerOptions SamplerOptions { get; }

    /// <summary>
    /// Gets the tracing batch options.
    /// </summary>
    public OpenTelemetryBatchOptions BatchOptions { get; }
}
