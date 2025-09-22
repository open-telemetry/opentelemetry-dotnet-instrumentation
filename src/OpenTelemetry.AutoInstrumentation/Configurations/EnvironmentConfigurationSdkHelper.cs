// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.Context.Propagation;

namespace OpenTelemetry.AutoInstrumentation.Configurations;

internal static class EnvironmentConfigurationSdkHelper
{
    public static void UseEnvironmentVariables(SdkSettings settings)
    {
        SetSdkTextMapPropagator(settings.Propagators);
    }

    private static void SetSdkTextMapPropagator(IReadOnlyList<Propagator> settingsPropagators)
    {
        if (settingsPropagators.Count == 0)
        {
            // use Sdk defaults
            return;
        }

        TextMapPropagator textMapPropagator;

        if (settingsPropagators.Count == 1)
        {
            textMapPropagator = GetTextMapPropagator(settingsPropagators[0]);
        }
        else
        {
            var internalPropagators = new List<TextMapPropagator>(settingsPropagators.Count);

            foreach (var propagator in settingsPropagators)
            {
                internalPropagators.Add(GetTextMapPropagator(propagator));
            }

            textMapPropagator = new CompositeTextMapPropagator(internalPropagators);
        }

        Sdk.SetDefaultTextMapPropagator(textMapPropagator);
    }

    private static TextMapPropagator GetTextMapPropagator(Propagator propagator)
    {
        return propagator switch
        {
            Propagator.W3CTraceContext => new TraceContextPropagator(),
            Propagator.W3CBaggage => new BaggagePropagator(),
            Propagator.B3Multi => new Extensions.Propagators.B3Propagator(singleHeader: false),
            Propagator.B3Single => new Extensions.Propagators.B3Propagator(singleHeader: true),
            _ => throw new ArgumentOutOfRangeException(nameof(propagator), propagator, "Propagator has an unexpected value."),
        };
    }
}
