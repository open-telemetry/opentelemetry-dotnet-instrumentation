using Datadog.Trace.ExtensionMethods;

namespace Datadog.Trace.Tagging
{
    internal class HttpTags : InstrumentationTags, IHasStatusCode
    {
        private readonly HttpTagsKeysMapping _convension;

        public HttpTags(HttpTagsKeysMapping convension)
        {
            _convension = convension;
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

            if (_convension.Method != null)
            {
                properties = properties.Concat(new Property<HttpTags, string>(_convension.Method, t => t.HttpMethod, (t, v) => t.HttpMethod = v));
            }

            if (_convension.Url != null)
            {
                properties = properties.Concat(new Property<HttpTags, string>(_convension.Url, t => t.HttpUrl, (t, v) => t.HttpUrl = v));
            }

            if (_convension.StatusCode != null)
            {
                properties = properties.Concat(new Property<HttpTags, string>(_convension.StatusCode, t => t.HttpStatusCode, (t, v) => t.HttpStatusCode = v));
            }

            if (_convension.InstrumentationName != null)
            {
                properties = properties.Concat(new Property<HttpTags, string>(_convension.InstrumentationName, t => t.InstrumentationName, (t, v) => t.InstrumentationName = v));
            }

            if (_convension.HandlerType != null)
            {
                properties = properties.Concat(new Property<HttpTags, string>(_convension.HandlerType, t => t.HttpClientHandlerType, (t, v) => t.HttpClientHandlerType = v));
            }

            return properties;
        }
    }
}
