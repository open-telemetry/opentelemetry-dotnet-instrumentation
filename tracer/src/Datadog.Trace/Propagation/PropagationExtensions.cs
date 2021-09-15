using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Datadog.Trace.ExtensionMethods;
using Datadog.Trace.Headers;

namespace Datadog.Trace.Propagation
{
    internal static class PropagationExtensions
    {
        internal const string HttpRequestHeadersTagPrefix = "http.request.headers";
        internal const string HttpResponseHeadersTagPrefix = "http.response.headers";

        private static readonly ConcurrentDictionary<Key, string> DefaultTagMappingCache = new ConcurrentDictionary<Key, string>();

        public static void Inject(this IPropagator propagator, SpanContext context, IHeadersCollection headers)
        {
            propagator.Inject(context, headers, InjectToHeadersCollection);
        }

        public static SpanContext Extract(this IPropagator propagator, IHeadersCollection headers)
        {
            return propagator.Extract(headers, ExtractFromHeadersCollection);
        }

        [Obsolete("This method is deprecated and will be removed. Use ExtractHeaderTags<T>(T, IEnumerable<KeyValuePair<string, string>>, string) instead. " +
                  "Kept for backwards compatability where there is a version mismatch between manual and automatic instrumentation")]
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

        public static IEnumerable<KeyValuePair<string, string>> ExtractHeaderTags(this IHeadersCollection headers, IEnumerable<KeyValuePair<string, string>> headerToTagMap, string defaultTagPrefix)
        {
            foreach (KeyValuePair<string, string> headerNameToTagName in headerToTagMap)
            {
                string headerValue = ParseString(headers, headerNameToTagName.Key);
                if (headerValue is null)
                {
                    continue;
                }

                // Tag name is normalized during Tracer instantiation so use as-is
                if (!string.IsNullOrWhiteSpace(headerNameToTagName.Value))
                {
                    yield return new KeyValuePair<string, string>(headerNameToTagName.Value, headerValue);
                }
                else
                {
                    // Since the header name was saved to do the lookup in the input headers,
                    // convert the header to its final tag name once per prefix
                    var cacheKey = new Key(headerNameToTagName.Key, defaultTagPrefix);
                    string tagNameResult = DefaultTagMappingCache.GetOrAdd(cacheKey, key =>
                    {
                        if (key.HeaderName.TryConvertToNormalizedHeaderTagName(out string normalizedHeaderTagName))
                        {
                            return key.TagPrefix + "." + normalizedHeaderTagName;
                        }
                        else
                        {
                            return null;
                        }
                    });

                    if (tagNameResult != null)
                    {
                        yield return new KeyValuePair<string, string>(tagNameResult, headerValue);
                    }
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

        private struct Key : IEquatable<Key>
        {
            public readonly string HeaderName;
            public readonly string TagPrefix;

            public Key(
                string headerName,
                string tagPrefix)
            {
                HeaderName = headerName;
                TagPrefix = tagPrefix;
            }

            /// <summary>
            /// Gets the struct hashcode
            /// </summary>
            /// <returns>Hashcode</returns>
            public override int GetHashCode()
            {
                unchecked
                {
                    return (HeaderName.GetHashCode() * 397) ^ TagPrefix.GetHashCode();
                }
            }

            /// <summary>
            /// Gets if the struct is equal to other object or struct
            /// </summary>
            /// <param name="obj">Object to compare</param>
            /// <returns>True if both are equals; otherwise, false.</returns>
            public override bool Equals(object obj)
            {
                return obj is Key key &&
                       HeaderName == key.HeaderName &&
                       TagPrefix == key.TagPrefix;
            }

            /// <inheritdoc />
            public bool Equals(Key other)
            {
                return HeaderName == other.HeaderName &&
                       TagPrefix == other.TagPrefix;
            }
        }
    }
}
