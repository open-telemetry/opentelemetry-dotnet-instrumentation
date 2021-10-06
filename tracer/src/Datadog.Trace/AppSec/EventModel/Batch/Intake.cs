using System.Collections.Generic;
using Datadog.Trace.Vendors.Newtonsoft.Json;

namespace Datadog.Trace.AppSec.EventModel.Batch
{
    internal class Intake : IEvent
    {
        [JsonProperty("protocol_version")]
        internal int ProtocolVersion { get; set; }

        [JsonProperty("idempotency_key")]
        internal string IdemPotencyKey { get; set; }

        [JsonProperty("events")]
        internal IEnumerable<IEvent> Events { get; set; }
    }
}
