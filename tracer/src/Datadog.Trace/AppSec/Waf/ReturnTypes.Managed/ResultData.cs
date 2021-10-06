using System.Collections.Generic;
using Datadog.Trace.Vendors.Newtonsoft.Json;

namespace Datadog.Trace.AppSec.Waf.ReturnTypes.Managed
{
    internal class ResultData
    {
        [JsonProperty("ret_code")]
        internal int RetCode { get; set; }

        [JsonProperty("flow")]
        internal string Flow { get; set; }

        [JsonProperty("step")]
        internal string Step { get; set; }

        [JsonProperty("rule")]
        internal string Rule { get; set; }

        [JsonProperty("filter")]
        internal List<Filter> Filter { get; set; }
    }
}
