using Datadog.Trace.Configuration;
using Datadog.Trace.Util;

namespace Datadog.Trace.Conventions
{
    internal class DatadogOutboundHttpConvention : IOutboundHttpConvention
    {
        private readonly TracerSettings _tracerSettings;

        public DatadogOutboundHttpConvention(TracerSettings tracerSettings)
        {
            _tracerSettings = tracerSettings;
        }

        public void Apply(OutboundHttpArgs args)
        {
            var span = args.Span;
            var requestUri = args.RequestUri;
            var httpMethod = args.HttpMethod;
            var tags = args.HttpTags;
            var integrationId = args.IntegrationInfo;

            span.OperationName = "http.request";

            span.Type = SpanTypes.Http;

            string resourceUrl = requestUri != null ? UriHelpers.CleanUri(requestUri, removeScheme: true, tryRemoveIds: true) : null;
            string httpUrl = requestUri != null ? UriHelpers.CleanUri(requestUri, removeScheme: false, tryRemoveIds: false) : null;
            span.ResourceName = $"{httpMethod} {resourceUrl}";

            tags.HttpMethod = httpMethod?.ToUpperInvariant();

            tags.HttpUrl = httpUrl;

            tags.InstrumentationName = IntegrationRegistry.GetName(integrationId);

            tags.SetAnalyticsSampleRate(integrationId, _tracerSettings, enabledWithGlobalSetting: false);
        }
    }
}