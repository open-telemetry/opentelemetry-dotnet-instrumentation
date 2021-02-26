namespace Datadog.Trace.ClrProfiler.Conventions
{
    internal interface IOutboundHttpConvention
    {
        void Apply(OutboundHttpArgs args);
    }
}