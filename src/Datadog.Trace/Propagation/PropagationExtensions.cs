using System.Collections.Generic;
using Datadog.Trace.Headers;

namespace Datadog.Trace.Propagation
{
    internal static class PropagationExtensions
    {
        public static void Inject(this IPropagator propagator, SpanContext context, IHeadersCollection headers)
        {
            propagator.Inject(context, headers, InjectToHeadersCollection);
        }

        public static SpanContext Extract(this IPropagator propagator, IHeadersCollection headers)
        {
            return propagator.Extract(headers, ExtractFromHeadersCollection);
        }

        public static IEnumerable<KeyValuePair<string, string>> ExtractHeaderTags(this IHeadersCollection headers, IEnumerable<KeyValuePair<string, string>> headerToTagMap)
        {
            foreach (KeyValuePair<string, string> headerNameToTagName in headerToTagMap)
            {
                string headerValue = ParseString(headers, headerNameToTagName.Key);

                if (headerValue != null)
                {
                    yield return new KeyValuePair<string, string>(headerNameToTagName.Value, headerValue);
                }
            }
        }

        public static string ParseString(this IHeadersCollection headers, string headerName)
        {
            return PropagationHelpers.ParseString(headers, (carrier, header) => carrier.GetValues(header), headerName);
        }

        private static void InjectToHeadersCollection(IHeadersCollection carrier, string header, string value)
        {
            carrier.Set(header, value);
        }

        private static IEnumerable<string> ExtractFromHeadersCollection(IHeadersCollection carrier, string header)
        {
            return carrier.GetValues(header);
        }
    }
}
