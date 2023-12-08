// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Globalization;
using OpenTelemetry.Trace;

namespace OpenTelemetry.AutoInstrumentation.Configurations;

internal static class TracerSamplerHelper
{
    public static Sampler? GetSampler(string tracerSampler, string? tracerSamplerArguments)
    {
        switch (tracerSampler)
        {
            case "always_on":
                return new AlwaysOnSampler();
            case "always_off":
                return new AlwaysOffSampler();
            case "traceidratio":
                return CreateTraceIdRatioBasedSampler(tracerSamplerArguments);
            case "parentbased_always_on":
                return new ParentBasedSampler(new AlwaysOnSampler());
            case "parentbased_always_off":
                return new ParentBasedSampler(new AlwaysOffSampler());
            case "parentbased_traceidratio":
                return new ParentBasedSampler(CreateTraceIdRatioBasedSampler(tracerSamplerArguments));
        }

        return null;
    }

    private static TraceIdRatioBasedSampler CreateTraceIdRatioBasedSampler(string? arguments)
    {
        const double defaultRatio = 1.0;

        var ratio = defaultRatio;

        if (double.TryParse(arguments, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedRatio)
            && parsedRatio >= 0.0 && parsedRatio <= 1.0)
        {
            ratio = parsedRatio;
        }

        return new TraceIdRatioBasedSampler(ratio);
    }
}
