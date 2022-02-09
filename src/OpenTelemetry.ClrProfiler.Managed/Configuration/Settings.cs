using System;
using System.Collections.Generic;
using System.Linq;
using OpenTelemetry.Exporter;

namespace OpenTelemetry.ClrProfiler.Managed.Configuration
{
    // TODO Move settings to more suitable place?

    /// <summary>
    /// Settings
    /// </summary>
    public class Settings
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Settings"/> class
        /// using the specified <see cref="IConfigurationSource"/> to initialize values.
        /// </summary>
        /// <param name="source">The <see cref="IConfigurationSource"/> to use when retrieving configuration values.</param>
        private Settings(IConfigurationSource source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            Exporter = source.GetString(ConfigurationKeys.Exporter) ?? "otlp";
            OtlpExportProtocol = GetExporterOtlpProtocol(source);
            OtlpExportEndpoint = GetExporterOtlpEndpoint(source, OtlpExportProtocol);

            LoadTracerAtStartup = source.GetBool(ConfigurationKeys.LoadTracerAtStartup) ?? true;
            ConsoleExporterEnabled = source.GetBool(ConfigurationKeys.ConsoleExporterEnabled) ?? true;

            var instrumentations = new Dictionary<string, Instrumentation>();
            var enabledInstrumentations = source.GetString(ConfigurationKeys.Instrumentations);
            if (enabledInstrumentations != null)
            {
                foreach (var instrumentation in enabledInstrumentations.Split(separator: ','))
                {
                    if (Enum.TryParse(instrumentation, out Instrumentation parsedType))
                    {
                        instrumentations[instrumentation] = parsedType;
                    }
                    else
                    {
                        throw new ArgumentException($"The \"{instrumentation}\" is not recognized as supported instrumentation and cannot be disabled");
                    }
                }
            }

            var disabledInstrumentations = source.GetString(ConfigurationKeys.DisabledInstrumentations);
            if (disabledInstrumentations != null)
            {
                foreach (var instrumentation in disabledInstrumentations.Split(separator: ','))
                {
                    instrumentations.Remove(instrumentation);
                }
            }

            EnabledInstrumentations = instrumentations.Values.ToList();

            var providerPlugins = source.GetString(ConfigurationKeys.ProviderPlugins);
            if (providerPlugins != null)
            {
                foreach (var pluginAssemblyQualifiedName in providerPlugins.Split(':'))
                {
                    TracerPlugins.Add(pluginAssemblyQualifiedName);
                }
            }

            var additionalSources = source.GetString(ConfigurationKeys.AdditionalSources);
            if (additionalSources != null)
            {
                foreach (var sourceName in additionalSources.Split(separator: ','))
                {
                    ActivitySources.Add(sourceName);
                }
            }

            var legacySources = source.GetString(ConfigurationKeys.LegacySources);
            if (legacySources != null)
            {
                foreach (var sourceName in legacySources.Split(separator: ','))
                {
                    LegacySources.Add(sourceName);
                }
            }

            TraceEnabled = source.GetBool(ConfigurationKeys.TraceEnabled) ?? true;
            LoadTracerAtStartup = source.GetBool(ConfigurationKeys.LoadTracerAtStartup) ?? true;

            Integrations = new IntegrationSettingsCollection(source);
        }

        /// <summary>
        /// Gets or sets a value indicating whether tracing is enabled.
        /// Default is <c>true</c>.
        /// </summary>
        /// <seealso cref="ConfigurationKeys.TraceEnabled"/>
        public bool TraceEnabled { get; set; }

        /// <summary>
        /// Gets the version of the service
        /// </summary>
        public string ServiceVersion { get; }

        /// <summary>
        /// Gets a value indicating whether the tracer should be loaded by the profiler. Default is true.
        /// </summary>
        public bool LoadTracerAtStartup { get; }

        /// <summary>
        /// Gets the name of the exporter.
        /// </summary>
        public string Exporter { get; }

        /// <summary>
        /// Gets the the OTLP transport protocol. Supported values: Grpc and HttpProtobuf.
        /// </summary>
        public OtlpExportProtocol? OtlpExportProtocol { get; }

        /// <summary>
        /// Gets the the OTLP exporter endpoint.
        /// Will be removed when https://github.com/open-telemetry/opentelemetry-dotnet/pull/2868 is released.
        /// </summary>
        // TODO: To be removed when https://github.com/open-telemetry/opentelemetry-dotnet/pull/2868 is released.
        public Uri OtlpExportEndpoint { get; }

        /// <summary>
        /// Gets a value indicating whether the console exporter is enabled.
        /// </summary>
        public bool ConsoleExporterEnabled { get; }

        /// <summary>
        /// Gets the list of enabled instrumentations.
        /// </summary>
        public IList<Instrumentation> EnabledInstrumentations { get; }

        /// <summary>
        /// Gets the list of plugins represented by <see cref="Type.AssemblyQualifiedName"/>.
        /// </summary>
        public IList<string> TracerPlugins { get; } = new List<string>();

        /// <summary>
        /// Gets the list of activitysources to be added to the tracer at the startup.
        /// </summary>
        public IList<string> ActivitySources { get; } = new List<string> { "OpenTelemetry.ClrProfiler.*" };

        /// <summary>
        /// Gets the list of legacy sources to be added to the tracer at the startup.
        /// </summary>
        public IList<string> LegacySources { get; } = new List<string>();

        /// <summary>
        /// Gets a collection of <see cref="Integrations"/> keyed by integration name.
        /// </summary>
        public IntegrationSettingsCollection Integrations { get; }

        internal static Settings FromDefaultSources()
        {
            var configurationSource = new CompositeConfigurationSource
            {
                new EnvironmentConfigurationSource(),

#if NETFRAMEWORK
                // on .NET Framework only, also read from app.config/web.config
                new NameValueConfigurationSource(System.Configuration.ConfigurationManager.AppSettings)
#endif
            };

            return new Settings(configurationSource);
        }

        private static OtlpExportProtocol? GetExporterOtlpProtocol(IConfigurationSource source)
        {
            // the defualt in SDK is grpc. http/protobuf should be default for our purposes
            var exporterOtlpProtocol = source.GetString(ConfigurationKeys.ExporterOtlpProtocol);

            if (string.IsNullOrEmpty(exporterOtlpProtocol) || exporterOtlpProtocol == "http/protobuf")
            {
                // override settings only for http/protobuf
                // the second part of the condition is needed to override default endpoint as long as https://github.com/open-telemetry/opentelemetry-dotnet/pull/2868 is not fixed
                return OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
            }

            // null value here means that it will be handled by OTEL .NET SDK
            return null;
        }

        private static Uri GetExporterOtlpEndpoint(IConfigurationSource source, OtlpExportProtocol? otlpExportProtocol)
        {
            var exporterOtlpEndpoint = source.GetString(ConfigurationKeys.ExporterOtlpEndpoint);

            if (!otlpExportProtocol.HasValue || otlpExportProtocol.Value != OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf || !string.IsNullOrWhiteSpace(exporterOtlpEndpoint))
            {
                // null value here means that it will be handled by OTEL .NET SDK
                return null;
            }

            // override endpoint only for otlp over http/protobuf
            return new Uri("http://localhost:4318/v1/traces");
        }
    }
}
