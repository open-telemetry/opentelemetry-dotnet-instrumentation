using OpenTelemetry.ClrProfiler.Managed.Configuration;

namespace OpenTelemetry.ClrProfiler.Managed.Util
{
    internal static class SettingsExtensions
    {
        internal static bool IsIntegrationEnabled(this Settings settings, IntegrationInfo integration, bool defaultValue = true)
        {
            if (settings.TraceEnabled && !DomainMetadata.ShouldAvoidAppDomain())
            {
                return settings.Integrations[integration].Enabled ?? defaultValue;
            }

            return false;
        }
    }
}
