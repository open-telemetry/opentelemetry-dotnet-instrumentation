// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.AutoInstrumentation.Configurations;
using OpenTelemetry.Logs;

namespace OpenTelemetry.AutoInstrumentation.Logger;

internal static class LoggerInitializer
{
    private static volatile bool _initializedAtLeastOnce;
    private static Type? _loggingProviderSdkType;

    public static bool IsInitializedAtLeastOnce => _initializedAtLeastOnce;

    // this method is only called from LoggingBuilderIntegration
    public static void AddOpenTelemetryLogsFromIntegration(ILoggingBuilder builder)
    {
        if (builder.Services is ServiceCollection)
        {
            AddOpenTelemetryLogs(builder);
        }
    }

    // this method is only called from BootstrapperHostingStartup
    public static void AddOpenTelemetryLogsFromStartup(this ILoggingBuilder builder)
    {
        AddOpenTelemetryLogs(builder);
    }

    private static void AddOpenTelemetryLogs(ILoggingBuilder builder)
    {
        try
        {
            if (builder.Services == null)
            {
                AutoInstrumentationEventSource.Log.Verbose("Logs: The builder.Services property is not of the IServiceCollection type, so we're skipping the integration of logs with ServiceCollection.");
                return;
            }

            // Integrate AddOpenTelemetry only once for ServiceCollection.

            _loggingProviderSdkType ??= Type.GetType("OpenTelemetry.Logs.LoggerProviderBuilderSdk, OpenTelemetry");
            var openTelemetryLoggerProviderDescriptor = builder.Services.FirstOrDefault(descriptor => descriptor.ImplementationType == _loggingProviderSdkType);
            if (openTelemetryLoggerProviderDescriptor != null)
            {
                AutoInstrumentationEventSource.Log.Verbose("Logs: AddOpenTelemetry already called on logging builder instance.");
                return;
            }

            var settings = Instrumentation.LogSettings.Value;
            var pluginManager = Instrumentation.PluginManager;

            builder.AddOpenTelemetry(options =>
            {
                options.SetResourceBuilder(ResourceConfigurator.CreateResourceBuilder(Instrumentation.GeneralSettings.Value.EnabledResourceDetectors));

                options.IncludeFormattedMessage = settings.IncludeFormattedMessage;

                pluginManager?.ConfigureLogsOptions(options);

                foreach (var logExporter in settings.LogExporters)
                {
                    switch (logExporter)
                    {
                        case LogExporter.Otlp:
                            options.AddOtlpExporter(otlpOptions =>
                            {
                                // Copy Auto settings to SDK settings
                                settings.OtlpSettings?.CopyTo(otlpOptions);

                                pluginManager?.ConfigureLogsOptions(otlpOptions);
                            });
                            break;
                        case LogExporter.Console:
                            if (pluginManager != null)
                            {
                                options.AddConsoleExporter(pluginManager.ConfigureLogsOptions);
                            }

                            break;
                        default:
                            throw new ArgumentOutOfRangeException($"Logs exporter '{logExporter}' is incorrect");
                    }
                }
            });
            _initializedAtLeastOnce = true;

            AutoInstrumentationEventSource.Log.Information($"Logs: Loaded AddOpenTelemetry from LoggingBuilder.");
        }
        catch (Exception ex)
        {
            AutoInstrumentationEventSource.Log.Error($"Error in AddOpenTelemetryLogs: {ex}");
            throw;
        }
    }
}
#endif
