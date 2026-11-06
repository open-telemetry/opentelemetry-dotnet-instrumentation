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
    private const string DefaultConfigFile = "config.yaml";

    private static readonly bool IsYamlConfigEnabled = GetYamlConfigurationValue(ConfigurationKeys.FileBasedConfiguration.Enabled) == "true";
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

#if NETFRAMEWORK
    // Each ASP.NET application selects its own YAML file, so in hosted applications
    // the Web.config value takes precedence over the process environment variable.
    internal static string? GetYamlConfigurationValue(string key, System.Collections.Specialized.NameValueCollection appSettings, bool isHosted)
    {
        return ProvidedByAppSettings(key, appSettings, isHosted) ? appSettings[key] : Environment.GetEnvironmentVariable(key);
    }

    internal static string GetYamlConfigurationFilePath(System.Collections.Specialized.NameValueCollection appSettings, bool isHosted, string applicationDirectory)
    {
        var configFile = GetYamlConfigurationValue(ConfigurationKeys.FileBasedConfiguration.FileName, appSettings, isHosted);
        var resolveFromApplication = ProvidedByAppSettings(ConfigurationKeys.FileBasedConfiguration.FileName, appSettings, isHosted);

        if (string.IsNullOrEmpty(configFile))
        {
            configFile = DefaultConfigFile;

            // The default file belongs to the application that enabled YAML in its Web.config.
            resolveFromApplication = ProvidedByAppSettings(ConfigurationKeys.FileBasedConfiguration.Enabled, appSettings, isHosted);
        }

        // Resolve Web.config paths against the application root, like other Web.config paths.
        // The IIS worker process directory is %windir%\System32\inetsrv for every application.
        return resolveFromApplication && !Path.IsPathRooted(configFile) ? Path.Combine(applicationDirectory, configFile) : configFile!;
    }
#endif

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
#if NETFRAMEWORK
        var configFile = GetYamlConfigurationFilePath(System.Configuration.ConfigurationManager.AppSettings, System.Web.Hosting.HostingEnvironment.IsHosted, AppDomain.CurrentDomain.BaseDirectory);
#else
        var configFile = Environment.GetEnvironmentVariable(ConfigurationKeys.FileBasedConfiguration.FileName);

        if (string.IsNullOrEmpty(configFile))
        {
            configFile = DefaultConfigFile;
        }
#endif

        if (!File.Exists(configFile))
        {
            Logger.Error($"Configuration file '{configFile}' was not found.");
            return new YamlConfiguration();
        }

        var config = Parser.ParseYaml<YamlConfiguration>(configFile);

        // TODO validate file format version https://github.com/open-telemetry/opentelemetry-configuration/blob/4f185c07eaaffc18c9ad34a46085e7ad6625fca0/README.md#file-format
        return config;
    }

    private static string? GetYamlConfigurationValue(string key)
    {
#if NETFRAMEWORK
        return GetYamlConfigurationValue(key, System.Configuration.ConfigurationManager.AppSettings, System.Web.Hosting.HostingEnvironment.IsHosted);
#else
        return Environment.GetEnvironmentVariable(key);
#endif
    }

#if NETFRAMEWORK
    private static bool ProvidedByAppSettings(string key, System.Collections.Specialized.NameValueCollection appSettings, bool isHosted)
    {
        return isHosted && !string.IsNullOrEmpty(appSettings[key]);
    }
#endif
}
