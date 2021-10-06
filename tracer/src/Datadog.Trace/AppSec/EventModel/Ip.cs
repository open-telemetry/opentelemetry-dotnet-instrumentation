using Datadog.Trace.Vendors.Newtonsoft.Json;

namespace Datadog.Trace.AppSec.EventModel
{
    internal class Ip
    {
        [JsonProperty("address")]
        public string Address { get; set; }
    }
}
