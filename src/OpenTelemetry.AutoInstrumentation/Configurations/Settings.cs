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
    private static readonly bool IsYamlConfigEnabled = Environment.GetEnvironmentVariable(ConfigurationKeys.FileBasedConfiguration.Enabled) == "true";
    private static readonly Lazy<YamlConfiguration> YamlConfiguration = new(ReadYamlConfiguration);

    private bool FailFast { get; set; }

    public static T FromDefaultSources<T>(bool failFast)
        where T : Settings, new()
    {
        if (IsYamlConfigEnabled)
        {
            var settings = new T { FailFast = failFast };
            settings.LoadFile(YamlConfiguration.Value);
            return settings;
        }
        else
        {
            var configuration = new Configuration(failFast, new EnvironmentConfigurationSource(failFast));
            var settings = new T { FailFast = failFast };
            settings.LoadEnvVar(configuration);
            return settings;
        }
    }

    public void LoadEnvVar(Configuration configuration)
    {
        OnLoadEnvVar(configuration);
    }

    public void LoadFile(YamlConfiguration configuration)
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
    /// using the specified <see cref="YamlConfiguration"/> to initialize values.
    /// </summary>
    /// <param name="configuration">The <see cref="YamlConfiguration"/> to use when retrieving configuration values.</param>
    protected virtual void OnLoadFile(YamlConfiguration configuration)
    {
        // TODO temporary fallback to env var configuration until we support all settings in yaml
        // TODO make the method abstract when all settings are supported in yaml
        var envVarConfiguration = new Configuration(FailFast, new EnvironmentConfigurationSource(FailFast));
        OnLoadEnvVar(envVarConfiguration);
    }

    private static YamlConfiguration ReadYamlConfiguration()
    {
        var configFile = Environment.GetEnvironmentVariable(ConfigurationKeys.FileBasedConfiguration.FileName) ?? "config.yaml";
        // TODO validate file existence

        var config = Parser.ParseYaml(configFile);

        // TODO validate file format version https://github.com/open-telemetry/opentelemetry-configuration/blob/4f185c07eaaffc18c9ad34a46085e7ad6625fca0/README.md#file-format
        return config;
    }
}
