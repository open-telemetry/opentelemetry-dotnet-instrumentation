using Datadog.Trace.ExtensionMethods;
using Datadog.Trace.Tagging;

namespace Datadog.Trace.Conventions
{
    internal class OtelHttpTags : HttpTags
    {
        protected static readonly IProperty<string>[] HttpTagsProperties =
            InstrumentationTagsProperties.Concat(
                new Property<HttpTags, string>(Trace.Tags.HttpStatusCode, t => t.HttpStatusCode, (t, v) => t.HttpStatusCode = v),
                new Property<HttpTags, string>("http-client-handler-type", t => t.HttpClientHandlerType, (t, v) => t.HttpClientHandlerType = v),
                new Property<HttpTags, string>(Trace.Tags.HttpMethod, t => t.HttpMethod, (t, v) => t.HttpMethod = v),
                new Property<HttpTags, string>(Trace.Tags.HttpUrl, t => t.HttpUrl, (t, v) => t.HttpUrl = v),
                new Property<OtelHttpTags, string>("http.scheme", t => t.HttpScheme, (t, v) => t.HttpScheme = v),
                new Property<OtelHttpTags, string>("http.host", t => t.HttpHost, (t, v) => t.HttpHost = v),
                new Property<OtelHttpTags, string>("http.target", t => t.HttpTarget, (t, v) => t.HttpTarget = v),
                new Property<HttpTags, string>(Trace.Tags.InstrumentationName, t => t.InstrumentationName, (t, v) => t.InstrumentationName = v));

        public string HttpScheme { get; set; }

        public string HttpHost { get; set; }

        public string HttpTarget { get; set; }

        protected override IProperty<string>[] GetAdditionalTags() => HttpTagsProperties;
    }
}