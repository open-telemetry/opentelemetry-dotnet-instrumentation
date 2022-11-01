// <copyright file="EnvironmentConfigurationTracerHelper.cs" company="OpenTelemetry Authors">
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
using OpenTelemetry.Trace;

namespace OpenTelemetry.AutoInstrumentation.Configuration;

internal static class EnvironmentConfigurationTracerHelper
{
    public static TracerProviderBuilder UseEnvironmentVariables(this TracerProviderBuilder builder, TracerSettings settings)
    {
        builder.SetExporter(settings);

        foreach (var enabledInstrumentation in settings.EnabledInstrumentations)
        {
            _ = enabledInstrumentation switch
            {
                TracerInstrumentation.AspNet => Wrappers.AddSdkAspNetInstrumentation(builder),
                TracerInstrumentation.GrpcNetClient => Wrappers.AddGrpcClientInstrumentation(builder),
                TracerInstrumentation.HttpClient => Wrappers.AddHttpClientInstrumentation(builder),
                TracerInstrumentation.Npgsql => builder.AddSource("Npgsql"),
                TracerInstrumentation.SqlClient => Wrappers.AddSqlClientInstrumentation(builder),
                TracerInstrumentation.Wcf => Wrappers.AddWcfInstrumentation(builder),
#if NETCOREAPP3_1_OR_GREATER
                TracerInstrumentation.MassTransit => builder.AddSource("MassTransit"),
                TracerInstrumentation.MongoDB => builder.AddSource("MongoDB.Driver.Core.Extensions.DiagnosticSources"),
                TracerInstrumentation.MySqlData => builder.AddSource("OpenTelemetry.Instrumentation.MySqlData"),
                TracerInstrumentation.StackExchangeRedis => builder.AddSource("OpenTelemetry.Instrumentation.StackExchangeRedis"),
#endif
                _ => null
            };
        }

        builder.AddSource(settings.ActivitySources.ToArray());
        foreach (var legacySource in settings.LegacySources)
        {
            builder.AddLegacySource(legacySource);
        }

        return builder;
    }

    private static TracerProviderBuilder SetExporter(this TracerProviderBuilder builder, TracerSettings settings)
    {
        if (settings.ConsoleExporterEnabled)
        {
            Wrappers.AddConsoleExporter(builder);
        }

        return settings.TracesExporter switch
        {
            TracesExporter.Zipkin => Wrappers.AddZipkinExporter(builder),
            TracesExporter.Jaeger => Wrappers.AddJaegerExporter(builder),
            TracesExporter.Otlp => Wrappers.AddOtlpExporter(builder, settings),
            TracesExporter.None => builder,
            _ => throw new ArgumentOutOfRangeException($"Traces exporter '{settings.TracesExporter}' is incorrect")
        };
    }

    /// <summary>
    /// This class wraps external extensions methods to ensure the dlls are not loaded, if not necessary.
    /// </summary>
    private static class Wrappers
    {
        // Invstrumentations

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static TracerProviderBuilder AddWcfInstrumentation(TracerProviderBuilder builder)
        {
            return builder.AddWcfInstrumentation();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static TracerProviderBuilder AddHttpClientInstrumentation(TracerProviderBuilder builder)
        {
            return builder.AddHttpClientInstrumentation();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static TracerProviderBuilder AddSdkAspNetInstrumentation(TracerProviderBuilder builder)
        {
#if NET462
            builder.AddAspNetInstrumentation();
#elif NETCOREAPP3_1_OR_GREATER
            builder.AddSource("OpenTelemetry.Instrumentation.AspNetCore");
            builder.AddLegacySource("Microsoft.AspNetCore.Hosting.HttpRequestIn");
#endif

            return builder;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static TracerProviderBuilder AddSqlClientInstrumentation(TracerProviderBuilder builder)
        {
            return builder.AddSqlClientInstrumentation();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static TracerProviderBuilder AddGrpcClientInstrumentation(TracerProviderBuilder builder)
        {
            return builder.AddGrpcClientInstrumentation(options =>
                    options.SuppressDownstreamInstrumentation = !Instrumentation.TracerSettings.EnabledInstrumentations.Contains(TracerInstrumentation.HttpClient));
        }

        // Exporters

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static TracerProviderBuilder AddConsoleExporter(TracerProviderBuilder builder)
        {
            return builder.AddConsoleExporter();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static TracerProviderBuilder AddZipkinExporter(TracerProviderBuilder builder)
        {
            return builder.AddZipkinExporter();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static TracerProviderBuilder AddJaegerExporter(TracerProviderBuilder builder)
        {
            return builder.AddJaegerExporter();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static TracerProviderBuilder AddOtlpExporter(TracerProviderBuilder builder, TracerSettings settings)
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
            return builder.AddOtlpExporter(options =>
            {
                if (settings.OtlpExportProtocol.HasValue)
                {
                    options.Protocol = settings.OtlpExportProtocol.Value;
                }
            });
        }
    }
}
