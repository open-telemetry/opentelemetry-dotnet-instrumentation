// <copyright file="ZSpanMock.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

#if NETFRAMEWORK
using IntegrationTests.Helpers.Compatibility;
#endif

namespace IntegrationTests.Helpers.Mocks;

[DebuggerDisplay("TraceId={TraceId}, SpanId={SpanId}, Service={Service}, Name={Name}, Resource={Resource}")]
internal class ZSpanMock : IMockSpan
{
    [JsonExtensionData]
    private Dictionary<string, JToken> _zipkinData;

    public ZSpanMock()
    {
        _zipkinData = new Dictionary<string, JToken>();
    }

    public string TraceId
    {
        get => _zipkinData["traceId"].ToString();
    }

    public ulong SpanId
    {
        get => Convert.ToUInt64(_zipkinData["id"].ToString(), 16);
    }

    public string Name { get; set; }

    public string Resource { get; set; }

    public string Service
    {
        get => _zipkinData["localEndpoint"]["serviceName"].ToString();
    }

    public string Type { get; set; }

    public long Start
    {
        get => Convert.ToInt64(_zipkinData["timestamp"].ToString());
    }

    public long Duration { get; set; }

    public ulong? ParentId
    {
        get
        {
            _zipkinData.TryGetValue("parentId", out JToken parentId);
            return parentId == null ? null : Convert.ToUInt64(parentId.ToString(), 16);
        }
    }

    public byte Error { get; set; }

    public Dictionary<string, string> Tags { get; set; }

    public Dictionary<DateTimeOffset, Dictionary<string, string>> Logs
    {
        get
        {
            var logs = new Dictionary<DateTimeOffset, Dictionary<string, string>>();

            if (_zipkinData.TryGetValue("annotations", out JToken annotations))
            {
                foreach (var item in annotations.ToObject<List<Dictionary<string, object>>>())
                {
                    DateTimeOffset timestamp = ((long)item["timestamp"]).UnixMicrosecondsToDateTimeOffset();
                    Dictionary<string, string> fields = JsonConvert.DeserializeObject<Dictionary<string, string>>(item["value"].ToString());
                    logs[timestamp] = fields;
                }
            }

            return logs;
        }
    }

    public Dictionary<string, double> Metrics { get; set; }

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"TraceId: {TraceId}");
        sb.AppendLine($"ParentId: {ParentId}");
        sb.AppendLine($"SpanId: {SpanId}");
        sb.AppendLine($"Service: {Service}");
        sb.AppendLine($"Name: {Name}");
        sb.AppendLine($"Resource: {Resource}");
        sb.AppendLine($"Type: {Type}");
        sb.AppendLine($"Start: {Start}");
        sb.AppendLine($"Duration: {Duration}");
        sb.AppendLine($"Error: {Error}");
        sb.AppendLine("Tags:");

        if (Tags?.Count > 0)
        {
            foreach (var kv in Tags)
            {
                sb.Append($"\t{kv.Key}:{kv.Value}\n");
            }
        }

        sb.AppendLine("Logs:");
        foreach (var e in Logs)
        {
            sb.Append($"\t{e.Key}:\n");
            foreach (var kv in e.Value)
            {
                sb.Append($"\t\t{kv.Key}:{kv.Value}\n");
            }
        }

        return sb.ToString();
    }

    [OnDeserialized]
    private void OnDeserialized(StreamingContext context)
    {
        var resourceNameTag = Tags.GetValueOrDefault("resource.name");
        // If resource.name tag not set, it matches the operation name
        Resource = string.IsNullOrEmpty(resourceNameTag) ? Name : resourceNameTag;
        Type = Tags.GetValueOrDefault("component");
        var error = Tags.GetValueOrDefault("error") ?? "false";
        Error = (byte)(error.ToLowerInvariant().Equals("true") ? 1 : 0);
        var spanKind = _zipkinData.GetValueOrDefault("kind")?.ToString();
        if (spanKind != null)
        {
            Tags["span.kind"] = spanKind.ToLowerInvariant();
        }
    }
}
