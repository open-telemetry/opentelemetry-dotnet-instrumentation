using System;
using Datadog.Trace.Configuration;
using Datadog.Trace.Tagging;

namespace Datadog.Trace.ClrProfiler.Conventions
{
    internal struct OutboundHttpArgs
    {
        public Span Span;
        public HttpTags HttpTags;
        public string HttpMethod;
        public Uri RequestUri;
        public IntegrationInfo IntegrationInfo;
        public Tracer Tracer;
    }
}