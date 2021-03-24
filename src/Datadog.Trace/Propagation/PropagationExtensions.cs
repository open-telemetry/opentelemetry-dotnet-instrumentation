using System.Collections.Generic;
using Datadog.Trace.Headers;

namespace Datadog.Trace.Propagation
{
    internal static class PropagationExtensions
    {
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
