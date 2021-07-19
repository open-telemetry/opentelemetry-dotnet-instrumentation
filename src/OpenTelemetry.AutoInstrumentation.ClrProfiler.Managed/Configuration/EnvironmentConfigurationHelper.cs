using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
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
            string serviceName = settings.ServiceName
                ?? GetApplicationName()
                ?? "UNKNOWN_SERVICE_NAME";

            var resourceBuilder = ResourceBuilder
                .CreateDefault()
                .AddService(serviceName, serviceVersion: settings.ServiceVersion);

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

        /// <summary>
        /// Gets an "application name" for the executing application by looking at
        /// the hosted app name (.NET Framework on IIS only), assembly name, and process name.
        /// </summary>
        /// <returns>The default service name.</returns>
        private static string GetApplicationName()
        {
            try
            {
#if NETFRAMEWORK
                // System.Web.dll is only available on .NET Framework
                if (System.Web.Hosting.HostingEnvironment.IsHosted)
                {
                    // if this app is an ASP.NET application, return "SiteName/ApplicationVirtualPath".
                    // note that ApplicationVirtualPath includes a leading slash.
                    return (System.Web.Hosting.HostingEnvironment.SiteName + System.Web.Hosting.HostingEnvironment.ApplicationVirtualPath).TrimEnd('/');
                }
#endif

                return Assembly.GetEntryAssembly()?.GetName().Name ??
                       Process.GetCurrentProcess().ProcessName;
            }
            catch (Exception)
            {
                Console.WriteLine("ERROR >> Error creating default service name.");
                return null;
            }
        }
    }
}
