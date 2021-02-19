using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using Datadog.Trace.Configuration;
using Datadog.Trace.ExtensionMethods;
using Datadog.Trace.Vendors.Newtonsoft.Json;
using Datadog.Trace.Vendors.Newtonsoft.Json.Serialization;

namespace Datadog.Trace.Agent
{
    internal class ZipkinSerializer
    {
        private readonly JsonSerializer serializer = new JsonSerializer();

        // Don't serialize with BOM
        private readonly Encoding utf8 = new UTF8Encoding(false);

        public void Serialize(Stream stream, Span[][] traces)
        {
            var zipkinTraces = new List<ZipkinSpan>();

            foreach (var trace in traces)
            {
                foreach (var span in trace)
                {
                    var zspan = new ZipkinSpan(span);
                    zipkinTraces.Add(zspan);
                }
            }

            using (var sw = new StreamWriter(stream, utf8, 4096, true))
            {
                serializer.Serialize(sw, zipkinTraces);
            }
        }

        [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy), ItemNullValueHandling = NullValueHandling.Ignore)]
        internal class ZipkinSpan
        {
            private static readonly IDictionary<string, string> EmptyTags = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>());

            private readonly Span _span;
            private readonly IDictionary<string, string> _tags;

            public ZipkinSpan(Span span)
            {
                _span = span;
                _tags = BuildTags(span);
            }

            public string Id
            {
                get => _span.Context.SpanId.ToString("x16");
            }

            public string TraceId
            {
                get => _span.Context.TraceId.ToString("x16");
            }

            public string ParentId
            {
                get => _span.Context.ParentId?.ToString("x16");
            }

            public string Name
            {
                get => _span.OperationName;
            }

            public long Timestamp
            {
                get => _span.StartTime.ToUnixTimeMicroseconds();
            }

            public long Duration
            {
                get => _span.Duration.ToMicroseconds();
            }

            public string Kind
            {
                // Per Zipkin convention these are always upper case.
                get => _span.GetTag(Trace.Tags.SpanKind)?.ToUpperInvariant();
            }

            public Dictionary<string, string> LocalEndpoint
            {
                get
                {
                    var actualServiceName = !string.IsNullOrWhiteSpace(_span.ServiceName)
                        ? _span.ServiceName
                        : Tracer.Instance.DefaultServiceName;

                    // TODO: Save this allocation.
                    return new Dictionary<string, string>() { { "serviceName", actualServiceName } };
                }
            }

            public IDictionary<string, string> Tags
            {
                get => _tags;
            }

            private static IDictionary<string, string> BuildTags(Span span)
            {
                var spanTags = span?.Tags?.Tags;
                if (spanTags == null || spanTags.Count == 0)
                {
                    return EmptyTags;
                }

                var tags = new Dictionary<string, string>(spanTags.Count);
                foreach (var entry in spanTags)
                {
                    if (!entry.Key.Equals(Trace.Tags.SpanKind))
                    {
                        tags[entry.Key] = entry.Value;
                    }
                }

                return tags;
            }
        }
    }
}
