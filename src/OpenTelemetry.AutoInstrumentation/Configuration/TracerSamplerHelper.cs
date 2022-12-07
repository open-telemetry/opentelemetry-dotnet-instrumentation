// <copyright file="TracerSamplerHelper.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

using System.Globalization;
using OpenTelemetry.Trace;

namespace OpenTelemetry.AutoInstrumentation.Configuration;

internal static class TracerSamplerHelper
{
    public static Sampler GetSampler(string tracerSampler, string tracerSamplerArguments)
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

    private static TraceIdRatioBasedSampler CreateTraceIdRatioBasedSampler(string arguments)
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
