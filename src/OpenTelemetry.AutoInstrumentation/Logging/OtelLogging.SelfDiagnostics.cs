// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.SelfDiagnostics;
using SdkLogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace OpenTelemetry.AutoInstrumentation.Logging;

/// <summary>
/// Routing of the managed loggers through the OpenTelemetry SDK self-diagnostics.
/// </summary>
internal static partial class OtelLogging
{
    private const string ManagedLoggerSuffix = "Managed";
    private const string OpAmpLoggerSuffix = "OpAmp";

    // Creation and notifications share the dictionary lock so late loggers inherit the process-wide routing mode.
    private static SelfDiagnosticsLogger.RoutingMode _routingMode;
    private static LogLevel? _selfDiagnosticsLogLevel = GetSelfDiagnosticsLogLevel();
    private static SelfDiagnosticsOptions? _selfDiagnosticsOptions;

    internal static void SetSelfDiagnosticsOptions(SelfDiagnosticsOptions options)
    {
        // Keep the instance: any later ConfigureServices callbacks finish updating it before Build returns.
        Volatile.Write(ref _selfDiagnosticsOptions, options);
    }

    internal static void WriteShutdownError(Exception exception)
    {
        var options = Volatile.Read(ref _selfDiagnosticsOptions);

        if (IsSelfDiagnosticsDisabled(options))
        {
            return;
        }

        Console.Error.WriteLine($"OpenTelemetry Automatic Instrumentation shutdown failed: {exception}");
    }

    /// <summary>
    /// Routes managed loggers to SDK self-diagnostics and replays their buffered entries.
    /// </summary>
    /// <remarks>
    /// Routing changes only when the managed event source has an enabled listener.
    /// </remarks>
    internal static void NotifySdkAvailable()
    {
        SelfDiagnosticsLogger[] loggers;
        lock (OtelLoggers)
        {
            // External diagnostic listeners also enable this source and suppress fallback logging.
            if (!AutoInstrumentationEventSource.Log.IsEnabled())
            {
                return;
            }

            _routingMode = SelfDiagnosticsLogger.RoutingMode.Sdk;
            loggers = OtelLoggers.Values.OfType<SelfDiagnosticsLogger>().ToArray();
        }

        foreach (var logger in loggers)
        {
            logger.OnSdkAvailable();
        }
    }

    /// <summary>
    /// Flushes buffered entries to the fallback sink and uses it for subsequent entries.
    /// </summary>
    /// <remarks>
    /// Existing SDK routing is unaffected.
    /// </remarks>
    internal static void UseFallbackIfStillBuffering()
    {
        SelfDiagnosticsLogger[] loggers;

        lock (OtelLoggers)
        {
            if (_routingMode != SelfDiagnosticsLogger.RoutingMode.Buffering)
            {
                return;
            }

            _routingMode = SelfDiagnosticsLogger.RoutingMode.Fallback;

            loggers = [.. OtelLoggers.Values.OfType<SelfDiagnosticsLogger>()];
        }

        foreach (var logger in loggers)
        {
            logger.UseFallbackIfStillBuffering();
        }
    }

    static partial void TryGetSelfDiagnosticsLogger(string suffix, ref IOtelLogger? logger)
    {
        if (suffix is not (ManagedLoggerSuffix or OpAmpLoggerSuffix))
        {
            return;
        }

        lock (OtelLoggers)
        {
            logger = OtelLoggers.GetOrAdd(suffix, CreateSelfDiagnosticsLogger);
        }
    }

    private static IOtelLogger CreateSelfDiagnosticsLogger(string suffix)
    {
        var sdkMessagePrefix = suffix == ManagedLoggerSuffix ? string.Empty : $"[{suffix}] ";
        return new SelfDiagnosticsLogger(_selfDiagnosticsLogLevel, () => CreateSelfDiagnosticsFallbackSink(suffix), sdkMessagePrefix, _routingMode);
    }

    private static ISink CreateSelfDiagnosticsFallbackSink(string suffix)
    {
        var options = Volatile.Read(ref _selfDiagnosticsOptions);

        if (IsSelfDiagnosticsDisabled(options))
        {
            return NoopSink.Instance;
        }

        return CreateSink(suffix);
    }

    private static bool IsSelfDiagnosticsDisabled(SelfDiagnosticsOptions? options) =>
        options == null
            ? !_selfDiagnosticsLogLevel.HasValue || _configuredLogSink == LogSink.NoOp
            : options.MinimumLevel == SdkLogLevel.None ||
              (string.IsNullOrEmpty(options.LogDirectory) && !options.LogToStdout && !options.LogToStderr);

    private static LogLevel? GetSelfDiagnosticsLogLevel()
    {
        // The main loggers accept the SDK vocabulary; the linked projects keep the legacy parser.
        try
        {
            var value = Environment.GetEnvironmentVariable(OtelLogLevel);
            if (string.IsNullOrWhiteSpace(value))
            {
                return LogLevel.Information;
            }

            return value.Trim().ToUpperInvariant() switch
            {
                "ERROR" => LogLevel.Error,
                "WARN" or "WARNING" => LogLevel.Warning,
                "INFO" or "INFORMATION" => LogLevel.Information,
                "DEBUG" or "TRACE" => LogLevel.Debug,
                "NONE" => null,
                _ => LogLevel.Warning,
            };
        }
#pragma warning disable CA1031 // Do not catch general exception
        catch (Exception)
#pragma warning restore CA1031 // Do not catch general exception
        {
            return LogLevel.Information;
        }
    }

    static partial void ResetSelfDiagnostics()
    {
        lock (OtelLoggers)
        {
            foreach (var logger in OtelLoggers.Values)
            {
                if (logger is SelfDiagnosticsLogger selfDiagnosticsLogger)
                {
                    selfDiagnosticsLogger.Close();
                }
            }

            _routingMode = SelfDiagnosticsLogger.RoutingMode.Buffering;
            _selfDiagnosticsLogLevel = GetSelfDiagnosticsLogLevel();
            _selfDiagnosticsOptions = null;
        }
    }
}
