// <copyright file="IntegrationSettings.cs" company="OpenTelemetry Authors">
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

namespace OpenTelemetry.AutoInstrumentation.Configuration;

/// <summary>
/// Contains integration-specific settings.
/// </summary>
public class IntegrationSettings
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IntegrationSettings"/> class.
    /// </summary>
    /// <param name="integrationName">The integration name.</param>
    /// <param name="source">The <see cref="IConfigurationSource"/> to use when retrieving configuration values.</param>
    public IntegrationSettings(string integrationName, IConfigurationSource source)
    {
        IntegrationName = integrationName ?? throw new ArgumentNullException(nameof(integrationName));

        if (source == null)
        {
            return;
        }

        Enabled = source.GetBool(string.Format(ConfigurationKeys.Integrations.Enabled, integrationName));
    }

    /// <summary>
    /// Gets the name of the integration. Used to retrieve integration-specific settings.
    /// </summary>
    public string IntegrationName { get; }

    /// <summary>
    /// Gets or sets a value indicating whether
    /// this integration is enabled.
    /// </summary>
    public bool? Enabled { get; set; }
}
