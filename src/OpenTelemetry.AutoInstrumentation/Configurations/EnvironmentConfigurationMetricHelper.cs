// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.CompilerServices;
using OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;
using OpenTelemetry.AutoInstrumentation.Configurations.Otlp;
using OpenTelemetry.AutoInstrumentation.Loading;
using OpenTelemetry.AutoInstrumentation.Logging;
using OpenTelemetry.AutoInstrumentation.Plugins;
using OpenTelemetry.Metrics;

namespace OpenTelemetry.AutoInstrumentation.Configurations;

internal static class EnvironmentConfigurationMetricHelper
{
    private static readonly IOtelLogger Logger = OtelLogging.GetLogger();

    public static MeterProviderBuilder UseEnvironmentVariables(
        this MeterProviderBuilder builder,
        LazyInstrumentationLoader lazyInstrumentationLoader,
        MetricSettings settings,
        PluginManager pluginManager)
    {
        foreach (var enabledMeter in settings.EnabledInstrumentations)
        {
            _ = enabledMeter switch
            {
#if NETFRAMEWORK
                MetricInstrumentation.AspNet => Wrappers.AddAspNetInstrumentation(builder, lazyInstrumentationLoader, pluginManager),
#endif
                MetricInstrumentation.HttpClient => Wrappers.AddHttpClientInstrumentation(builder, lazyInstrumentationLoader),
                MetricInstrumentation.NetRuntime => Wrappers.AddRuntimeInstrumentation(builder, pluginManager),
                MetricInstrumentation.Process => Wrappers.AddProcessInstrumentation(builder),
#if NET
                MetricInstrumentation.Npgsql => builder.AddMeter("Npgsql"),
#endif
                MetricInstrumentation.NServiceBus => builder
#if NET
                    .AddMeter("NServiceBus.Core.Pipeline.Incoming") // NServiceBus 9.1.0+
#endif
                    .AddMeter("NServiceBus.Core"), // NServiceBus [8,0.0, 9.1.0)

#if NET
                MetricInstrumentation.AspNetCore => builder
                    .AddMeter(
                        "Microsoft.AspNetCore.Hosting",
                        "Microsoft.AspNetCore.Server.Kestrel",
                        "Microsoft.AspNetCore.Http.Connections",
                        "Microsoft.AspNetCore.Routing",
                        "Microsoft.AspNetCore.Diagnostics",
                        "Microsoft.AspNetCore.RateLimiting",
                        // Following metrics added in .NET 10
                        "Microsoft.AspNetCore.Components",
                        "Microsoft.AspNetCore.Components.Server.Circuits",
                        "Microsoft.AspNetCore.Components.Lifecycle",
                        "Microsoft.AspNetCore.Authorization",
                        "Microsoft.AspNetCore.Authentication",
                        "Microsoft.AspNetCore.Identity",
                        "Microsoft.AspNetCore.MemoryPool"),
#endif
                MetricInstrumentation.SqlClient => Wrappers.AddSqlClientInstrumentation(builder, lazyInstrumentationLoader, pluginManager),
                _ => null,
            };
        }

        // Exporters can cause dependency loads.
        // Should be called later if dependency listeners are already setup.
        builder
            .SetExporter(settings, pluginManager)
            .AddMeter([.. settings.Meters]);

        return builder;
    }

    private static MeterProviderBuilder SetExporter(this MeterProviderBuilder builder, MetricSettings settings, PluginManager pluginManager)
    {
        // If no exporters are specified, it means to use processors (file-based configuration).
        if (settings.MetricExporters.Count == 0)
        {
            if (settings.Readers != null)
            {
                foreach (var reader in settings.Readers)
                {
                    if (reader.Periodic != null && reader.Pull != null)
                    {
                        Logger.Debug("Both periodic and pull metric readers are configured. It is not supported. Skipping.");
                        continue;
                    }

                    if (reader.Periodic == null && reader.Pull == null)
                    {
                        Logger.Debug("No valid metric reader configured, skipping.");
                        continue;
                    }

                    if (reader.Periodic != null)
                    {
                        var exporter = reader.Periodic.Exporter;
                        if (exporter == null)
                        {
                            Logger.Debug("No exporter section for periodic metric reader. Skipping.");
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
                                Logger.Debug("No valid exporter configured for periodic metric reader. Skipping.");
                                continue;
                            case > 1:
                                Logger.Debug("Multiple exporters are configured for periodic metric reader. Only one exporter is supported. Skipping.");
                                continue;
                        }

                        if (exporter.OtlpHttp != null)
                        {
                            builder = Wrappers.AddOtlpHttpExporter(builder, pluginManager, reader.Periodic, exporter.OtlpHttp);
                        }
                        else if (exporter.OtlpGrpc != null)
                        {
                            builder = Wrappers.AddOtlpGrpcExporter(builder, pluginManager, reader.Periodic, exporter.OtlpGrpc);
                        }
                        else if (exporter.Console != null)
                        {
                            builder = Wrappers.AddConsoleExporter(builder, pluginManager, exporter.Console);
                        }
                    }

                    if (reader.Pull != null)
                    {
                        var exporter = reader.Pull.Exporter;
                        if (exporter == null)
                        {
                            Logger.Debug("No exporter section for periodic metric reader. Skipping.");
                            continue;
                        }

                        if (exporter.Prometheus != null)
                        {
                            builder = Wrappers.AddPrometheusHttpListener(builder, pluginManager);
                        }
                    }
                }
            }
        }
        else
        {
            foreach (var metricExporter in settings.MetricExporters)
            {
                builder = metricExporter switch
                {
                    MetricsExporter.Prometheus => Wrappers.AddPrometheusHttpListener(builder, pluginManager),
                    MetricsExporter.Otlp => Wrappers.AddOtlpExporter(builder, settings, pluginManager),
                    MetricsExporter.Console => Wrappers.AddConsoleExporter(builder, pluginManager),
                    _ => throw new ArgumentOutOfRangeException($"Metrics exporter '{metricExporter}' is incorrect")
                };
            }
        }

        return builder;
    }

    /// <summary>
    /// This class wraps external extension methods to ensure the dlls are not loaded, if not necessary.
    /// .NET Framework is aggressively inlining these wrappers. Inlining must be disabled to ensure the wrapping effect.
    /// </summary>
    private static class Wrappers
    {
        // Meters
#if NETFRAMEWORK
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static MeterProviderBuilder AddAspNetInstrumentation(MeterProviderBuilder builder, LazyInstrumentationLoader lazyInstrumentationLoader, PluginManager pluginManager)
        {
            DelayedInitialization.Metrics.AddAspNet(lazyInstrumentationLoader, pluginManager);
            return builder.AddMeter("OpenTelemetry.Instrumentation.AspNet");
        }
#endif

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static MeterProviderBuilder AddHttpClientInstrumentation(MeterProviderBuilder builder, LazyInstrumentationLoader lazyInstrumentationLoader)
        {
#if NET
            // HTTP has build in support for metrics in .NET8. Executing OpenTelemetry.Instrumentation.Http in this case leads to duplicated metrics.
            return builder
                .AddMeter("System.Net.Http")
                .AddMeter("System.Net.NameResolution");
#else
            DelayedInitialization.Metrics.AddHttpClient(lazyInstrumentationLoader);
            return builder.AddMeter("OpenTelemetry.Instrumentation.Http");
#endif
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static MeterProviderBuilder AddRuntimeInstrumentation(MeterProviderBuilder builder, PluginManager pluginManager)
        {
            return builder.AddRuntimeInstrumentation(pluginManager.ConfigureMetricsOptions);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static MeterProviderBuilder AddProcessInstrumentation(MeterProviderBuilder builder)
        {
            return builder.AddProcessInstrumentation();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static MeterProviderBuilder AddSqlClientInstrumentation(MeterProviderBuilder builder, LazyInstrumentationLoader lazyInstrumentationLoader, PluginManager pluginManager)
        {
            DelayedInitialization.Metrics.AddSqlClient(lazyInstrumentationLoader, pluginManager);
            return builder.AddMeter("OpenTelemetry.Instrumentation.SqlClient");
        }

        // Exporters

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static MeterProviderBuilder AddConsoleExporter(MeterProviderBuilder builder, PluginManager pluginManager, PeriodicMetricReaderConfig readerConfig)
        {
            return builder.AddConsoleExporter((consoleExporterOptions, metricReaderOptions) =>
            {
                readerConfig.CopyTo(metricReaderOptions.PeriodicExportingMetricReaderOptions);
                pluginManager.ConfigureMetricsOptions(consoleExporterOptions);
                pluginManager.ConfigureMetricsOptions(metricReaderOptions);
            });
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static MeterProviderBuilder AddConsoleExporter(MeterProviderBuilder builder, PluginManager pluginManager)
        {
            return builder.AddConsoleExporter((consoleExporterOptions, metricReaderOptions) =>
            {
                pluginManager.ConfigureMetricsOptions(consoleExporterOptions);
                pluginManager.ConfigureMetricsOptions(metricReaderOptions);
            });
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static MeterProviderBuilder AddConsoleExporter(MeterProviderBuilder builder, PluginManager pluginManager, ConsoleExporterConfig consoleExporterConfig)
        {
            return builder.AddConsoleExporter((consoleExporterOptions, metricReaderOptions) =>
            {
                metricReaderOptions.TemporalityPreference = consoleExporterConfig.GetTemporalityPreference();
                pluginManager.ConfigureMetricsOptions(consoleExporterOptions);
                pluginManager.ConfigureMetricsOptions(metricReaderOptions);
            });
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static MeterProviderBuilder AddPrometheusHttpListener(MeterProviderBuilder builder, PluginManager pluginManager)
        {
            Logger.Warning("Prometheus exporter is configured. It is intended for the inner dev loop. Do NOT use in production");

            return builder.AddPrometheusHttpListener(pluginManager.ConfigureMetricsOptions);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static MeterProviderBuilder AddOtlpExporter(MeterProviderBuilder builder, MetricSettings settings, PluginManager pluginManager)
        {
            return builder.AddOtlpExporter((options, metricReaderOptions) =>
            {
                // Copy Auto settings to SDK settings
                settings.OtlpSettings?.CopyTo(options);

                pluginManager.ConfigureMetricsOptions(options);
                pluginManager.ConfigureMetricsOptions(metricReaderOptions);
            });
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static MeterProviderBuilder AddOtlpHttpExporter(MeterProviderBuilder builder, PluginManager pluginManager, PeriodicMetricReaderConfig readerConfig, OtlpHttpExporterConfig otlpHttp)
        {
            var otlpSettings = new OtlpSettings(OtlpSignalType.Metrics, otlpHttp);
            return builder.AddOtlpExporter((options, metricReaderOptions) =>
            {
                otlpSettings.CopyTo(options);
                readerConfig.CopyTo(metricReaderOptions.PeriodicExportingMetricReaderOptions);
                metricReaderOptions.TemporalityPreference = otlpHttp.GetTemporalityPreference();

                pluginManager.ConfigureMetricsOptions(options);
                pluginManager.ConfigureMetricsOptions(metricReaderOptions);
            });
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static MeterProviderBuilder AddOtlpGrpcExporter(MeterProviderBuilder builder, PluginManager pluginManager, PeriodicMetricReaderConfig readerConfig, OtlpGrpcExporterConfig otlpGrpc)
        {
            var otlpSettings = new OtlpSettings(otlpGrpc);
            return builder.AddOtlpExporter((options, metricReaderOptions) =>
            {
                otlpSettings.CopyTo(options);
                readerConfig.CopyTo(metricReaderOptions.PeriodicExportingMetricReaderOptions);
                metricReaderOptions.TemporalityPreference = otlpGrpc.GetTemporalityPreference();

                pluginManager.ConfigureMetricsOptions(options);
                pluginManager.ConfigureMetricsOptions(metricReaderOptions);
            });
        }
    }
}
