// <copyright file="EnvironmentConfigurationMetricHelper.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

using System.Runtime.CompilerServices;
using OpenTelemetry.AutoInstrumentation.Loading;
using OpenTelemetry.AutoInstrumentation.Loading.Initializers;
using OpenTelemetry.AutoInstrumentation.Logging;
using OpenTelemetry.AutoInstrumentation.Plugins;
using OpenTelemetry.Metrics;

namespace OpenTelemetry.AutoInstrumentation.Configuration;

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
                MetricInstrumentation.AspNet => Wrappers.AddAspNetInstrumentation(builder, lazyInstrumentationLoader),
                MetricInstrumentation.HttpClient => Wrappers.AddHttpClientInstrumentation(builder, lazyInstrumentationLoader),
                MetricInstrumentation.NetRuntime => Wrappers.AddRuntimeInstrumentation(builder, pluginManager),
                MetricInstrumentation.Process => Wrappers.AddProcessInstrumentation(builder, pluginManager),
                MetricInstrumentation.NServiceBus => builder.AddMeter("NServiceBus.Core"),
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
        if (settings.ConsoleExporterEnabled)
        {
            Wrappers.AddConsoleExporter(builder, settings, pluginManager);
        }

        return settings.MetricExporter switch
        {
            MetricsExporter.Prometheus => Wrappers.AddPrometheusHttpListener(builder, pluginManager),
            MetricsExporter.Otlp => Wrappers.AddOtlpExporter(builder, settings, pluginManager),
            MetricsExporter.None => builder,
            _ => throw new ArgumentOutOfRangeException($"Metrics exporter '{settings.MetricExporter}' is incorrect")
        };
    }

    /// <summary>
    /// This class wraps external extension methods to ensure the dlls are not loaded, if not necessary.
    /// .NET Framework is aggressively inlining these wrappers. Inlining must be disabled to ensure the wrapping effect.
    /// </summary>
    private static class Wrappers
    {
        // Meters

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static MeterProviderBuilder AddAspNetInstrumentation(MeterProviderBuilder builder, LazyInstrumentationLoader lazyInstrumentationLoader)
        {
#if NET462
            new AspNetMetricsInitializer(lazyInstrumentationLoader);

            builder.AddMeter("OpenTelemetry.Instrumentation.AspNet");
#elif NET6_0_OR_GREATER
            lazyInstrumentationLoader.Add(new AspNetCoreMetricsInitializer());

            builder.AddMeter("OpenTelemetry.Instrumentation.AspNetCore");
#endif

            return builder;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static MeterProviderBuilder AddHttpClientInstrumentation(MeterProviderBuilder builder, LazyInstrumentationLoader lazyInstrumentationLoader)
        {
            new HttpClientMetricsInitializer(lazyInstrumentationLoader);

            return builder.AddMeter("OpenTelemetry.Instrumentation.Http");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static MeterProviderBuilder AddRuntimeInstrumentation(MeterProviderBuilder builder, PluginManager pluginManager)
        {
            return builder.AddRuntimeInstrumentation(pluginManager.ConfigureMetricsOptions);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static MeterProviderBuilder AddProcessInstrumentation(MeterProviderBuilder builder, PluginManager pluginManager)
        {
            return builder.AddProcessInstrumentation(pluginManager.ConfigureMetricsOptions);
        }

        // Exporters

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static MeterProviderBuilder AddConsoleExporter(MeterProviderBuilder builder, MetricSettings settings, PluginManager pluginManager)
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
                if (settings.OtlpExportProtocol.HasValue)
                {
                    options.Protocol = settings.OtlpExportProtocol.Value;
                }

                pluginManager.ConfigureMetricsOptions(options);
                pluginManager.ConfigureMetricsOptions(metricReaderOptions);
            });
        }
    }
}
