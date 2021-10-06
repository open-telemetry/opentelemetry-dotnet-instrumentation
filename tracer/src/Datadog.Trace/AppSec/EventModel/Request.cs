using System;
using Datadog.Trace.Vendors.Newtonsoft.Json;

namespace Datadog.Trace.AppSec.EventModel
{
    internal class Request
    {
        [JsonProperty("scheme")]
        public string Scheme { get; set; }

        [JsonProperty("method")]
        public string Method { get; set; }

        [JsonProperty("url")]
        public Uri Url { get; set; }

        [JsonProperty("host")]
        public string Host { get; set; }

        [JsonProperty("port")]
        public long Port { get; set; }

        [JsonProperty("path")]
        public string Path { get; set; }

        [JsonProperty("resource")]
        public string Resource { get; set; }

        [JsonProperty("remote_ip")]
        public string RemoteIp { get; set; }

        [JsonProperty("remote_port")]
        public long RemotePort { get; set; }
    }
}
