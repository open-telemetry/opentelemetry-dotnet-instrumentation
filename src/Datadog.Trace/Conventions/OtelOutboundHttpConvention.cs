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
            string serviceName = _tracer.Settings.GetServiceName(_tracer, "http-client");
            var scope = _tracer.StartActiveWithTags("http.request", tags: tags, serviceName: serviceName, spanId: args.SpanId);

            scope.Span.Type = SpanTypes.Http;
            var requestUri = args.RequestUri;
            var httpMethod = args.HttpMethod;
            string resourceUrl = requestUri != null ? UriHelpers.CleanUri(requestUri, removeScheme: true, tryRemoveIds: true) : null;
            scope.Span.ResourceName = $"{httpMethod} {resourceUrl}";
            tags.HttpMethod = httpMethod;
            tags.HttpUrl = $"{requestUri.Scheme}{Uri.SchemeDelimiter}{requestUri.Authority}{requestUri.PathAndQuery}{requestUri.Fragment}";

            var integrationId = args.IntegrationInfo;
            tags.InstrumentationName = IntegrationRegistry.GetName(integrationId);
            tags.SetAnalyticsSampleRate(integrationId, _tracer.Settings, enabledWithGlobalSetting: false);
            return scope;
        }
    }
}