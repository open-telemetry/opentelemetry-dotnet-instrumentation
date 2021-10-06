using Datadog.Trace.Vendors.Newtonsoft.Json;

namespace Datadog.Trace.AppSec.EventModel
{
    internal class Identifiers
    {
        [JsonProperty("sqreen_id")]
        public string SqreenId { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }
    }
}
