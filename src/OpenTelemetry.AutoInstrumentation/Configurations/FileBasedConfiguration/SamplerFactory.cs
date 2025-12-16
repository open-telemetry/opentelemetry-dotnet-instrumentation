// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Logging;
using OpenTelemetry.Trace;

namespace OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;

internal static class SamplerFactory
{
    private static readonly IOtelLogger Logger = OtelLogging.GetLogger();

    public static Sampler? CreateSampler(SamplerConfig? samplerConfig, bool failFast)
    {
        try
        {
            return CreateSamplerInternal(samplerConfig, failFast, "tracer_provider.sampler");
        }
        catch (Exception ex) when (!failFast)
        {
            Logger.Error(ex, "Failed to create sampler from file-based configuration.");
            return null;
        }
    }

    private static Sampler? CreateSamplerInternal(SamplerConfig? samplerConfig, bool failFast, string path)
    {
        if (samplerConfig == null)
        {
            return null;
        }

        var configuredSamplers = new Dictionary<string, Sampler?>();

        if (samplerConfig.AlwaysOn != null)
        {
            configuredSamplers.Add("always_on", new AlwaysOnSampler());
        }

        if (samplerConfig.AlwaysOff != null)
        {
            configuredSamplers.Add("always_off", new AlwaysOffSampler());
        }

        if (samplerConfig.TraceIdRatio != null)
        {
            configuredSamplers.Add("trace_id_ratio", CreateTraceIdRatioSampler(samplerConfig.TraceIdRatio, failFast, path + ".trace_id_ratio"));
        }

        if (samplerConfig.ParentBased != null)
        {
            configuredSamplers.Add("parent_based", CreateParentBasedSampler(samplerConfig.ParentBased, failFast, path + ".parent_based"));
        }

        if (configuredSamplers.Count == 0)
        {
            var message = $"Sampler configuration '{path}' does not specify a sampler type.";
            Logger.Warning(message);

            if (failFast)
            {
                throw new InvalidOperationException(message);
            }

            return null;
        }

        if (configuredSamplers.Count > 1)
        {
            var configuredNames = string.Join(", ", configuredSamplers.Keys);
            var message = $"Sampler configuration '{path}' specifies multiple sampler types ({configuredNames}). Only one sampler can be configured.";
            Logger.Error(message);

            if (failFast)
            {
                throw new InvalidOperationException(message);
            }

            return null;
        }

        var configuredSampler = configuredSamplers.Values.First();
        if (configuredSampler == null)
        {
            var message = $"Sampler configuration '{path}' is invalid.";
            Logger.Error(message);

            if (failFast)
            {
                throw new InvalidOperationException(message);
            }
        }

        return configuredSampler;
    }

    private static TraceIdRatioBasedSampler? CreateTraceIdRatioSampler(TraceIdRatioSamplerConfig config, bool failFast, string path)
    {
        if (!config.Ratio.HasValue)
        {
            var message = $"Sampler configuration '{path}' must define the 'ratio' property.";
            Logger.Error(message);

            if (failFast)
            {
                throw new InvalidOperationException(message);
            }

            return null;
        }

        var ratio = config.Ratio.Value;
        if (ratio is < 0 or > 1)
        {
            var message = $"Sampler configuration '{path}' ratio must be between 0 and 1 inclusive.";
            Logger.Error(message);

            if (failFast)
            {
                throw new InvalidOperationException(message);
            }

            return null;
        }

        return new TraceIdRatioBasedSampler(ratio);
    }

    private static ParentBasedSampler CreateParentBasedSampler(ParentBasedSamplerConfig config, bool failFast, string path)
    {
        var rootSampler = GetSamplerOrDefault(config.Root, new AlwaysOnSampler(), failFast, path + ".root", "always_on");
        var remoteParentSampled = GetSamplerOrDefault(config.RemoteParentSampled, new AlwaysOnSampler(), failFast, path + ".remote_parent_sampled", "always_on");
        var remoteParentNotSampled = GetSamplerOrDefault(config.RemoteParentNotSampled, new AlwaysOffSampler(), failFast, path + ".remote_parent_not_sampled", "always_off");
        var localParentSampled = GetSamplerOrDefault(config.LocalParentSampled, new AlwaysOnSampler(), failFast, path + ".local_parent_sampled", "always_on");
        var localParentNotSampled = GetSamplerOrDefault(config.LocalParentNotSampled, new AlwaysOffSampler(), failFast, path + ".local_parent_not_sampled", "always_off");

        return new ParentBasedSampler(rootSampler, remoteParentSampled, remoteParentNotSampled, localParentSampled, localParentNotSampled);
    }

    private static Sampler GetSamplerOrDefault(SamplerVariantsConfig? samplerConfig, Sampler defaultSampler, bool failFast, string path, string defaultSamplerName)
    {
        if (samplerConfig == null)
        {
            return defaultSampler;
        }

        var configuredSamplers = new Dictionary<string, Sampler?>();

        if (samplerConfig.AlwaysOn != null)
        {
            configuredSamplers.Add("always_on", new AlwaysOnSampler());
        }

        if (samplerConfig.AlwaysOff != null)
        {
            configuredSamplers.Add("always_off", new AlwaysOffSampler());
        }

        if (samplerConfig.TraceIdRatio != null)
        {
            configuredSamplers.Add("trace_id_ratio", CreateTraceIdRatioSampler(
                samplerConfig.TraceIdRatio, failFast, $"{path}.trace_id_ratio"));
        }

        if (configuredSamplers.Count == 0)
        {
            var message = $"Sampler configuration '{path}' does not specify a sampler type.";
            Logger.Warning(message);

            if (failFast)
            {
                throw new InvalidOperationException(message);
            }

            return defaultSampler;
        }

        if (configuredSamplers.Count > 1)
        {
            var configuredNames = string.Join(", ", configuredSamplers.Keys);
            var message = $"Sampler configuration '{path}' specifies multiple sampler types ({configuredNames}). Only one sampler can be configured.";
            Logger.Error(message);

            if (failFast)
            {
                throw new InvalidOperationException(message);
            }

            return defaultSampler;
        }

        var sampler = configuredSamplers.Values.First();
        if (sampler == null)
        {
            var message = $"Sampler configuration '{path}' is invalid. Falling back to default '{defaultSamplerName}' sampler.";
            Logger.Warning(message);

            if (failFast)
            {
                throw new InvalidOperationException(message);
            }

            return defaultSampler;
        }

        return sampler;
    }
}
