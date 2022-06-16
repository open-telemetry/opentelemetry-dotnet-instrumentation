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
using System.Collections.Generic;
using System.Linq;
using OpenTelemetry.Metrics;

namespace OpenTelemetry.AutoInstrumentation.Configuration;

internal static class EnvironmentConfigurationMetricHelper
{
    private static readonly Dictionary<MeterInstrumentation, Action<MeterProviderBuilder>> AddMeters = new()
    {
        [MeterInstrumentation.AspNet] = builder => builder.AddSdkAspNetInstrumentation(),
        [MeterInstrumentation.HttpClient] = builder => builder.AddHttpClientInstrumentation(),
        [MeterInstrumentation.NetRuntime] = builder => builder.AddRuntimeMetrics(),
    };

    public static MeterProviderBuilder UseEnvironmentVariables(this MeterProviderBuilder builder, MeterSettings settings)
    {
        builder
            .SetExporter(settings)
            .AddMeter(settings.Meters.ToArray());

        foreach (var enabledMeter in settings.EnabledInstrumentations)
        {
            if (AddMeters.TryGetValue(enabledMeter, out var addMeter))
            {
                addMeter(builder);
            }
        }

        return builder;
    }

    public static MeterProviderBuilder AddSdkAspNetInstrumentation(this MeterProviderBuilder builder)
    {
#if NET462
        return builder.AddAspNetInstrumentation();
#elif NETCOREAPP3_1_OR_GREATER
        return builder.AddAspNetCoreInstrumentation();
#endif
    }

    private static MeterProviderBuilder SetExporter(this MeterProviderBuilder builder, MeterSettings settings)
    {
        if (settings.ConsoleExporterEnabled)
        {
            builder.AddConsoleExporter();
        }

        switch (settings.MetricExporter)
        {
            case MetricsExporter.Prometheus:
                builder.AddPrometheusExporter(options => { options.StartHttpListener = true; });
                break;
            case MetricsExporter.Otlp:
#if NETCOREAPP3_1
                if (settings.Http2UnencryptedSupportEnabled)
                {
                    // Adding the OtlpExporter creates a GrpcChannel.
                    // This switch must be set before creating a GrpcChannel/HttpClient when calling an insecure gRPC service.
                    // See: https://docs.microsoft.com/aspnet/core/grpc/troubleshoot#call-insecure-grpc-services-with-net-core-client
                    AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
                }
#endif
                builder.AddOtlpExporter(options =>
                {
                    if (settings.OtlpExportProtocol.HasValue)
                    {
                        options.Protocol = settings.OtlpExportProtocol.Value;
                    }
                });
                break;
            case MetricsExporter.None:
                break;
            default:
                throw new ArgumentOutOfRangeException($"Metrics exporter '{settings.MetricExporter}' is incorrect");
        }

        return builder;
    }
}
