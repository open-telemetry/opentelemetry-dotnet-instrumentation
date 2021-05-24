using System;

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

            var agentUri = Environment.GetEnvironmentVariable(ConfigurationKeys.AgentUri) ?? $"http://localhost:8126";
            AgentUri = new Uri(agentUri);

            JaegerExporterAgentHost = Environment.GetEnvironmentVariable(ConfigurationKeys.JaegerExporterAgentHost) ?? "localhost";
            JaegerExporterAgentPort = int.TryParse(Environment.GetEnvironmentVariable(ConfigurationKeys.JaegerExporterAgentPort), out var port) ? port : 6831;

            LoadTracerAtStartup = bool.TryParse(Environment.GetEnvironmentVariable(ConfigurationKeys.LoadTracerAtStartup), out var loadTracerAtStartup) ? loadTracerAtStartup : true;
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
        public Uri AgentUri { get; }

        /// <summary>
        /// Gets jaeger exporter agent host.
        /// </summary>
        public string JaegerExporterAgentHost { get; }

        /// <summary>
        /// Gets jaeger exporter agent port.
        /// </summary>
        public int JaegerExporterAgentPort { get; }

        private static Settings Create()
        {
            return new();
        }
    }
}
