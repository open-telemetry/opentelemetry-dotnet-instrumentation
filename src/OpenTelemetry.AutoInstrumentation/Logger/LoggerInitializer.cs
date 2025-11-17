// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.AutoInstrumentation.Configurations;
using OpenTelemetry.AutoInstrumentation.Configurations.Otlp;
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

            _loggingProviderSdkType ??= Type.GetType("OpenTelemetry.Logs.LoggerProviderBuilderSdk, OpenTelemetry");
            var openTelemetryLoggerProviderDescriptor = builder.Services.FirstOrDefault(descriptor => descriptor.ImplementationType == _loggingProviderSdkType);
            if (openTelemetryLoggerProviderDescriptor != null)
            {
                AutoInstrumentationEventSource.Log.Verbose("Logs: AddOpenTelemetry already called on logging builder instance.");
                return;
            }

            var settings = Instrumentation.LogSettings.Value;
            var pluginManager = Instrumentation.PluginManager;

            builder.AddOpenTelemetry(loggerBuilder =>
            {
                loggerBuilder.SetResourceBuilder(ResourceConfigurator.CreateResourceBuilder(Instrumentation.ResourceSettings.Value));

                loggerBuilder.IncludeFormattedMessage = settings.IncludeFormattedMessage;

                pluginManager?.ConfigureLogsOptions(loggerBuilder);

                if (settings.LogExporters.Count == 0)
                {
                    AutoInstrumentationEventSource.Log.Information("Logs: Using file-based configuration for exporters.");

                    if (settings.Processors == null)
                    {
                        AutoInstrumentationEventSource.Log.Verbose("Logs: No processors found in file-based configuration.");
                        return;
                    }

                    foreach (var processor in settings.Processors)
                    {
                        if (processor.Batch != null && processor.Simple != null)
                        {
                            AutoInstrumentationEventSource.Log.Verbose("Logs: Both batch and simple processors are configured. Skipping unsupported combination.");
                            continue;
                        }

                        if (processor.Batch == null && processor.Simple == null)
                        {
                            AutoInstrumentationEventSource.Log.Verbose("Logs: No valid processor configuration found, skipping.");
                            continue;
                        }

                        if (processor.Batch != null)
                        {
                            var exporter = processor.Batch.Exporter;
                            if (exporter == null)
                            {
                                AutoInstrumentationEventSource.Log.Verbose("Logs: No exporter defined for batch processor, skipping.");
                                continue;
                            }

                            if (exporter.OtlpHttp == null && exporter.OtlpGrpc == null)
                            {
                                AutoInstrumentationEventSource.Log.Verbose("Logs: No valid exporter found for batch processor, skipping.");
                                continue;
                            }

                            if (exporter.OtlpHttp != null && exporter.OtlpGrpc != null)
                            {
                                AutoInstrumentationEventSource.Log.Verbose("Logs: Multiple exporters configured for batch processor, only one supported. Skipping.");
                                continue;
                            }

                            if (exporter.OtlpHttp != null)
                            {
                                AutoInstrumentationEventSource.Log.Verbose("Logs: Adding OTLP HTTP batch exporter.");
                                var otlpSettings = new OtlpSettings(OtlpSignalType.Logs, exporter.OtlpHttp);
                                loggerBuilder.AddOtlpExporter(opt =>
                                {
                                    processor.Batch.CopyTo(opt.BatchExportProcessorOptions);
                                    otlpSettings.CopyTo(opt);
                                    pluginManager?.ConfigureLogsOptions(opt);
                                });
                            }
                            else if (exporter.OtlpGrpc != null)
                            {
                                AutoInstrumentationEventSource.Log.Verbose("Logs: Adding OTLP gRPC batch exporter.");
                                var otlpSettings = new OtlpSettings(exporter.OtlpGrpc);
                                loggerBuilder.AddOtlpExporter(opt =>
                                {
                                    processor.Batch.CopyTo(opt.BatchExportProcessorOptions);
                                    otlpSettings.CopyTo(opt);
                                    pluginManager?.ConfigureLogsOptions(opt);
                                });
                            }
                        }
                        else if (processor.Simple != null)
                        {
                            var exporter = processor.Simple.Exporter;
                            if (exporter == null)
                            {
                                AutoInstrumentationEventSource.Log.Verbose("Logs: No exporter defined for simple processor, skipping.");
                                continue;
                            }

                            var exportersCount = 0;

                            if (exporter.OtlpHttp != null)
                            {
                                exportersCount++;
                            }

                            if (exporter.OtlpGrpc != null)
                            {
                                exportersCount++;
                            }

                            if (exporter.Console != null)
                            {
                                exportersCount++;
                            }

                            switch (exportersCount)
                            {
                                case 0:
                                    AutoInstrumentationEventSource.Log.Verbose("Logs: No valid exporter found for simple processor, skipping.");
                                    continue;
                                case > 1:
                                    AutoInstrumentationEventSource.Log.Verbose("Logs: Multiple exporters configured for simple processor, only one supported. Skipping.");
                                    continue;
                            }

                            if (exporter.OtlpHttp != null)
                            {
                                AutoInstrumentationEventSource.Log.Verbose("Logs: Adding OTLP HTTP simple exporter.");
                                var otlpSettings = new OtlpSettings(OtlpSignalType.Logs, exporter.OtlpHttp);
                                loggerBuilder.AddOtlpExporter(opt =>
                                {
                                    opt.ExportProcessorType = ExportProcessorType.Simple;
                                    otlpSettings.CopyTo(opt);
                                    pluginManager?.ConfigureLogsOptions(opt);
                                });
                            }
                            else if (exporter.OtlpGrpc != null)
                            {
                                AutoInstrumentationEventSource.Log.Verbose("Logs: Adding OTLP gRPC simple exporter.");
                                var otlpSettings = new OtlpSettings(exporter.OtlpGrpc);
                                loggerBuilder.AddOtlpExporter(opt =>
                                {
                                    opt.ExportProcessorType = ExportProcessorType.Simple;
                                    otlpSettings.CopyTo(opt);
                                    pluginManager?.ConfigureLogsOptions(opt);
                                });
                            }
                            else if (exporter.Console != null)
                            {
                                AutoInstrumentationEventSource.Log.Verbose("Logs: Adding Console simple exporter.");
                                if (pluginManager != null)
                                {
                                    loggerBuilder.AddConsoleExporter(pluginManager.ConfigureLogsOptions);
                                }
                            }
                        }
                    }

                    return;
                }

                foreach (var logExporter in settings.LogExporters)
                {
                    switch (logExporter)
                    {
                        case LogExporter.Otlp:
                            loggerBuilder.AddOtlpExporter(opt =>
                            {
                                settings.OtlpSettings?.CopyTo(opt);
                                pluginManager?.ConfigureLogsOptions(opt);
                            });
                            break;
                        case LogExporter.Console:
                            if (pluginManager != null)
                            {
                                loggerBuilder.AddConsoleExporter(pluginManager.ConfigureLogsOptions);
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
