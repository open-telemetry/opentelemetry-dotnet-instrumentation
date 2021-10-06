using System;
using Datadog.Trace.Vendors.Newtonsoft.Json;

namespace Datadog.Trace.AppSec.EventModel
{
    internal class Service
    {
        [JsonProperty("context_version")]
        public string ContextVersion => "0.1.0";

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("environment")]
        public string Environment { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonPropertyIgnoreNullValue("when")]
        public DateTimeOffset? When { get; set; }
    }
}
