using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenTelemetry.AutoInstrumentation.ClrProfiler.Managed.Configuration
{
    // TODO Move settings to more suitable place?

    /// <summary>
    /// Settings
    /// </summary>
    public class Settings
    {
        private static readonly Lazy<Settings> LazyInstance = new(Create);

        private Settings()
        {
            ServiceName = Environment.GetEnvironmentVariable(ConfigurationKeys.ServiceName);
            ServiceVersion = Environment.GetEnvironmentVariable(ConfigurationKeys.ServiceVersion);
            Exporter = Environment.GetEnvironmentVariable(ConfigurationKeys.Exporter);

            var zipkinEndpoint = Environment.GetEnvironmentVariable(ConfigurationKeys.ZipkinEndpoint) ?? $"http://localhost:8126";
            ZipkinEndpoint = new Uri(zipkinEndpoint);

            JaegerExporterAgentHost = Environment.GetEnvironmentVariable(ConfigurationKeys.JaegerExporterAgentHost) ?? "localhost";
            JaegerExporterAgentPort = int.TryParse(Environment.GetEnvironmentVariable(ConfigurationKeys.JaegerExporterAgentPort), out var port) ? port : 6831;

            LoadTracerAtStartup = bool.TryParse(Environment.GetEnvironmentVariable(ConfigurationKeys.LoadTracerAtStartup), out var loadTracerAtStartup) ? loadTracerAtStartup : true;

            ConsoleExporterEnabled = bool.TryParse(Environment.GetEnvironmentVariable(ConfigurationKeys.ConsoleExporterEnabled), out var consoleExporterEnabled) ? consoleExporterEnabled : true;

            var enabledInstrumentations = Environment.GetEnvironmentVariable(ConfigurationKeys.EnabledInstrumentations);
            if (enabledInstrumentations == null)
            {
                EnabledInstrumentations = Enum.GetValues(typeof(InstrumentationType)).Cast<InstrumentationType>().ToList();
            }
            else
            {
                foreach (var instrumentation in enabledInstrumentations.Split(','))
                {
                    if (Enum.TryParse(instrumentation, out InstrumentationType parsedType))
                    {
                        EnabledInstrumentations.Add(parsedType);
                    }
                    else
                    {
                        // TODO replace with proper logging
                        Console.WriteLine($"Could not parse instrumentation \"{instrumentation}\". Skipping...");
                    }
                }
            }

            var additionalSources = Environment.GetEnvironmentVariable(ConfigurationKeys.AdditionalSources);
            if (additionalSources != null)
            {
                foreach (var sourceName in additionalSources.Split(','))
                {
                    ActivitySources.Add(sourceName);
                }
            }
        }

        /// <summary>
        /// Gets the settings instance.
        /// </summary>
        public static Settings Instance => LazyInstance.Value;

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

        private static Settings Create()
        {
            return new();
        }
    }
}
