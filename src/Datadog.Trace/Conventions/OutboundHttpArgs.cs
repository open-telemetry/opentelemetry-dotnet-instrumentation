using System;
using Datadog.Trace.Configuration;
using Datadog.Trace.Tagging;

namespace Datadog.Trace.Conventions
{
    /// <summary>
    /// Arguments used by <c>"IOutboundHttpConvention"</c>.
    /// </summary>
    internal struct OutboundHttpArgs
    {
        public ulong? SpanId;

        /// <summary>
        /// Request's HTTP method.
        /// </summary>
        public string HttpMethod;

        /// <summary>
        /// Request's URI.
        /// </summary>
        public Uri RequestUri;

        /// <summary>
        /// Request's URI.
        /// </summary>
        public IntegrationInfo IntegrationInfo;
    }
}