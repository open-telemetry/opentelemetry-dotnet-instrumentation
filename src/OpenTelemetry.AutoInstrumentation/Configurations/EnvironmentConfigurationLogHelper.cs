// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.CompilerServices;
using OpenTelemetry;
using OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;
using OpenTelemetry.AutoInstrumentation.Configurations.Otlp;
using OpenTelemetry.AutoInstrumentation.Loading;
using OpenTelemetry.AutoInstrumentation.Logging;
using OpenTelemetry.AutoInstrumentation.Plugins;
using OpenTelemetry.Logs;

namespace OpenTelemetry.AutoInstrumentation.Configurations;

internal static class EnvironmentConfigurationLogHelper
{
    private static readonly IOtelLogger Logger = OtelLogging.GetLogger();

    public static LoggerProviderBuilder UseEnvironmentVariables(
        this LoggerProviderBuilder builder,
        LazyInstrumentationLoader lazyInstrumentationLoader,
        LogSettings settings,
        PluginManager pluginManager)
    {
        SetExporter(builder, settings, pluginManager);
        return builder;
    }

    private static void SetExporter(LoggerProviderBuilder builder, LogSettings settings, PluginManager pluginManager)
    {
        if (settings.LogExporters.Count == 0)
        {
            if (settings.Processors != null)
            {
                foreach (var processor in settings.Processors)
                {
                    if (processor.Batch != null && processor.Simple != null)
                    {
                        Logger.Debug("Both batch and simple processors are configured. It is not supported. Skipping.");
                        continue;
                    }

                    if (processor.Batch == null && processor.Simple == null)
                    {
                        Logger.Debug("No valid processor configured, skipping.");
                        continue;
                    }

                    if (processor.Batch != null)
                    {
                        var exporter = processor.Batch.Exporter;
                        if (exporter == null)
                        {
                            Logger.Debug("No exporter configured for batch processor. Skipping.");
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

                        switch (exportersCount)
                        {
                            case 0:
                                Logger.Debug("No valid exporter configured for batch processor. Skipping.");
                                continue;
                            case > 1:
                                Logger.Debug("Multiple exporters are configured for batch processor. Only one exporter is supported. Skipping.");
                                continue;
                        }

                        if (exporter.OtlpHttp != null)
                        {
                            builder = Wrappers.AddOtlpHttpExporter(builder, pluginManager, processor.Batch, exporter.OtlpHttp);
                        }
                        else if (exporter.OtlpGrpc != null)
                        {
                            builder = Wrappers.AddOtlpGrpcExporter(builder, pluginManager, processor.Batch, exporter.OtlpGrpc);
                        }
                    }
                    else if (processor.Simple != null)
                    {
                        var exporter = processor.Simple.Exporter;
                        if (exporter == null)
                        {
                            Logger.Debug("No exporter configured for simple processor. Skipping.");
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
                                Logger.Debug("No valid exporter configured for simple processor. Skipping.");
                                continue;
                            case > 1:
                                Logger.Debug("Multiple exporters are configured for simple processor. Only one exporter is supported. Skipping.");
                                continue;
                        }

                        if (exporter.OtlpHttp != null)
                        {
                            builder = Wrappers.AddOtlpHttpExporter(builder, pluginManager, exporter.OtlpHttp);
                        }
                        else if (exporter.OtlpGrpc != null)
                        {
                            builder = Wrappers.AddOtlpGrpcExporter(builder, pluginManager, exporter.OtlpGrpc);
                        }
                        else if (exporter.Console != null)
                        {
                            builder = Wrappers.AddConsoleExporter(builder, pluginManager);
                        }
                    }
                }
            }

            return;
        }

        foreach (var logExporter in settings.LogExporters)
        {
            builder = logExporter switch
            {
                LogExporter.Otlp => Wrappers.AddOtlpExporter(builder, settings, pluginManager),
                LogExporter.Console => Wrappers.AddConsoleExporter(builder, pluginManager),
                _ => throw new ArgumentOutOfRangeException($"Log exporter '{logExporter}' is incorrect")
            };
        }
    }

    private static class Wrappers
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static LoggerProviderBuilder AddConsoleExporter(LoggerProviderBuilder builder, PluginManager pluginManager)
        {
            return builder.AddConsoleExporter(pluginManager.ConfigureLogsOptions);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static LoggerProviderBuilder AddOtlpExporter(LoggerProviderBuilder builder, LogSettings settings, PluginManager pluginManager)
        {
            return builder.AddOtlpExporter(options =>
            {
                settings.OtlpSettings?.CopyTo(options);
                pluginManager?.ConfigureLogsOptions(options);
            });
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static LoggerProviderBuilder AddOtlpHttpExporter(LoggerProviderBuilder builder, PluginManager pluginManager, LogBatchProcessorConfig batch, OtlpHttpExporterConfig otlpHttp)
        {
            var otlpSettings = new OtlpSettings(OtlpSignalType.Logs, otlpHttp);
            return builder.AddOtlpExporter(options =>
            {
                batch?.CopyTo(options.BatchExportProcessorOptions);
                otlpSettings.CopyTo(options);
                pluginManager?.ConfigureLogsOptions(options);
            });
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static LoggerProviderBuilder AddOtlpGrpcExporter(LoggerProviderBuilder builder, PluginManager pluginManager, LogBatchProcessorConfig batch, OtlpGrpcExporterConfig otlpGrpc)
        {
            var otlpSettings = new OtlpSettings(otlpGrpc);
            return builder.AddOtlpExporter(options =>
            {
                batch?.CopyTo(options.BatchExportProcessorOptions);
                otlpSettings.CopyTo(options);
                pluginManager?.ConfigureLogsOptions(options);
            });
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static LoggerProviderBuilder AddOtlpHttpExporter(LoggerProviderBuilder builder, PluginManager pluginManager, OtlpHttpExporterConfig otlpHttp)
        {
            var otlpSettings = new OtlpSettings(OtlpSignalType.Logs, otlpHttp);
            return builder.AddOtlpExporter(options =>
            {
                options.ExportProcessorType = ExportProcessorType.Simple;
                otlpSettings.CopyTo(options);
                pluginManager?.ConfigureLogsOptions(options);
            });
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static LoggerProviderBuilder AddOtlpGrpcExporter(LoggerProviderBuilder builder, PluginManager pluginManager, OtlpGrpcExporterConfig otlpGrpc)
        {
            var otlpSettings = new OtlpSettings(otlpGrpc);
            return builder.AddOtlpExporter(options =>
            {
                options.ExportProcessorType = ExportProcessorType.Simple;
                otlpSettings.CopyTo(options);
                pluginManager?.ConfigureLogsOptions(options);
            });
        }
    }
}
