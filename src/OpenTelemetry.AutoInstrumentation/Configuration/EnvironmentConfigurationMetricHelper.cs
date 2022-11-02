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

using System;
using System.Linq;
using System.Runtime.CompilerServices;
using OpenTelemetry.AutoInstrumentation.Logging;
using OpenTelemetry.Metrics;

namespace OpenTelemetry.AutoInstrumentation.Configuration;

internal static class EnvironmentConfigurationMetricHelper
{
    private static readonly ILogger Logger = OtelLogging.GetLogger();

    public static MeterProviderBuilder UseEnvironmentVariables(this MeterProviderBuilder builder, MetricSettings settings)
    {
        builder
            .SetExporter(settings)
            .AddMeter(settings.Meters.ToArray());

        foreach (var enabledMeter in settings.EnabledInstrumentations)
        {
            _ = enabledMeter switch
            {
                MetricInstrumentation.AspNet => Wrappers.AddSdkAspNetInstrumentation(builder),
                MetricInstrumentation.HttpClient => Wrappers.AddHttpClientInstrumentation(builder),
                MetricInstrumentation.NetRuntime => Wrappers.AddRuntimeInstrumentation(builder),
                MetricInstrumentation.Process => Wrappers.AddProcessInstrumentation(builder),
                _ => null,
            };
        }

        return builder;
    }

    private static MeterProviderBuilder SetExporter(this MeterProviderBuilder builder, MetricSettings settings)
    {
        if (settings.ConsoleExporterEnabled)
        {
            Wrappers.AddConsoleExporter(builder, settings);
        }

        return settings.MetricExporter switch
        {
            MetricsExporter.Prometheus => Wrappers.AddPrometheusExporter(builder),
            MetricsExporter.Otlp => Wrappers.AddOtlpExporter(builder, settings),
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
        public static MeterProviderBuilder AddSdkAspNetInstrumentation(MeterProviderBuilder builder)
        {
#if NET462
            builder.AddAspNetInstrumentation();
#elif NETCOREAPP3_1_OR_GREATER
            builder.AddMeter("OpenTelemetry.Instrumentation.AspNetCore");
#endif

            return builder;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static MeterProviderBuilder AddHttpClientInstrumentation(MeterProviderBuilder builder)
        {
            return builder.AddHttpClientInstrumentation();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static MeterProviderBuilder AddRuntimeInstrumentation(MeterProviderBuilder builder)
        {
            return builder.AddRuntimeInstrumentation();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static MeterProviderBuilder AddProcessInstrumentation(MeterProviderBuilder builder)
        {
            return builder.AddProcessInstrumentation();
        }

        // Exporters

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static MeterProviderBuilder AddConsoleExporter(MeterProviderBuilder builder, MetricSettings settings)
        {
            return builder.AddConsoleExporter((_, metricReaderOptions) =>
            {
                if (settings.MetricExportInterval != null)
                {
                    metricReaderOptions.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = settings.MetricExportInterval;
                }
            });
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static MeterProviderBuilder AddPrometheusExporter(MeterProviderBuilder builder)
        {
            Logger.Warning("Prometheus exporter is configured. It is intended for the inner dev loop. Do NOT use in production");

            return builder.AddPrometheusExporter(options =>
            {
                options.StartHttpListener = true;
                options.ScrapeResponseCacheDurationMilliseconds = 300;
            });
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static MeterProviderBuilder AddOtlpExporter(MeterProviderBuilder builder, MetricSettings settings)
        {
#if NETCOREAPP3_1
            if (settings.Http2UnencryptedSupportEnabled)
            {
                // Adding the OtlpExporter creates a GrpcChannel.
                // This switch must be set before creating a GrpcChannel/HttpClient when calling an insecure gRPC service.
                // See: https://docs.microsoft.com/aspnet/core/grpc/troubleshoot#call-insecure-grpc-services-with-net-core-client
                AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            }
#endif

            return builder.AddOtlpExporter((options, metricReaderOptions) =>
            {
                if (settings.OtlpExportProtocol.HasValue)
                {
                    options.Protocol = settings.OtlpExportProtocol.Value;
                }

                if (settings.MetricExportInterval != null)
                {
                    metricReaderOptions.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = settings.MetricExportInterval;
                }
            });
        }
    }
}
