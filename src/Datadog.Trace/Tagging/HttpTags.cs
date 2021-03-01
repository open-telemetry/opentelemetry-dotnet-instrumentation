using Datadog.Trace.ExtensionMethods;

namespace Datadog.Trace.Tagging
{
    internal class HttpTags : InstrumentationTags, IHasStatusCode
    {
        private readonly HttpTagsKeysMapping _convention;

        public HttpTags(HttpTagsKeysMapping convention)
        {
            _convention = convention;
        }

        // for sake of TagsListTests using reflection
        private HttpTags()
            : this(new HttpTagsKeysMapping
            {
                HandlerType = "handler-type",
                InstrumentationName = "instr",
                Method = "method",
                StatusCode = "status-code",
                Url = "url",
            })
        {
        }

        public override string SpanKind => SpanKinds.Client;

        public string InstrumentationName { get; set; }

        public string HttpMethod { get; set; }

        public string HttpUrl { get; set; }

        public string HttpClientHandlerType { get; set; }

        public string HttpStatusCode { get; set; }

        protected override IProperty<string>[] GetAdditionalTags()
        {
            var properties = InstrumentationTagsProperties;

            if (_convention.Method != null)
            {
                properties = properties.Concat(new Property<HttpTags, string>(_convention.Method, t => t.HttpMethod, (t, v) => t.HttpMethod = v));
            }

            if (_convention.Url != null)
            {
                properties = properties.Concat(new Property<HttpTags, string>(_convention.Url, t => t.HttpUrl, (t, v) => t.HttpUrl = v));
            }

            if (_convention.StatusCode != null)
            {
                properties = properties.Concat(new Property<HttpTags, string>(_convention.StatusCode, t => t.HttpStatusCode, (t, v) => t.HttpStatusCode = v));
            }

            if (_convention.InstrumentationName != null)
            {
                properties = properties.Concat(new Property<HttpTags, string>(_convention.InstrumentationName, t => t.InstrumentationName, (t, v) => t.InstrumentationName = v));
            }

            if (_convention.HandlerType != null)
            {
                properties = properties.Concat(new Property<HttpTags, string>(_convention.HandlerType, t => t.HttpClientHandlerType, (t, v) => t.HttpClientHandlerType = v));
            }

            return properties;
        }
    }
}
