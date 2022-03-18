// <copyright file="IntegrationSettingsCollection.cs" company="OpenTelemetry Authors">
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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace OpenTelemetry.AutoInstrumentation.Configuration;

/// <summary>
/// A collection of <see cref="IntegrationSettings"/> instances, referenced by name.
/// </summary>
public class IntegrationSettingsCollection
{
    private readonly IConfigurationSource _source;
    private readonly ConcurrentDictionary<string, IntegrationSettings> _settingsByName;
    private readonly Func<string, IntegrationSettings> _valueFactory;
    private readonly IntegrationSettings[] _settingsById;
    private ICollection<string> _disabledIntegrations;

    /// <summary>
    /// Initializes a new instance of the <see cref="IntegrationSettingsCollection"/> class.
    /// </summary>
    /// <param name="source">The <see cref="IConfigurationSource"/> to use when retrieving configuration values.</param>
    public IntegrationSettingsCollection(IConfigurationSource source)
    {
        _source = source;
        _settingsByName = new ConcurrentDictionary<string, IntegrationSettings>();
        _settingsById = GetIntegrationSettings(source);
        _valueFactory = name =>
        {
            if (IntegrationRegistry.Ids.TryGetValue(name, out var id))
            {
                return _settingsById[id];
            }

            // We have no id for this integration, it will only be available in _settingsByName
            var settings = new IntegrationSettings(name, _source);

            if (_disabledIntegrations?.Contains(name) == true)
            {
                settings.Enabled = false;
            }

            return settings;
        };
    }

    /// <summary>
    /// Gets the <see cref="IntegrationSettings"/> for the specified integration.
    /// </summary>
    /// <param name="integrationName">The name of the integration.</param>
    /// <returns>The integration-specific settings for the specified integration.</returns>
    public IntegrationSettings this[string integrationName] => this[new IntegrationInfo(integrationName)];

    internal IntegrationSettings this[IntegrationInfo integration]
    {
        get
        {
            return integration.Name == null ? _settingsById[integration.Id] : _settingsByName.GetOrAdd(integration.Name, _valueFactory);
        }
    }

    internal void SetDisabledIntegrations(HashSet<string> disabledIntegrationNames)
    {
        if (disabledIntegrationNames == null || disabledIntegrationNames.Count == 0)
        {
            return;
        }

        _disabledIntegrations = disabledIntegrationNames;

        foreach (var settings in _settingsById.Concat(_settingsByName.Values))
        {
            if (disabledIntegrationNames.Contains(settings.IntegrationName))
            {
                settings.Enabled = false;
            }
        }
    }

    private static IntegrationSettings[] GetIntegrationSettings(IConfigurationSource source)
    {
        var integrations = new IntegrationSettings[IntegrationRegistry.Names.Length];

        for (int i = 0; i < integrations.Length; i++)
        {
            var name = IntegrationRegistry.Names[i];

            if (name != null)
            {
                integrations[i] = new IntegrationSettings(name, source);
            }
        }

        return integrations;
    }
}
