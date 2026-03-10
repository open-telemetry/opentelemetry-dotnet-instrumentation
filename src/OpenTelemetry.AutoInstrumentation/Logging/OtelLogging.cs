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
    private const string OtelDotnetAutoLogger = "OTEL_DOTNET_AUTO_LOGGER";
    private const string NixDefaultDirectory = "/var/log/opentelemetry/dotnet";

    private static readonly long FileSizeLimitBytes = GetConfiguredFileSizeLimitBytes();
    private static readonly ConcurrentDictionary<string, IOtelLogger> OtelLoggers = new();

    private static LogLevel? _configuredLogLevel = GetConfiguredLogLevel();
    private static LogSink _configuredLogSink = GetConfiguredLogSink();

    /// <summary>
    /// Returns Logger implementation.
    /// </summary>
    /// <returns>Logger</returns>
    public static IOtelLogger GetLogger()
    {
        // Default to managed logs
        return GetLogger("Managed");
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

    public static void CloseLogger(string suffix, IOtelLogger otelLogger)
    {
        try
        {
            // Update logger associated with the key, so that future calls to GetLogger
            // return NoopLogger.
            if (OtelLoggers.TryUpdate(suffix, NoopLogger.Instance, otelLogger))
            {
                otelLogger.Close();
            }
        }
#pragma warning disable CA1031 // Do not catch general exception
        catch (Exception)
#pragma warning restore CA1031 // Do not catch general exception
        {
            // intentionally empty
        }
    }

    // Helper method for testing
    internal static void Reset()
    {
        _configuredLogLevel = GetConfiguredLogLevel();
        _configuredLogSink = GetConfiguredLogSink();
        OtelLoggers.Clear();
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
#pragma warning disable CA1031 // Do not catch general exception
        catch (Exception)
#pragma warning restore CA1031 // Do not catch general exception
        {
            // theoretically, can happen when process has no privileges to check env
        }

        return logLevel;
    }

    internal static LogSink GetConfiguredLogSink()
    {
        // Use File as a default sink
        var logSink = LogSink.File;

        try
        {
            var configuredValue = Environment.GetEnvironmentVariable(OtelDotnetAutoLogger) ?? string.Empty;

            logSink = configuredValue switch
            {
                Constants.ConfigurationValues.Loggers.File => LogSink.File,
                Constants.ConfigurationValues.Loggers.Console => LogSink.Console,
                Constants.ConfigurationValues.None => LogSink.NoOp,
                _ => logSink
            };
        }
#pragma warning disable CA1031 // Do not catch general exception
        catch (Exception)
#pragma warning restore CA1031 // Do not catch general exception
        {
            // theoretically, can happen when process has no privileges to check env
        }

        return logSink;
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
#pragma warning disable CA1031 // Do not catch general exception
        catch (Exception)
#pragma warning restore CA1031 // Do not catch general exception
        {
            // theoretically, can happen when process has no privileges to check env
            return defaultFileSizeLimitBytes;
        }
    }

    private static IOtelLogger CreateLogger(string suffix)
    {
        if (!_configuredLogLevel.HasValue)
        {
            return NoopLogger.Instance;
        }

        var sink = CreateSink(suffix);

        return new InternalLogger(sink, _configuredLogLevel.Value);
    }

    private static ISink CreateSink(string suffix)
    {
        // Uses ISink? here, sink creation can fail so we specify default fallback at the end.
        ISink? sink = _configuredLogSink switch
        {
            LogSink.NoOp => NoopSink.Instance,
            LogSink.Console => new ConsoleSink(suffix),
            LogSink.File => CreateFileSink(suffix),
            // default to null, then default value is specified only at the end.
            _ => null,
        };

        return sink ??
            // Default to NoopSink
            NoopSink.Instance;
    }

    private static PeriodicFlushToDiskSink? CreateFileSink(string suffix)
    {
        try
        {
            var logDirectory = GetLogDirectory();
            if (logDirectory != null)
            {
                var fileName = GetLogFileName(suffix);
                var logPath = Path.Combine(logDirectory, fileName);

                var rollingFileSink = new RollingFileSink(
                    path: logPath,
                    fileSizeLimitBytes: FileSizeLimitBytes,
                    retainedFileCountLimit: 10,
                    rollingInterval: RollingInterval.Day,
                    rollOnFileSizeLimit: true,
                    retainedFileTimeLimit: null);
                return new PeriodicFlushToDiskSink(rollingFileSink, TimeSpan.FromSeconds(5));
            }
        }
#pragma warning disable CA1031 // Do not catch general exception
        catch (Exception)
#pragma warning restore CA1031 // Do not catch general exception
        {
            // unable to configure logging to a file
        }

        // Could not create file sink
        return null;
    }

    private static string GetLogFileName(string suffix)
    {
        try
        {
            using var process = Process.GetCurrentProcess();
            var appDomainName = GetEncodedAppDomainName();

            return string.IsNullOrEmpty(suffix)
                ? $"otel-dotnet-auto-{process.Id}-{appDomainName}-.log"
                : $"otel-dotnet-auto-{process.Id}-{appDomainName}-{suffix}-.log";
        }
#pragma warning disable CA1031 // Do not catch general exception
        catch (Exception)
#pragma warning restore CA1031 // Do not catch general exception
        {
            // We can't get the process info
            return string.IsNullOrEmpty(suffix)
                ? $"otel-dotnet-auto-{Guid.NewGuid()}-.log"
                : $"otel-dotnet-auto-{Guid.NewGuid()}-{suffix}-.log";
        }
    }

    private static string GetEncodedAppDomainName()
    {
        var name = AppDomain.CurrentDomain.FriendlyName;
        return name
            .Replace(Path.DirectorySeparatorChar, '-')
            .Replace(Path.AltDirectorySeparatorChar, '-')
            .Trim('-');
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
#pragma warning disable CA1031 // Do not catch general exception
        catch (Exception)
#pragma warning restore CA1031 // Do not catch general exception
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
#pragma warning disable CA1031 // Do not catch general exception
        catch (Exception)
#pragma warning restore CA1031 // Do not catch general exception
        {
            // Unable to create the directory meaning that the user will have to create it on their own.
            // It is unsafe to log here, so return null to defer deciding what the path is
            return null;
        }
    }
}
