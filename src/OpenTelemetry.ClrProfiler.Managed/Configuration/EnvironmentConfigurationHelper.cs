using System;
using System.Collections.Generic;
using System.Linq;
#if NETCOREAPP3_1
using OpenTelemetry.Exporter;
#endif
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace OpenTelemetry.ClrProfiler.Managed.Configuration
{
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
#if NET461
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

            switch (settings.Exporter)
            {
                case "zipkin":
                    builder.AddZipkinExporter();
                    break;
                case "jaeger":
                    builder.AddJaegerExporter();
                    break;
                case "otlp":
#if NETCOREAPP3_1
                    if (!settings.OtlpExportProtocol.HasValue || settings.OtlpExportProtocol != OtlpExportProtocol.HttpProtobuf)
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

                        if (settings.OtlpExportEndpoint != null)
                        {
                            options.Endpoint = settings.OtlpExportEndpoint;
                        }
                    });
                    break;
                case "":
                case null:
                    break;
                default:
                    throw new FormatException($"The exporter name '{settings}' is not recognized");
            }

            return builder;
        }
    }
}
