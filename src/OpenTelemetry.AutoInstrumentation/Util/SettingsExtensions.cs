using OpenTelemetry.AutoInstrumentation.Configuration;

namespace OpenTelemetry.AutoInstrumentation.Util;

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
