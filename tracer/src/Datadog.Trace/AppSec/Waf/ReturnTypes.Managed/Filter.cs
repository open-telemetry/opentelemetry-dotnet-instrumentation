using System.Collections.Generic;
using Datadog.Trace.Vendors.Newtonsoft.Json;

namespace Datadog.Trace.AppSec.Waf.ReturnTypes.Managed
{
    internal class Filter
    {
        [JsonProperty("operator")]
        internal string Operator { get; set; }

        [JsonProperty("operator_value")]
        internal string OperatorValue { get; set; }

        [JsonProperty("binding_accessor")]
        internal string BindingAccessor { get; set; }

        [JsonProperty("manifest_key")]
        internal string ManifestKey { get; set; }

        [JsonProperty("resolved_value")]
        internal string ResolvedValue { get; set; }

        [JsonProperty("match_status")]
        internal string MatchStatus { get; set; }
    }
}
