using System;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace OpenTelemetry.AutoInstrumentation.ClrProfiler.Managed.Configuration
{
    internal static class TraceConfigurationHelper
    {
        public static TracerProviderBuilder UseEnvironmentVariables(this TracerProviderBuilder builder, Settings settings)
        {
            var resourceBuilder = ResourceBuilder
                .CreateDefault()
                .AddService(settings.ServiceName ?? "UNKNOWN_SERVICE_NAME", serviceVersion: settings.ServiceVersion);

            return builder
                .SetResourceBuilder(resourceBuilder)
                .SetExporter(settings);
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
            // TODO for PoC we always want the console exporter to be present -- remove later
            builder.AddConsoleExporter();

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
