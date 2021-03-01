using Datadog.Trace.Tagging;

namespace Datadog.Trace.Conventions
{
    /// <summary>
    /// Convention used to when defining operation names, span tags, statuses for outbound HTTP requests.
    /// </summary>
    internal interface IOutboundHttpConvention
    {
        Scope CreateScope(OutboundHttpArgs args, out HttpTags tags);
    }
}