// <copyright file="SdkSettings.cs" company="OpenTelemetry Authors">
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

using System.Diagnostics.CodeAnalysis;
using OpenTelemetry.AutoInstrumentation.Logging;

namespace OpenTelemetry.AutoInstrumentation.Configurations;

/// <summary>
/// Propagator Settings
/// </summary>
internal class SdkSettings : Settings
{
    private static readonly IOtelLogger Logger = OtelLogging.GetLogger();

    /// <summary>
    /// Gets the list of propagators to be used.
    /// </summary>
    public IList<Propagator> Propagators { get; } = new List<Propagator>();

    protected override void OnLoad(Configuration configuration)
    {
        var propagators = configuration.GetString(ConfigurationKeys.Sdk.Propagators);

        if (!string.IsNullOrEmpty(propagators))
        {
            foreach (var propagatorValue in propagators!.Split(Constants.ConfigurationValues.Separator))
            {
                if (TryParsePropagator(propagatorValue, out var propagator))
                {
                    Propagators.Add(propagator.Value);
                }
            }
        }
    }

    private static bool TryParsePropagator(string propagatorValue, [NotNullWhen(true)] out Propagator? propagator)
    {
        switch (propagatorValue)
        {
            case Constants.ConfigurationValues.Propagators.W3CTraceContext:
                propagator = Propagator.W3CTraceContext;
                break;
            case Constants.ConfigurationValues.Propagators.W3CBaggage:
                propagator = Propagator.W3CBaggage;
                break;
            case Constants.ConfigurationValues.Propagators.B3Multi:
                propagator = Propagator.B3Multi;
                break;
            case Constants.ConfigurationValues.Propagators.B3Single:
                propagator = Propagator.B3Single;
                break;
            default:
                propagator = null;
                Logger.Error($"Propagator '{propagatorValue}' is not supported. Skipping.");
                return false;
        }

        return true;
    }
}
