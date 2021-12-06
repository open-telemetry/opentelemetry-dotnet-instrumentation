using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
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
                    builder.AddZipkinExporter(options =>
                    {
                        options.ExportProcessorType = ExportProcessorType.Simple; // workaround for https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/issues/290
                    });
                    break;
                case "jaeger":
                    builder.AddJaegerExporter(options =>
                    {
                        options.ExportProcessorType = ExportProcessorType.Simple; // workaround for https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/issues/290
                    });
                    break;
                case "otlp":
#if NETCOREAPP3_1
                    // Adding the OtlpExporter creates a GrpcChannel.
                    // This switch must be set before creating a GrpcChannel/HttpClient when calling an insecure gRPC service.
                    // See: https://docs.microsoft.com/aspnet/core/grpc/troubleshoot#call-insecure-grpc-services-with-net-core-client
                    AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
#endif
                    builder.AddOtlpExporter(options =>
                    {
                        options.ExportProcessorType = ExportProcessorType.Simple; // workaround for https://github.com/open-telemetry/opentelemetry-dotnet-instrumentation/issues/290
                    });
                    break;
                case "":
                case null:
                    break;
                default:
                    throw new ArgumentOutOfRangeException("The exporter name is not recognised");
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
            catch (Exception ex)
            {
                // TODO: Log service name creation error
                Console.WriteLine($">>>>>>>>>>>>>>>>>>>>>>> Error: Could not get service name. Message: {ex.Message}");
                throw;
            }
        }
    }
}
