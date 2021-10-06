using Datadog.Trace.Vendors.Newtonsoft.Json;

namespace Datadog.Trace.AppSec.EventModel
{
    internal class Response
    {
        [JsonProperty("status")]
        public long Status { get; set; }

        [JsonProperty("blocked")]
        public bool Blocked { get; set; }
    }
}
