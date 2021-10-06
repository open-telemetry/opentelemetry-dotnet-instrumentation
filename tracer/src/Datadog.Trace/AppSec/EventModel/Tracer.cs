using Datadog.Trace.Vendors.Newtonsoft.Json;

namespace Datadog.Trace.AppSec.EventModel
{
    internal class Tracer
    {
        [JsonProperty("context_version")]
        public string ContextVersion => "0.1.0";

        [JsonProperty("runtime_type")]
        public string RuntimeType { get; set; }

        [JsonProperty("runtime_version")]
        public string RuntimeVersion { get; set; }

        [JsonProperty("lib_version")]
        public string LibVersion { get; set; }
    }
}
