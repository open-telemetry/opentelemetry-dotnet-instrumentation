using System;

namespace OpenTelemetry.ClrProfiler.Managed.Configuration
{
    /// <summary>
    /// Contains integration-specific settings.
    /// </summary>
    public class IntegrationSettings
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IntegrationSettings"/> class.
        /// </summary>
        /// <param name="integrationName">The integration name.</param>
        /// <param name="source">The <see cref="IConfigurationSource"/> to use when retrieving configuration values.</param>
        public IntegrationSettings(string integrationName, IConfigurationSource source)
        {
            IntegrationName = integrationName ?? throw new ArgumentNullException(nameof(integrationName));

            if (source == null)
            {
                return;
            }

            Enabled = source.GetBool(string.Format(ConfigurationKeys.Integrations.Enabled, integrationName)) ??
                      source.GetBool(string.Format("OTEL_{0}_ENABLED", integrationName));
        }

        /// <summary>
        /// Gets the name of the integration. Used to retrieve integration-specific settings.
        /// </summary>
        public string IntegrationName { get; }

        /// <summary>
        /// Gets or sets a value indicating whether
        /// this integration is enabled.
        /// </summary>
        public bool? Enabled { get; set; }
    }
}
