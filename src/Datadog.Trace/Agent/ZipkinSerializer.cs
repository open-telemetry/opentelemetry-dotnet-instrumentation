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

        public void Serialize(Stream stream, Span[][] traces, TracerSettings settings)
        {
            var zipkinTraces = new List<ZipkinSpan>();

            foreach (var trace in traces)
            {
                foreach (var span in trace)
                {
                    var zspan = new ZipkinSpan(span, settings);
                    zipkinTraces.Add(zspan);
                }
            }

            using (var sw = new StreamWriter(stream, utf8, 4096, true))
            {
                serializer.Serialize(sw, zipkinTraces);
            }
        }

        private static IDictionary<string, string> BuildTags(Span span, TracerSettings settings)
        {
            var tags = new Dictionary<string, string>();

            foreach (var entry in span.Tags.Tags)
            {
                if (!entry.Key.Equals(Trace.Tags.SpanKind))
                {
                    tags[entry.Key] = entry.Value;
                }
            }

            return tags;
        }

        [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
        internal class ZipkinSpan
        {
            private static IDictionary<string, string> emptyTags = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>());

            private readonly Span _span;
            private readonly TracerSettings _settings;
            private readonly IDictionary<string, string> _tags;

            public ZipkinSpan(Span span, TracerSettings settings)
            {
                _span = span;
                _settings = settings;
                _tags = span.Tags != null ? BuildTags(span, settings) : emptyTags;
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
                get => _span.GetTag(Trace.Tags.SpanKind)?.ToUpper();
            }

            public Dictionary<string, string> LocalEndpoint
            {
                get
                {
                    var actualServiceName = !string.IsNullOrWhiteSpace(_span.ServiceName)
                        ? _span.ServiceName
                        : Tracer.Instance.DefaultServiceName;

                    // TODO: Save this allocation
                    return new Dictionary<string, string>() { { "serviceName", actualServiceName } };
                }
            }

            public IDictionary<string, string> Tags
            {
                get => _tags;
            }

            // Methods below are used by Newtonsoft JSON serializer to decide if should serialize
            // some properties when they are null.

            public bool ShouldSerializeTags()
            {
                return _tags != null;
            }

            public bool ShouldSerializeParentId()
            {
                return _span.Context.ParentId != null;
            }

            public bool ShouldSerializeKind()
            {
                return _span.Tags?.GetTag(Trace.Tags.SpanKind) != null;
            }
        }
    }
}
