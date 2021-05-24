using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace OpenTelemetry.AutoInstrumentation.ClrProfiler.Managed.Configuration
{
    internal static class TraceConfigurationHelper
    {
        public static TracerProviderBuilder UseEnvironmentVariables(this TracerProviderBuilder builder, Settings settings)
        {
            return builder.SetResourceBuilder(ResourceBuilder.CreateDefault()
                                                             .AddService(settings.ServiceName, serviceVersion: settings.ServiceVersion))
                          .SetExporter(settings);
        }

        public static TracerProviderBuilder AddAspNetInstrumentation(this TracerProviderBuilder builder)
        {
#if NETFRAMEWORK
            return builder.AddAspNetInstrumentation();
#else
            return builder.AddAspNetCoreInstrumentation();
#endif
        }

        private static TracerProviderBuilder SetExporter(this TracerProviderBuilder builder, Settings settings)
        {
            switch (settings.Exporter)
            {
                case "zipkin":
                    builder.AddZipkinExporter(options =>
                    {
                        options.Endpoint = settings.AgentUri;
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
                    });

                    break;
#endif
            }

            return builder;
        }
    }
}
