// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Text.RegularExpressions;
using Vendors.YamlDotNet.RepresentationModel;

namespace OpenTelemetry.AutoInstrumentation.Logging;

internal partial class LoggingConfiguration
{
    // EnvVar configuration Environment Variables
    private const string OtelDotnetAutoLogDirectory = "OTEL_DOTNET_AUTO_LOG_DIRECTORY";
    private const string OtelLogLevel = "OTEL_LOG_LEVEL";
    private const string OtelDotnetAutoLogger = "OTEL_DOTNET_AUTO_LOGGER";
    private const string OtelDotnetAutoLogFileSize = "OTEL_DOTNET_AUTO_LOG_FILE_SIZE";

    // FileBased configuration Environment Variables
    private const string FileBasedConfigurationEnabled = "OTEL_EXPERIMENTAL_FILE_BASED_CONFIGURATION_ENABLED";
    private const string FileBasedConfigurationFileName = "OTEL_EXPERIMENTAL_CONFIG_FILE";

    // Defaults
    private const string NixDefaultDirectory = "/var/log/opentelemetry/dotnet";
    private const long DefaultFileSizeLimitBytes = 10 * 1024 * 1024;

    private static readonly bool IsYamlConfigEnabled = Environment.GetEnvironmentVariable(FileBasedConfigurationEnabled) == "true";

    private static readonly Lazy<Dictionary<string, string?>> YamlConfiguration = new(ReadYamlConfiguration);

    public LoggingConfiguration()
    {
        if (IsYamlConfigEnabled)
        {
            OnLoadFile(YamlConfiguration.Value);
        }
        else
        {
            OnLoadEnvVar();
        }
    }

    public string? LogDirectory { get; private set; }

    public LogLevel? LogLevel { get; private set; } = Logging.LogLevel.Information;

    public long LogFileSize { get; private set; } = DefaultFileSizeLimitBytes;

    public LogSink Logger { get; private set; } = LogSink.File;

    private static LogLevel? GetConfiguredLogLevel(string? configuredValue)
    {
        LogLevel? logLevel = Logging.LogLevel.Information;

        return configuredValue switch
        {
            Constants.ConfigurationValues.LogLevel.Error => Logging.LogLevel.Error,
            Constants.ConfigurationValues.LogLevel.Warning => Logging.LogLevel.Warning,
            Constants.ConfigurationValues.LogLevel.Information => Logging.LogLevel.Information,
            Constants.ConfigurationValues.LogLevel.Debug => Logging.LogLevel.Debug,
            Constants.ConfigurationValues.None => null,
            _ => logLevel
        };
    }

    private static long GetConfiguredFileSizeLimitBytes(string? configuredFileSizeLimit)
    {
        if (string.IsNullOrEmpty(configuredFileSizeLimit))
        {
            return DefaultFileSizeLimitBytes;
        }

        return long.TryParse(configuredFileSizeLimit, out var limit) && limit > 0
            ? limit
            : DefaultFileSizeLimitBytes;
    }

    private static LogSink GetConfiguredLogSink(string? configuredValue)
    {
        // File is the default
        var logSink = LogSink.File;

        return configuredValue switch
        {
            Constants.ConfigurationValues.Loggers.File => LogSink.File,
            Constants.ConfigurationValues.Loggers.Console => LogSink.Console,
            Constants.ConfigurationValues.None => LogSink.NoOp,
            _ => logSink
        };
    }

    private static string? GetLogDirectory(string? logDirectory)
    {
        if (string.IsNullOrEmpty(logDirectory))
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                var windowsDefaultDirectory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                    @"OpenTelemetry .NET AutoInstrumentation",
                    "logs");

                logDirectory = windowsDefaultDirectory;
            }
            else
            {
                // Linux
                logDirectory = NixDefaultDirectory;
            }
        }

        // Try to ensure the directory exists; fallback to temp on failure.
        logDirectory = CreateDirectoryIfMissing(logDirectory!) ?? Path.GetTempPath();
        return logDirectory;
    }

    private static string? CreateDirectoryIfMissing(string pathToCreate)
    {
        try
        {
            Directory.CreateDirectory(pathToCreate);
            return pathToCreate;
        }
        catch
        {
            // Unable to create the directory meaning that the user will have to create it on their own.
            // It is unsafe to log here, so return null to defer deciding what the path is
            return null;
        }
    }

    private static Dictionary<string, string?> ReadYamlConfiguration()
    {
        var configFile = Environment.GetEnvironmentVariable(FileBasedConfigurationFileName) ?? "config.yaml";

        var yaml = File.ReadAllText(configFile);

        var stream = new YamlStream();
        stream.Load(new StringReader(yaml));
        var root = (YamlMappingNode)stream.Documents[0].RootNode;

        string? GetScalar(string key)
        {
            var k = new YamlScalarNode(key);
            if (!root.Children.TryGetValue(k, out var node))
            {
                return null;
            }

            var value = ((YamlScalarNode)node).Value;
            return value is null ? null : ReplaceEnvVariables(value);
        }

        var cfg = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["log_level"] = GetScalar("log_level"),
            ["log_directory"] = GetScalar("log_directory"),
            ["log_file_size"] = GetScalar("log_file_size"),
            ["logger"] = GetScalar("logger")
        };

        return cfg;
    }

    private static string ReplaceEnvVariables(string input)
    {
        return GetEnvVarRegex().Replace(input, match =>
        {
            var varName = match.Groups[1].Value;
            var fallback = match.Groups[2].Success ? match.Groups[2].Value : null;
            var envValue = Environment.GetEnvironmentVariable(varName);
            return envValue ?? fallback ?? match.Value;
        });
    }

#if NET
    [GeneratedRegex(@"\$\{([A-Z0-9_]+)(?::-([^}]*))?\}", RegexOptions.Compiled)]
    private static partial Regex GetEnvVarRegex();
#else
#pragma warning disable SA1201 // A field should not follow a method
    private static readonly Regex EnvVarRegex =
        new(@"\$\{([A-Z0-9_]+)(?::-([^}]*))?\}", RegexOptions.Compiled);
#pragma warning restore SA1201 // A field should not follow a method

    private static Regex GetEnvVarRegex() => EnvVarRegex;
#endif

    private void OnLoadEnvVar()
    {
        try
        {
            LogDirectory = GetLogDirectory(Environment.GetEnvironmentVariable(OtelDotnetAutoLogDirectory));
            LogLevel = GetConfiguredLogLevel(Environment.GetEnvironmentVariable(OtelLogLevel));
            Logger = GetConfiguredLogSink(Environment.GetEnvironmentVariable(OtelDotnetAutoLogger));
            LogFileSize = GetConfiguredFileSizeLimitBytes(Environment.GetEnvironmentVariable(OtelDotnetAutoLogFileSize));
        }
        catch
        {
            // theoretically, can happen when process has no privileges to check env
        }
    }

    private void OnLoadFile(Dictionary<string, string?> configuration)
    {
        try
        {
            LogDirectory = GetLogDirectory(configuration["log_directory"]);
            LogLevel = GetConfiguredLogLevel(configuration["log_level"]);
            Logger = GetConfiguredLogSink(configuration["logger"]);
            LogFileSize = GetConfiguredFileSizeLimitBytes(configuration["log_file_size"]);
        }
        catch
        {
            // theoretically, can happen when process has no privileges to check file
        }
    }
}
