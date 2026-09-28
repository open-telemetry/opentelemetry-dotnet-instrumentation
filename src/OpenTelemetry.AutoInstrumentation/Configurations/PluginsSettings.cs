// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;

namespace OpenTelemetry.AutoInstrumentation.Configurations;

internal class PluginsSettings : Settings
{
    /// <summary>
    /// Gets a value indicating whether invalid plugin configuration should fail initialization.
    /// </summary>
    public bool FailFast { get; private set; }

    /// <summary>
    /// Gets the list of plugins represented by <see cref="Type.AssemblyQualifiedName"/>.
    /// </summary>
    public IList<string> Plugins { get; private set; } = [];

    protected override void OnLoadEnvVar(Configuration configuration)
    {
        FailFast = configuration.FailFast;

        var providerPlugins = configuration.GetString(ConfigurationKeys.ProviderPlugins);
        if (providerPlugins != null && !string.IsNullOrWhiteSpace(providerPlugins))
        {
            foreach (var pluginAssemblyQualifiedName in providerPlugins.Split([Constants.ConfigurationValues.DotNetQualifiedNameSeparator], StringSplitOptions.RemoveEmptyEntries))
            {
                var normalizedPluginAssemblyQualifiedName = pluginAssemblyQualifiedName.Trim();
                if (normalizedPluginAssemblyQualifiedName.Length > 0)
                {
                    Plugins.Add(normalizedPluginAssemblyQualifiedName);
                }
            }
        }
    }

    protected override void OnLoadFile(YamlConfiguration configuration)
    {
        FailFast = configuration.FailFast;
        Plugins = configuration.Plugins?.ParsePlugins() ?? [];
    }
}
