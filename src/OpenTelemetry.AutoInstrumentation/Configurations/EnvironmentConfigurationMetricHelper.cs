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
                    .AddMeter("Microsoft.AspNetCore.Hosting")
                    .AddMeter("Microsoft.AspNetCore.Server.Kestrel")
                    .AddMeter("Microsoft.AspNetCore.Http.Connections")
                    .AddMeter("Microsoft.AspNetCore.Routing")
                    .AddMeter("Microsoft.AspNetCore.Diagnostics")
                    .AddMeter("Microsoft.AspNetCore.RateLimiting"),
#endif
                MetricInstrumentation.SqlClient => Wrappers.AddSqlClientInstrumentation(builder, lazyInstrumentationLoader, pluginManager),
                _ => null,
            };
        }

        // Exporters can cause dependency loads.
        // Should be called later if dependency listeners are already setup.
        builder
            .SetExporter(settings, pluginManager)
            .AddMeter(settings.Meters.ToArray());

        return builder;
    }

    private static MeterProviderBuilder SetExporter(this MeterProviderBuilder builder, MetricSettings settings, PluginManager pluginManager)
    {
        if (settings.MetricExporters.Count == 0)
        {
            if (settings.Readers != null)
            {
                foreach (var reader in settings.Readers)
                {
                    if (reader.Periodic != null)
                    {
                        builder = Wrappers.AddPeriodicReader(builder, pluginManager, reader.Periodic);
                        continue;
                    }

                    if (reader.Pull != null)
                    {
                        builder = Wrappers.AddPullReader(builder, pluginManager, reader.Pull);
                        continue;
                    }

                    Logger.Debug("No valid metric reader configured, skipping.");
                }
            }

            return builder;
        }

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

        return builder;
    }

    /// <summary>
    /// This class wraps external extension methods to ensure the dlls are not loaded, if not necessary.
    /// .NET Framework is aggressively inlining these wrappers. Inlining must be disabled to ensure the wrapping effect.
    /// </summary>
    private static class Wrappers
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static MeterProviderBuilder AddPeriodicReader(
            MeterProviderBuilder builder,
            PluginManager pluginManager,
            PeriodicMetricReaderConfig readerConfig)
        {
            var exporter = readerConfig.Exporter;
            if (exporter == null)
            {
                Logger.Debug("No exporter configured for periodic metric reader. Skipping.");
                return builder;
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

            if (exporter.Prometheus != null)
            {
                exportersCount++;
            }

            switch (exportersCount)
            {
                case 0:
                    Logger.Debug("No valid exporter configured for periodic metric reader. Skipping.");
                    return builder;
                case > 1:
                    Logger.Debug("Multiple exporters are configured for periodic metric reader. Only one exporter is supported. Skipping.");
                    return builder;
            }

            if (exporter.OtlpHttp != null)
            {
                return AddOtlpHttpExporter(builder, pluginManager, readerConfig, exporter.OtlpHttp);
            }

            if (exporter.OtlpGrpc != null)
            {
                return AddOtlpGrpcExporter(builder, pluginManager, readerConfig, exporter.OtlpGrpc);
            }

            if (exporter.Console != null)
            {
                return AddConsoleExporter(builder, pluginManager, readerConfig, exporter.Console);
            }

            if (exporter.Prometheus != null)
            {
                return AddPrometheusHttpListener(builder, pluginManager, exporter.Prometheus);
            }

            return builder;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static MeterProviderBuilder AddPullReader(
            MeterProviderBuilder builder,
            PluginManager pluginManager,
            PullMetricReaderConfig readerConfig)
        {
            var exporter = readerConfig.Exporter;
            if (exporter == null)
            {
                Logger.Debug("No exporter configured for pull metric reader. Skipping.");
                return builder;
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

            if (exporter.Prometheus != null)
            {
                exportersCount++;
            }

            switch (exportersCount)
            {
                case 0:
                    Logger.Debug("No valid exporter configured for pull metric reader. Skipping.");
                    return builder;
                case > 1:
                    Logger.Debug("Multiple exporters are configured for pull metric reader. Only one exporter is supported. Skipping.");
                    return builder;
            }

            if (exporter.Prometheus != null)
            {
                return AddPrometheusHttpListener(builder, pluginManager, exporter.Prometheus);
            }

            Logger.Debug("Only the Prometheus exporter is supported for pull metric reader. Skipping.");
            return builder;
        }

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
        public static MeterProviderBuilder AddConsoleExporter(
            MeterProviderBuilder builder,
            PluginManager pluginManager,
            PeriodicMetricReaderConfig readerConfig,
            ConsoleMetricExporterConfig consoleExporter)
        {
            return builder.AddConsoleExporter((consoleExporterOptions, metricReaderOptions) =>
            {
                pluginManager.ConfigureMetricsOptions(consoleExporterOptions);
                ConfigureMetricReaderOptions(metricReaderOptions, readerConfig, consoleExporter.TemporalityPreference);
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
        public static MeterProviderBuilder AddPrometheusHttpListener(MeterProviderBuilder builder, PluginManager pluginManager)
        {
            Logger.Warning("Prometheus exporter is configured. It is intended for the inner dev loop. Do NOT use in production");

            return builder.AddPrometheusHttpListener(pluginManager.ConfigureMetricsOptions);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static MeterProviderBuilder AddPrometheusHttpListener(
            MeterProviderBuilder builder,
            PluginManager pluginManager,
            PrometheusExporterConfig prometheusExporter)
        {
            Logger.Warning("Prometheus exporter is configured. It is intended for the inner dev loop. Do NOT use in production");

            return builder.AddPrometheusHttpListener(options =>
            {
                if (!string.IsNullOrWhiteSpace(prometheusExporter.Host))
                {
                    options.Host = prometheusExporter.Host;
                }

                if (prometheusExporter.Port.HasValue)
                {
                    options.Port = prometheusExporter.Port.Value;
                }

                pluginManager.ConfigureMetricsOptions(options);
            });
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
        public static MeterProviderBuilder AddOtlpHttpExporter(
            MeterProviderBuilder builder,
            PluginManager pluginManager,
            PeriodicMetricReaderConfig readerConfig,
            OtlpHttpExporterConfig otlpHttp)
        {
            return builder.AddOtlpExporter((options, metricReaderOptions) =>
            {
                var otlpSettings = new OtlpSettings(OtlpSignalType.Metrics, otlpHttp);
                otlpSettings.CopyTo(options);

                pluginManager.ConfigureMetricsOptions(options);
                ConfigureMetricReaderOptions(metricReaderOptions, readerConfig, otlpHttp.TemporalityPreference);
                pluginManager.ConfigureMetricsOptions(metricReaderOptions);
            });
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static MeterProviderBuilder AddOtlpGrpcExporter(
            MeterProviderBuilder builder,
            PluginManager pluginManager,
            PeriodicMetricReaderConfig readerConfig,
            OtlpGrpcExporterConfig otlpGrpc)
        {
            return builder.AddOtlpExporter((options, metricReaderOptions) =>
            {
                var otlpSettings = new OtlpSettings(otlpGrpc);
                otlpSettings.CopyTo(options);

                pluginManager.ConfigureMetricsOptions(options);
                ConfigureMetricReaderOptions(metricReaderOptions, readerConfig, otlpGrpc.TemporalityPreference);
                pluginManager.ConfigureMetricsOptions(metricReaderOptions);
            });
        }
    }

    private static void ConfigureMetricReaderOptions(
        MetricReaderOptions metricReaderOptions,
        PeriodicMetricReaderConfig readerConfig,
        string? temporalityPreference)
    {
        metricReaderOptions.MetricReaderType = MetricReaderType.PeriodicExportingMetricReader;
        metricReaderOptions.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = readerConfig.Interval;
        metricReaderOptions.PeriodicExportingMetricReaderOptions.ExportTimeoutMilliseconds = readerConfig.Timeout;

        if (TryParseTemporalityPreference(temporalityPreference, out var temporality))
        {
            metricReaderOptions.TemporalityPreference = temporality;
        }
    }

    private static bool TryParseTemporalityPreference(string? value, out MetricReaderTemporalityPreference temporalityPreference)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            switch (value.Trim().ToLowerInvariant())
            {
                case "cumulative":
                    temporalityPreference = MetricReaderTemporalityPreference.Cumulative;
                    return true;
                case "delta":
                    temporalityPreference = MetricReaderTemporalityPreference.Delta;
                    return true;
                case "low_memory":
                    temporalityPreference = MetricReaderTemporalityPreference.LowMemory;
                    return true;
                default:
                    Logger.Debug($"Unknown temporality preference '{value}'. Skipping.");
                    break;
            }
        }

        temporalityPreference = default;
        return false;
    }
}
