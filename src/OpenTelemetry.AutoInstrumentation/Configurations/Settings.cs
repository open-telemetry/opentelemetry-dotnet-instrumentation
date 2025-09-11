// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;
using OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration.Parser;

namespace OpenTelemetry.AutoInstrumentation.Configurations;

/// <summary>
/// Global Settings
/// </summary>
internal abstract class Settings
{
    public static T FromDefaultSources<T>(bool failFast)
        where T : Settings, new()
    {
        var isConfigFileEnabled = Environment.GetEnvironmentVariable(ConfigurationKeys.FileBasedConfiguration.Enabled) == "true";

        if (isConfigFileEnabled)
        {
            var configFile = Environment.GetEnvironmentVariable(ConfigurationKeys.FileBasedConfiguration.FileName) ?? "config.yaml";
            var config = Parser.ParseYaml(configFile);
            var settings = new T();
            settings.LoadFile(config);
            return settings;
        }
        else
        {
            var configuration = new Configuration(failFast, new EnvironmentConfigurationSource(failFast));
            var settings = new T();
            settings.LoadEnvVar(configuration);
            return settings;
        }
    }

    public void LoadEnvVar(Configuration configuration)
    {
        OnLoadEnvVar(configuration);
    }

    public void LoadFile(Conf configuration)
    {
        OnLoadFile(configuration);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Settings"/> class
    /// using the specified <see cref="Configuration"/> to initialize values.
    /// </summary>
    /// <param name="configuration">The <see cref="Configuration"/> to use when retrieving configuration values.</param>
    protected abstract void OnLoadEnvVar(Configuration configuration);

    /// <summary>
    /// Initializes a new instance of the <see cref="Settings"/> class
    /// using the specified <see cref="Conf"/> to initialize values.
    /// </summary>
    /// <param name="configuration">The <see cref="Conf"/> to use when retrieving configuration values.</param>
    protected virtual void OnLoadFile(Conf configuration)
    {
    }
}
