// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;
using OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration.Parser;
using OpenTelemetry.AutoInstrumentation.Logging;

namespace OpenTelemetry.AutoInstrumentation.Configurations;

/// <summary>
/// Global Settings
/// </summary>
internal abstract class Settings
{
    private static readonly bool IsYamlConfigEnabled = Environment.GetEnvironmentVariable(ConfigurationKeys.FileBasedConfiguration.Enabled) == "true";
    private static readonly Lazy<YamlConfiguration> YamlConfiguration = new(ReadYamlConfiguration);
    private static readonly IOtelLogger Logger = OtelLogging.GetLogger();

    public static T FromDefaultSources<T>(bool failFast)
        where T : Settings, new()
    {
        if (IsYamlConfigEnabled)
        {
            var settings = new T();
            settings.LoadFile(YamlConfiguration.Value);
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
    protected abstract void OnLoadFile(YamlConfiguration configuration);

    private static YamlConfiguration ReadYamlConfiguration()
    {
        var experimentalConfigFile = Environment.GetEnvironmentVariable(ConfigurationKeys.FileBasedConfiguration.ExperimentalFileName);

        var configFile = Environment.GetEnvironmentVariable(ConfigurationKeys.FileBasedConfiguration.FileName);

        if (!string.IsNullOrEmpty(configFile))
        {
            if (!string.IsNullOrEmpty(experimentalConfigFile))
            {
                Logger.Warning("Both OTEL_EXPERIMENTAL_CONFIG_FILE (deprecated) and OTEL_CONFIG_FILE are set. " +
                    "Using OTEL_CONFIG_FILE and ignoring the deprecated variable.");
            }
        }
        else if (!string.IsNullOrEmpty(experimentalConfigFile))
        {
            Logger.Warning("OTEL_EXPERIMENTAL_CONFIG_FILE is deprecated. Please use OTEL_CONFIG_FILE instead.");
            configFile = experimentalConfigFile;
        }
        else
        {
            configFile = "config.yaml";
        }

        if (!File.Exists(configFile))
        {
            Logger.Error($"Configuration file '{configFile}' was not found.");
            throw new FileNotFoundException($"Configuration file '{configFile}' was not found.", configFile);
        }

        var config = Parser.ParseYaml<YamlConfiguration>(configFile);

        // TODO validate file format version https://github.com/open-telemetry/opentelemetry-configuration/blob/4f185c07eaaffc18c9ad34a46085e7ad6625fca0/README.md#file-format
        return config;
    }
}
