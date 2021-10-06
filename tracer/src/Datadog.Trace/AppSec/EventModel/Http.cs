using Datadog.Trace.Vendors.Newtonsoft.Json;

namespace Datadog.Trace.AppSec.EventModel
{
    internal class Http
    {
        [JsonProperty("context_version")]
        public string ContextVersion => "0.1.0";

        [JsonProperty("request")]
        public Request Request { get; set; }

        [JsonProperty("response")]
        public Response Response { get; set; }
    }
}
