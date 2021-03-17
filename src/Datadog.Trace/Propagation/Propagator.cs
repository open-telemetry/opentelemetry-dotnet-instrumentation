using System;
using System.Collections.Generic;
using System.Globalization;
using Datadog.Trace.Headers;

namespace Datadog.Trace.Propagation
{
    internal abstract class Propagator : IPropagator
    {
        protected static readonly CultureInfo InvariantCulture = CultureInfo.InvariantCulture;

        public abstract void Inject(SpanContext context, IHeadersCollection headers);

        public abstract void Inject<T>(SpanContext context, T carrier, Action<T, string, string> setter);

        public abstract SpanContext Extract(IHeadersCollection headers);

        public abstract SpanContext Extract<T>(T carrier, Func<T, string, IEnumerable<string>> getter);

        public IEnumerable<KeyValuePair<string, string>> ExtractHeaderTags(IHeadersCollection headers, IEnumerable<KeyValuePair<string, string>> headerToTagMap)
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

        protected static string ParseString(IHeadersCollection headers, string headerName)
        {
            var headerValues = headers.GetValues(headerName);

            foreach (string headerValue in headerValues)
            {
                if (!string.IsNullOrEmpty(headerValue))
                {
                    return headerValue;
                }
            }

            return null;
        }
    }
}
