using Datadog.Trace.Vendors.Newtonsoft.Json;

namespace Datadog.Trace.AppSec.EventModel
{
    internal class RuleMatch
    {
        [JsonProperty("operator")]
        public string Operator { get; set; }

        [JsonProperty("operator_value")]
        public string OperatorValue { get; set; }

        [JsonProperty("parameters")]
        public Parameter[] Parameters { get; set; }

        [JsonProperty("highlight")]
        public string[] Highlight { get; set; }

        [JsonProperty("has_server_side_match")]
        public bool HasServerSideMatch { get; set; }
    }
}
