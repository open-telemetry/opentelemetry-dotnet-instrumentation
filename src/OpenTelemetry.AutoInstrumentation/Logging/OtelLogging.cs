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
    private static readonly ConcurrentDictionary<string, IOtelLogger> OtelLoggers = new();

    // Keep configuration as a replaceable instance so Reset() can rebuild it.
    private static LoggingConfiguration _config = new();

    /// <summary>
    /// Returns Logger implementation.
    /// </summary>
    public static IOtelLogger GetLogger()
    {
        return GetLogger("Managed");
    }

    /// <summary>
    /// Returns Logger implementation for the given suffix.
    /// </summary>
    public static IOtelLogger GetLogger(string suffix)
    {
        return OtelLoggers.GetOrAdd(suffix, CreateLogger);
    }

    public static void CloseLogger(string suffix, IOtelLogger otelLogger)
    {
        try
        {
            if (OtelLoggers.TryUpdate(suffix, NoopLogger.Instance, otelLogger))
            {
                otelLogger.Close();
            }
        }
        catch
        {
            // intentionally empty
        }
    }

    internal static void Reset()
    {
        // Rebuild configuration from current source (YAML or ENV).
        _config = new LoggingConfiguration();
        OtelLoggers.Clear();
    }

    private static IOtelLogger CreateLogger(string suffix)
    {
        // If configuration explicitly disables logging (LogLevel == null), return Noop.
        if (!_config.LogLevel.HasValue)
        {
            return NoopLogger.Instance;
        }

        var sink = CreateSink(suffix);
        return new InternalLogger(sink, _config.LogLevel.Value);
    }

    private static ISink CreateSink(string suffix)
    {
        // Try to create the configured sink; fall back to Noop on any failure.
        ISink? sink = _config.Logger switch
        {
            LogSink.NoOp => NoopSink.Instance,
            LogSink.Console => new ConsoleSink(suffix),
            LogSink.File => CreateFileSink(suffix),
            _ => null
        };

        // Default to NoopSink if creation failed.
        return sink ?? NoopSink.Instance;
    }

    private static ISink? CreateFileSink(string suffix)
    {
        try
        {
            var logDirectory = _config.LogDirectory;
            if (!string.IsNullOrEmpty(logDirectory))
            {
                var fileName = GetLogFileName(suffix);
                var logPath = Path.Combine(logDirectory!, fileName);

                var rollingFileSink = new RollingFileSink(
                    path: logPath,
                    fileSizeLimitBytes: _config.LogFileSize,
                    retainedFileCountLimit: 10,
                    rollingInterval: RollingInterval.Day,
                    rollOnFileSizeLimit: true,
                    retainedFileTimeLimit: null);

                return new PeriodicFlushToDiskSink(rollingFileSink, TimeSpan.FromSeconds(5));
            }
        }
        catch
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
        catch
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
}
