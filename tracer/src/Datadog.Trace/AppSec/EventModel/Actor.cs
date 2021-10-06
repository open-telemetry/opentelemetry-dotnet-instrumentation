using System;
using Datadog.Trace.Vendors.Newtonsoft.Json;

namespace Datadog.Trace.AppSec.EventModel
{
    internal class Actor
    {
        [JsonProperty("context_version")]
        public string ContextVersion => "0.1.0";

        [JsonProperty("ip")]
        public Ip Ip { get; set; }

        [JsonProperty("identifiers")]
        public Identifiers Identifiers { get; set; }

        [JsonProperty("_id")]
        public string Id { get; set; }
    }
}
