using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace OpenTelemetry.AutoInstrumentation.ClrProfiler.Managed.Configuration
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

            Environment = source.GetString(ConfigurationKeys.Environment);

            ServiceName = source.GetString(ConfigurationKeys.ServiceName);
            ServiceVersion = source.GetString(ConfigurationKeys.ServiceVersion);
            Exporter = source.GetString(ConfigurationKeys.Exporter);

            ZipkinEndpoint = new Uri(source.GetString(ConfigurationKeys.ZipkinEndpoint) ?? "http://localhost:8126");

            JaegerExporterAgentHost = source.GetString(ConfigurationKeys.JaegerExporterAgentHost) ?? "localhost";
            JaegerExporterAgentPort = source.GetInt32(ConfigurationKeys.JaegerExporterAgentPort) ?? 6831;

            LoadTracerAtStartup = source.GetBool(ConfigurationKeys.LoadTracerAtStartup) ?? true;

            ConsoleExporterEnabled = source.GetBool(ConfigurationKeys.ConsoleExporterEnabled) ?? true;

            EnabledInstrumentations = Enum.GetValues(typeof(InstrumentationType)).Cast<InstrumentationType>().ToList();
            var disabledConfigurations = source.GetString(ConfigurationKeys.EnabledInstrumentations);
            if (disabledConfigurations != null)
            {
                foreach (var instrumentation in disabledConfigurations.Split(separator: ','))
                {
                    if (Enum.TryParse(instrumentation, out InstrumentationType parsedType))
                    {
                        EnabledInstrumentations.Remove(parsedType);
                    }
                    else
                    {
                        // TODO replace with proper logging
                        Console.WriteLine($"Could not parse instrumentation \"{instrumentation}\". Skipping...");
                    }
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

            TraceEnabled = source.GetBool(ConfigurationKeys.TraceEnabled) ?? true;
            LoadTracerAtStartup = source.GetBool(ConfigurationKeys.LoadTracerAtStartup) ?? true;

            Integrations = new IntegrationSettingsCollection(source);

            GlobalTags = source.GetDictionary(ConfigurationKeys.GlobalTags) ??
             // default value (empty)
             new ConcurrentDictionary<string, string>();
        }

        /// <summary>
        /// Gets or sets the default environment name applied to all spans.
        /// </summary>
        /// <seealso cref="ConfigurationKeys.Environment"/>
        public string Environment { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether tracing is enabled.
        /// Default is <c>true</c>.
        /// </summary>
        /// <seealso cref="ConfigurationKeys.TraceEnabled"/>
        public bool TraceEnabled { get; set; }

        /// <summary>
        /// Gets the name of the service.
        /// </summary>
        public string ServiceName { get; }

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
        /// Gets agent uri.
        /// </summary>
        public Uri ZipkinEndpoint { get; }

        /// <summary>
        /// Gets jaeger exporter agent host.
        /// </summary>
        public string JaegerExporterAgentHost { get; }

        /// <summary>
        /// Gets jaeger exporter agent port.
        /// </summary>
        public int JaegerExporterAgentPort { get; }

        /// <summary>
        /// Gets a value indicating whether the console exporter is enabled.
        /// </summary>
        public bool ConsoleExporterEnabled { get; }

        /// <summary>
        /// Gets the list of enabled instrumentations.
        /// </summary>
        public IList<InstrumentationType> EnabledInstrumentations { get; } = new List<InstrumentationType>();

        /// <summary>
        /// Gets the list of activitysources to be added to the tracer at the startup.
        /// </summary>
        public IList<string> ActivitySources { get; } = new List<string> { "OpenTelemetry.AutoInstrumentation.*" };

        /// <summary>
        /// Gets a collection of <see cref="Integrations"/> keyed by integration name.
        /// </summary>
        public IntegrationSettingsCollection Integrations { get; }

        /// <summary>
        /// Gets or sets the global tags, which are applied to all <see cref="Activity"/>s.
        /// </summary>
        public IDictionary<string, string> GlobalTags { get; set; }

        internal static Settings FromDefaultSources()
        {
            // env > AppSettings > datadog.json
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
    }
}
