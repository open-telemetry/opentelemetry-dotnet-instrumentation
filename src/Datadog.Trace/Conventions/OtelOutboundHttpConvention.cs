using System;
using Datadog.Trace.Configuration;
using Datadog.Trace.Tagging;
using Datadog.Trace.Util;

namespace Datadog.Trace.Conventions
{
    internal class OtelOutboundHttpConvention : IOutboundHttpConvention
    {
        private readonly Tracer _tracer;

        public OtelOutboundHttpConvention(Tracer tracer)
        {
            _tracer = tracer;
        }

        public Scope CreateScope(OutboundHttpArgs args, out HttpTags tags)
        {
            tags = new OtelHttpTags();
            
            string operationName = $"HTTP {args.HttpMethod}";
            string serviceName = _tracer.Settings.GetServiceName(_tracer, "http-client");
            var scope = _tracer.StartActiveWithTags(operationName, tags: tags, serviceName: serviceName, spanId: args.SpanId);
            scope.Span.Type = SpanTypes.Http;

            tags.HttpMethod = args.HttpMethod;

            var uri = args.RequestUri;
            tags.HttpUrl = $"{uri.Scheme}{Uri.SchemeDelimiter}{uri.Authority}{uri.PathAndQuery}{uri.Fragment}";
            tags.SetTag("http.scheme", uri.Scheme);
            tags.SetTag("http.host", uri.Authority);
            tags.SetTag("http.target", $"{uri.PathAndQuery}{uri.Fragment}");
            
            tags.InstrumentationName = IntegrationRegistry.GetName(args.IntegrationInfo);
            return scope;
        }
    }
}