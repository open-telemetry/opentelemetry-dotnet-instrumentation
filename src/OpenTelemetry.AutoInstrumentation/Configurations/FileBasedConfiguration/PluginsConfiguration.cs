// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Vendors.YamlDotNet.Serialization;

namespace OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;

internal class PluginsConfiguration
{
    /// <summary>
    /// Gets or sets the list of plugins.
    /// </summary>
    [YamlMember(Alias = "plugins")]
    public List<string>? Plugins { get; set; }

    /// <summary>
    /// Gets or sets the plugins list.
    /// </summary>
    [YamlMember(Alias = "plugins_list")]
    public string? PluginsList { get; set; }

    public List<string> ParsePlugins()
    {
        var uniquePlugins = new HashSet<string>();

        if (Plugins != null)
        {
            foreach (var plugin in Plugins)
            {
                if (!string.IsNullOrWhiteSpace(plugin))
                {
                    uniquePlugins.Add(plugin.Trim());
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(PluginsList))
        {
            foreach (var plugin in PluginsList!.Split(Constants.ConfigurationValues.DotNetQualifiedNameSeparator))
            {
                uniquePlugins.Add(plugin.Trim());
            }
        }

        return uniquePlugins.ToList();
    }
}
