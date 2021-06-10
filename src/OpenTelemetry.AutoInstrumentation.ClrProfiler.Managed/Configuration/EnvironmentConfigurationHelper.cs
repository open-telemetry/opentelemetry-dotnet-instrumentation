using System;
using System.Collections.Generic;
using System.Linq;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace OpenTelemetry.AutoInstrumentation.ClrProfiler.Managed.Configuration
{
    internal static class EnvironmentConfigurationHelper
    {
        private static readonly Dictionary<IntegrationIds, Action<TracerProviderBuilder>> AddInstrumentation = new()
        {
            [IntegrationIds.HttpClient] = builder => builder.AddHttpClientInstrumentation(),
            [IntegrationIds.AspNet] = builder => builder.AddSdkAspNetInstrumentation(),
            [IntegrationIds.SqlClient] = builder => builder.AddSqlClientInstrumentation()
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
                case "":
                    break;
                default:
                    throw new ArgumentOutOfRangeException("The exporter name is not recognised");
#endif
            }

            return builder;
        }
    }
}
