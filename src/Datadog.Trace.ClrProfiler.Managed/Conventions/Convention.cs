using Datadog.Trace.Configuration;

namespace Datadog.Trace.ClrProfiler.Conventions
{
    internal struct Convention
    {
        public IOutboundHttpConvention OutboundHttpScopeConvention;

        private static Convention singleton;
        private static volatile bool initialized;
        private static object syncRoot = new object();

        public static Convention Get(TracerSettings settings)
        {
            if (!initialized)
            {
                lock (syncRoot)
                {
                    if (!initialized)
                    {
                        // TODO: here we can select a convention based on TracerSettings (like chosing exporter via OTEL_EXPORTER)
                        singleton.OutboundHttpScopeConvention = new DatadogOutboundHttpConvention();
                        initialized = true;
                    }
                }
            }

            return singleton;
        }
    }
}