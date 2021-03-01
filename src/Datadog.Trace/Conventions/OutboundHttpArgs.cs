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
        /// <summary>
        /// Span for which the conventions should be applied.
        /// </summary>
        public Span Span;

        /// <summary>
        /// Tags for which the conventions should be applied.
        /// </summary>
        public HttpTags HttpTags;

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