// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.CompilerServices;
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
    }
}
