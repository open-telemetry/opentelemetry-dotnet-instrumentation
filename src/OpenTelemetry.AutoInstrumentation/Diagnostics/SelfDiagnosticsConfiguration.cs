// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.AutoInstrumentation.Logging;
using OpenTelemetry.AutoInstrumentation.Plugins;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.SelfDiagnostics;
using OpenTelemetry.Trace;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace OpenTelemetry.AutoInstrumentation.Diagnostics;

/// <summary>
/// Configures the SDK's <see cref="SelfDiagnosticsOptions"/> using automatic instrumentation logging settings.
/// </summary>
/// <remarks>
/// Explicit SDK settings take precedence over automatic instrumentation settings and defaults.
/// Plugins can override the resulting options.
/// </remarks>
internal static class SelfDiagnosticsConfiguration
{
    private const string OtelLogLevel = "OTEL_LOG_LEVEL";
    private const string OtelDotnetAutoLogFileSize = "OTEL_DOTNET_AUTO_LOG_FILE_SIZE";
    private const string OtelDotnetSelfDiagnosticsSinks = "OTEL_DOTNET_SELF_DIAGNOSTICS_SINKS";
    private const string OtelDotnetSelfDiagnosticsLogDirectory = "OTEL_DOTNET_SELF_DIAGNOSTICS_LOG_DIRECTORY";

    private static readonly IOtelLogger Logger = OtelLogging.GetLogger();

    /// <summary>
    /// Registers the self-diagnostics mapping on a tracer provider builder.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="pluginManager">The plugin manager, or null when plugins are not available.</param>
    /// <returns>The builder, for chaining.</returns>
    internal static TracerProviderBuilder ConfigureSelfDiagnostics(this TracerProviderBuilder builder, PluginManager? pluginManager)
    {
        return builder.ConfigureServices(services => services.Configure<SelfDiagnosticsOptions>(options => Configure(options, pluginManager)));
    }

    /// <summary>
    /// Registers the self-diagnostics mapping on a meter provider builder.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="pluginManager">The plugin manager, or null when plugins are not available.</param>
    /// <returns>The builder, for chaining.</returns>
    internal static MeterProviderBuilder ConfigureSelfDiagnostics(this MeterProviderBuilder builder, PluginManager? pluginManager)
    {
        return builder.ConfigureServices(services => services.Configure<SelfDiagnosticsOptions>(options => Configure(options, pluginManager)));
    }

    /// <summary>
    /// Registers the self-diagnostics mapping on a logger provider builder.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="pluginManager">The plugin manager, or null when plugins are not available.</param>
    /// <returns>The builder, for chaining.</returns>
    internal static LoggerProviderBuilder ConfigureSelfDiagnostics(this LoggerProviderBuilder builder, PluginManager? pluginManager)
    {
        return builder.ConfigureServices(services => services.Configure<SelfDiagnosticsOptions>(options => Configure(options, pluginManager)));
    }

#if NET
    /// <summary>
    /// Configures self-diagnostics for the Microsoft ILogger bridge.
    /// </summary>
    /// <param name="builder">The logging builder.</param>
    /// <param name="pluginManager">The plugin manager, or null when plugins are not available.</param>
    /// <returns>The builder, for chaining.</returns>
    internal static ILoggingBuilder ConfigureSelfDiagnostics(this ILoggingBuilder builder, PluginManager? pluginManager)
    {
        builder.Services.Configure<SelfDiagnosticsOptions>(options => Configure(options, pluginManager));
        // The SDK registers its diagnostics listener before running provider builder callbacks.
        builder.Services.ConfigureOpenTelemetryLoggerProvider((_, _) => OtelLogging.NotifySdkAvailable());
        return builder;
    }
#endif

    /// <summary>
    /// Applies the automatic instrumentation logging settings to <paramref name="options"/>
    /// and then lets plugins adjust the result. Never throws: this runs inside the SDK's
    /// options factory during provider <c>Build()</c>.
    /// </summary>
    /// <param name="options">The options, already populated by the SDK from its own environment variables.</param>
    /// <param name="pluginManager">The plugin manager, or null when plugins are not available.</param>
    internal static void Configure(SelfDiagnosticsOptions options, PluginManager? pluginManager)
    {
        try
        {
            ApplyAutomaticInstrumentationSettings(options);
        }
#pragma warning disable CA1031 // Do not catch general exception
        catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception
        {
            Logger.Error(ex, "Failed to configure SelfDiagnosticsOptions from automatic instrumentation logging settings.");
        }

        try
        {
            // Plugins can override both SDK and automatic instrumentation settings.
            pluginManager?.ConfigureSelfDiagnosticsOptions(options);
        }
#pragma warning disable CA1031 // Do not catch general exception
        catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception
        {
            Logger.Error(ex, "Plugin failed to configure SelfDiagnosticsOptions.");
        }

        OtelLogging.SetSelfDiagnosticsOptions(options);
    }

    private static void ApplyAutomaticInstrumentationSettings(SelfDiagnosticsOptions options)
    {
        // Preserve the automatic instrumentation default; the SDK has already parsed explicit values.
        if (!IsSet(OtelLogLevel))
        {
            options.MinimumLevel = LogLevel.Information;
        }

        if (!IsSet(OtelDotnetSelfDiagnosticsSinks))
        {
            // An explicit SDK log directory wins over the agent's sink choice, as for every other SDK setting.
            var sdkLogDirectoryIsSet = IsSet(OtelDotnetSelfDiagnosticsLogDirectory);

            switch (OtelLogging.GetConfiguredLogSink())
            {
                case LogSink.Console:
                    options.LogToStdout = true;
                    options.LogToStderr = false;

                    if (!sdkLogDirectoryIsSet)
                    {
                        options.LogDirectory = null;
                    }

                    break;

                case LogSink.NoOp:
                    options.LogToStdout = false;
                    options.LogToStderr = false;

                    if (!sdkLogDirectoryIsSet)
                    {
                        options.LogDirectory = null;
                    }

                    break;

                case LogSink.File:
                default:
                    options.LogToStdout = false;
                    options.LogToStderr = false;

                    if (!sdkLogDirectoryIsSet)
                    {
                        options.LogDirectory = OtelLogging.GetLogDirectory();
                    }

                    break;
            }
        }

        // There is no SDK variable for the file size. The SDK default (10,240 KB) equals the
        // automatic instrumentation default (10 MiB), so only an explicit value is mapped.
        if (IsSet(OtelDotnetAutoLogFileSize))
        {
            var bytes = OtelLogging.GetConfiguredFileSizeLimitBytes();
            var kilobytes = ((bytes - 1) / 1024) + 1;

            options.FileSizeLimitKilobytes = (int)Math.Min(int.MaxValue, kilobytes);
        }
    }

    private static bool IsSet(string environmentVariableName) =>
        !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(environmentVariableName));
}
