// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Concurrent;
using System.Diagnostics;

namespace OpenTelemetry.AutoInstrumentation.Logging;

/// <summary>
/// Configures shared logger used by instrumentations.
/// </summary>
internal static class OtelLogging
{
    private const string OtelDotnetAutoLogDirectory = "OTEL_DOTNET_AUTO_LOG_DIRECTORY";
    private const string OtelLogLevel = "OTEL_LOG_LEVEL";
    private const string OtelDotnetAutoLogFileSize = "OTEL_DOTNET_AUTO_LOG_FILE_SIZE";
    private const string NixDefaultDirectory = "/var/log/opentelemetry/dotnet";

    private static readonly long FileSizeLimitBytes = GetConfiguredFileSizeLimitBytes();
    private static readonly LogLevel? ConfiguredLogLevel = GetConfiguredLogLevel();

    private static readonly ConcurrentDictionary<string, IOtelLogger> OtelLoggers = new();

    /// <summary>
    /// Returns Logger implementation.
    /// </summary>
    /// <returns>Logger</returns>
    public static IOtelLogger GetLogger()
    {
        return GetLogger(string.Empty);
    }

    /// <summary>
    /// Returns Logger implementation.
    /// </summary>
    /// <param name="suffix">Suffix of the log file.</param>
    /// <returns>Logger</returns>
    public static IOtelLogger GetLogger(string suffix)
    {
        return OtelLoggers.GetOrAdd(suffix, CreateLogger);
    }

    internal static LogLevel? GetConfiguredLogLevel()
    {
        LogLevel? logLevel = LogLevel.Information;
        try
        {
            var configuredValue = Environment.GetEnvironmentVariable(OtelLogLevel) ?? string.Empty;

            logLevel = configuredValue switch
            {
                Constants.ConfigurationValues.LogLevel.Error => LogLevel.Error,
                Constants.ConfigurationValues.LogLevel.Warning => LogLevel.Warning,
                Constants.ConfigurationValues.LogLevel.Information => LogLevel.Information,
                Constants.ConfigurationValues.LogLevel.Debug => LogLevel.Debug,
                Constants.ConfigurationValues.None => null,
                _ => logLevel
            };
        }
        catch (Exception)
        {
            // theoretically, can happen when process has no privileges to check env
        }

        return logLevel;
    }

    internal static long GetConfiguredFileSizeLimitBytes()
    {
        const long defaultFileSizeLimitBytes = 10 * 1024 * 1024;

        try
        {
            var configuredFileSizeLimit = Environment.GetEnvironmentVariable(OtelDotnetAutoLogFileSize);
            if (string.IsNullOrEmpty(configuredFileSizeLimit))
            {
                return defaultFileSizeLimitBytes;
            }

            return long.TryParse(configuredFileSizeLimit, out var limit) && limit > 0 ? limit : defaultFileSizeLimitBytes;
        }
        catch (Exception)
        {
            // theoretically, can happen when process has no privileges to check env
            return defaultFileSizeLimitBytes;
        }
    }

    private static IOtelLogger CreateLogger(string suffix)
    {
        ISink? sink = null;

        if (!ConfiguredLogLevel.HasValue)
        {
            return NoopLogger.Instance;
        }

        try
        {
            var logDirectory = GetLogDirectory();
            if (logDirectory != null)
            {
                var fileName = GetLogFileName(suffix);
                var logPath = Path.Combine(logDirectory, fileName);
                sink = new RollingFileSink(
                    path: logPath,
                    fileSizeLimitBytes: FileSizeLimitBytes,
                    retainedFileCountLimit: 10,
                    rollingInterval: RollingInterval.Day,
                    rollOnFileSizeLimit: true,
                    retainedFileTimeLimit: null);
            }
        }
        catch (Exception)
        {
            // unable to configure logging to a file
        }

        sink ??= new NoopSink();

        return new InternalLogger(sink, ConfiguredLogLevel.Value);
    }

    private static string GetLogFileName(string suffix)
    {
        try
        {
            using var process = Process.GetCurrentProcess();
            var appDomainName = AppDomain.CurrentDomain.FriendlyName;

            return string.IsNullOrEmpty(suffix)
                ? $"otel-dotnet-auto-{appDomainName}-{process.Id}-.log"
                : $"otel-dotnet-auto-{appDomainName}-{process.Id}-{suffix}-.log";
        }
        catch
        {
            // We can't get the process info
            return string.IsNullOrEmpty(suffix)
                ? $"otel-dotnet-auto-{Guid.NewGuid()}-.log"
                : $"otel-dotnet-auto-{Guid.NewGuid()}-{suffix}-.log";
        }
    }

    private static string? GetLogDirectory()
    {
        string? logDirectory;

        try
        {
            logDirectory = Environment.GetEnvironmentVariable(OtelDotnetAutoLogDirectory);

            if (logDirectory == null)
            {
                if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                {
                    var windowsDefaultDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"OpenTelemetry .NET AutoInstrumentation", "logs");
                    logDirectory = windowsDefaultDirectory;
                }
                else
                {
                    // Linux
                    logDirectory = NixDefaultDirectory;
                }
            }

            logDirectory = CreateDirectoryIfMissing(logDirectory) ?? Path.GetTempPath();
        }
        catch
        {
            // The try block may throw a SecurityException if not granted the System.Security.Permissions.FileIOPermission
            // because of the following API calls
            //   - Directory.Exists
            //   - Environment.GetFolderPath
            //   - Path.GetTempPath

            // Unsafe to log
            logDirectory = null;
        }

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
}
