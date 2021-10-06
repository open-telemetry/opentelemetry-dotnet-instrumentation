using Datadog.Trace.Vendors.Newtonsoft.Json;

namespace Datadog.Trace.AppSec.EventModel
{
    internal class Host
    {
        [JsonProperty("context_version")]
        public string ContextVersion => "0.1.0";

        [JsonProperty("os_type")]
        public string OsType { get; set; }

        [JsonProperty("hostname")]
        public string Hostname { get; set; }
    }
}
