using Datadog.Trace.Configuration;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.AWS
{
    internal static class AwsConstants
    {
        internal const string IntegrationName = nameof(IntegrationIds.AwsSdk);
        internal static readonly IntegrationInfo IntegrationId = IntegrationRegistry.GetIntegrationInfo(IntegrationName);
    }
}
