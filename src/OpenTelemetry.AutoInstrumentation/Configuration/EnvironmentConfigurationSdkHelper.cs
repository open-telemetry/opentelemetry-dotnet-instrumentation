// <copyright file="EnvironmentConfigurationSdkHelper.cs" company="OpenTelemetry Authors">
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

using System;
using System.Collections.Generic;
using OpenTelemetry.Context.Propagation;

namespace OpenTelemetry.AutoInstrumentation.Configuration;

internal static class EnvironmentConfigurationSdkHelper
{
    public static void UseEnvironmentVariables(SdkSettings settings)
    {
        SetDefaultTextMapPropagator(settings.Propagators);
    }

    private static void SetDefaultTextMapPropagator(IList<Propagator> settingsPropagators)
    {
        if (settingsPropagators.Count == 0)
        {
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
        switch (propagator)
        {
            case Propagator.W3CTraceContext:
                return new TraceContextPropagator();
            case Propagator.W3CBaggage:
                return new BaggagePropagator();
            case Propagator.B3Multi:
                return new Extensions.Propagators.B3Propagator(false);
        }

        throw new ArgumentOutOfRangeException(nameof(propagator));
    }
}
