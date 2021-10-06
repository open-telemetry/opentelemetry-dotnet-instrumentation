using Datadog.Trace.Vendors.Newtonsoft.Json;

namespace Datadog.Trace.AppSec.EventModel
{
    internal class Context
    {
        [JsonProperty("actor")]
        public Actor Actor { get; set; }

        [JsonProperty("host")]
        public Host Host { get; set; }

        [JsonProperty("http")]
        public Http Http { get; set; }

        [JsonProperty("service")]
        public Service Service { get; set; }

        [JsonProperty("service_stack")]
        public ServiceStack ServiceStack { get; set; }

        [JsonProperty("span")]
        public Span Span { get; set; }

        [JsonProperty("trace")]
        public Span Trace { get; set; }

        [JsonProperty("tracer")]
        public Tracer Tracer { get; set; }
    }
}
