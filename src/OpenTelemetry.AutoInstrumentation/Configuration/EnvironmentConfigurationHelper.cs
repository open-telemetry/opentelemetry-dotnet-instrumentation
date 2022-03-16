// <copyright file="EnvironmentConfigurationHelper.cs" company="OpenTelemetry Authors">
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
#if NETCOREAPP3_1
using OpenTelemetry.Exporter;
#endif
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace OpenTelemetry.AutoInstrumentation.Configuration;

internal static class EnvironmentConfigurationHelper
{
    private static readonly Dictionary<Instrumentation, Action<TracerProviderBuilder>> AddInstrumentation = new()
    {
        [Instrumentation.HttpClient] = builder => builder.AddHttpClientInstrumentation(),
        [Instrumentation.AspNet] = builder => builder.AddSdkAspNetInstrumentation(),
        [Instrumentation.SqlClient] = builder => builder.AddSqlClientInstrumentation()
    };

    public static TracerProviderBuilder UseEnvironmentVariables(this TracerProviderBuilder builder, Settings settings)
    {
        var resourceBuilder = ResourceBuilder.CreateDefault();

        builder
            .SetResourceBuilder(resourceBuilder)
            .SetExporter(settings);

        foreach (var enabledInstrumentation in settings.EnabledInstrumentations)
        {
            if (AddInstrumentation.TryGetValue(enabledInstrumentation, out var addInstrumentation))
            {
                addInstrumentation(builder);
            }
        }

        builder.AddSource(settings.ActivitySources.ToArray());
        foreach (var legacySource in settings.LegacySources)
        {
            builder.AddLegacySource(legacySource);
        }

        return builder;
    }

    public static TracerProviderBuilder AddSdkAspNetInstrumentation(this TracerProviderBuilder builder)
    {
#if NET462
        return builder.AddAspNetInstrumentation();
#elif NETCOREAPP3_1_OR_GREATER
            return builder.AddAspNetCoreInstrumentation();
#else
            return builder;
#endif
    }

    private static TracerProviderBuilder SetExporter(this TracerProviderBuilder builder, Settings settings)
    {
        if (settings.ConsoleExporterEnabled)
        {
            builder.AddConsoleExporter();
        }

        switch (settings.TracesExporter)
        {
            case TracesExporter.Zipkin:
                builder.AddZipkinExporter();
                break;
            case TracesExporter.Jaeger:
                builder.AddJaegerExporter();
                break;
            case TracesExporter.Otlp:
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
            case TracesExporter.None:
                break;
            default:
                throw new ArgumentOutOfRangeException($"Traces exporter '{settings.TracesExporter}' is incorrect");
        }

        return builder;
    }
}
