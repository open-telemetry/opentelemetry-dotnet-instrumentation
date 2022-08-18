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

using System;
using System.Collections.Generic;

namespace OpenTelemetry.AutoInstrumentation.Configuration;

/// <summary>
/// Propagator Settings
/// </summary>
public class SdkSettings
{
    private SdkSettings(CompositeConfigurationSource source)
    {
        var propagators = source.GetString(ConfigurationKeys.Sdk.Propagators);

        if (!string.IsNullOrEmpty(propagators))
        {
            foreach (var propagator in propagators.Split(Constants.ConfigurationValues.Separator))
            {
                Propagators.Add(ParsePropagator(propagator));
            }
        }
    }

    /// <summary>
    /// Gets the list of propagators to be used.
    /// </summary>
    public IList<Propagator> Propagators { get; } = new List<Propagator>();

    internal static SdkSettings FromDefaultSources()
    {
        var configurationSource = new CompositeConfigurationSource
        {
            new EnvironmentConfigurationSource(),

#if NETFRAMEWORK
            // on .NET Framework only, also read from app.config/web.config
            new NameValueConfigurationSource(System.Configuration.ConfigurationManager.AppSettings)
#endif
        };

        return new SdkSettings(configurationSource);
    }

    private static Propagator ParsePropagator(string propagator)
    {
        switch (propagator)
        {
            case Constants.ConfigurationValues.Propagators.W3CTraceContext:
                return Propagator.W3CTraceContext;
            case Constants.ConfigurationValues.Propagators.W3CBaggage:
                return Propagator.W3CBaggage;
            case Constants.ConfigurationValues.Propagators.B3Multi:
                return Propagator.B3Multi;
            default:
                throw new FormatException($"Propagator '{propagator}' is not supported");
        }
    }
}
