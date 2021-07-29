using System;
using System.Collections.Generic;
using System.Linq;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace OpenTelemetry.AutoInstrumentation.ClrProfiler.Managed.Configuration
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
            var resourceBuilder = ResourceBuilder
                .CreateDefault()
                .AddService(settings.ServiceName ?? "UNKNOWN_SERVICE_NAME", serviceVersion: settings.ServiceVersion);

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
#if NET452 || NET461
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
                    builder.AddZipkinExporter(options =>
                    {
                        options.Endpoint = settings.ZipkinEndpoint;
                        options.ExportProcessorType = ExportProcessorType.Simple; // for PoC
                    });

                    break;
                case "jaeger":
#if NET452
                    throw new NotSupportedException();
#else
                    var agentHost = settings.JaegerExporterAgentHost;
                    var agentPort = settings.JaegerExporterAgentPort;

                    builder.AddJaegerExporter(options =>
                    {
                        options.AgentHost = agentHost;
                        options.AgentPort = agentPort;
                        options.ExportProcessorType = ExportProcessorType.Simple; // for PoC
                    });
                    break;
#endif
                case "OTLP":
#if NET452
                    throw new NotSupportedException();
#else
                    builder.AddOtlpExporter(
                        options =>
                        {
                            // TODO remove when sdk version with env vars support is released
                            var endpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");
                            if (!string.IsNullOrEmpty(endpoint))
                            {
                                options.Endpoint = new Uri(endpoint);
                            }
                        });
                    break;
#endif
                case "":
                    break;
                default:
                    throw new ArgumentOutOfRangeException("The exporter name is not recognised");
            }

            return builder;
        }
    }
}
