// <copyright file="MockZipkinCollector.cs" company="OpenTelemetry Authors">
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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;

#if NETFRAMEWORK
using System.Net;
using IntegrationTests.Helpers.Compatibility;
#endif

#if NETCOREAPP3_1_OR_GREATER
using Microsoft.AspNetCore.Http;
#endif

namespace IntegrationTests.Helpers;

public class MockZipkinCollector : IDisposable
{
    private static readonly TimeSpan DefaultWaitTimeout = TimeSpan.FromMinutes(1);

    private readonly ITestOutputHelper _output;
    private readonly TestHttpServer _listener;

    private readonly BlockingCollection<ZSpanMock> _spans = new(100); // bounded to avoid memory leak
    private readonly List<Expectation> _expectations = new();

    private MockZipkinCollector(ITestOutputHelper output, string host = "localhost")
    {
        _output = output;
#if NETFRAMEWORK
        _listener = new TestHttpServer(output, HandleHttpRequests, host, "/api/v2/spans/");
#else
        _listener = new TestHttpServer(output, HandleHttpRequests, "/api/v2/spans");
#endif
    }

    /// <summary>
    /// Gets the TCP port that this collector is listening on.
    /// </summary>
    public int Port { get => _listener.Port; }

#if NETFRAMEWORK
    public static async Task<MockZipkinCollector> Start(ITestOutputHelper output, string host = "localhost")
    {
        var collector = new MockZipkinCollector(output, host);

        var healthzResult = await collector._listener.VerifyHealthzAsync();

        if (!healthzResult)
        {
            collector.Dispose();
            throw new InvalidOperationException($"Cannot start {nameof(MockZipkinCollector)}!");
        }

        return collector;
    }
#endif

#if NETCOREAPP3_1_OR_GREATER
    public static Task<MockZipkinCollector> Start(ITestOutputHelper output)
    {
        var collector = new MockZipkinCollector(output);

        return Task.FromResult(collector);
    }
#endif

    public void Dispose()
    {
        WriteOutput("Shutting down.");
        _spans.Dispose();
        _listener.Dispose();
    }

    public void Expect(Func<ZSpanMock, bool> predicate = null, string description = null)
    {
        predicate ??= x => true;
        description ??= "<no description>";

        _expectations.Add(new Expectation { Predicate = predicate, Description = description });
    }

    public void AssertExpectations(TimeSpan? timeout = null)
    {
        if (_expectations.Count == 0)
        {
            throw new InvalidOperationException("Expectations were not set");
        }

        var missingExpectations = new List<Expectation>(_expectations);
        var expectationsMet = new List<ZSpanMock>();
        var additionalEntries = new List<ZSpanMock>();

        timeout ??= DefaultWaitTimeout;
        var cts = new CancellationTokenSource();

        try
        {
            cts.CancelAfter(timeout.Value);
            foreach (var span in _spans.GetConsumingEnumerable(cts.Token))
            {
                var found = false;
                for (var i = missingExpectations.Count - 1; i >= 0; i--)
                {
                    if (!missingExpectations[i].Predicate(span))
                    {
                        continue;
                    }

                    expectationsMet.Add(span);
                    missingExpectations.RemoveAt(i);
                    found = true;
                    break;
                }

                if (!found)
                {
                    additionalEntries.Add(span);
                    continue;
                }

                if (missingExpectations.Count == 0)
                {
                    return;
                }
            }
        }
        catch (ArgumentOutOfRangeException)
        {
            // CancelAfter called with non-positive value
            FailExpectations(missingExpectations, expectationsMet, additionalEntries);
        }
        catch (OperationCanceledException)
        {
            // timeout
            FailExpectations(missingExpectations, expectationsMet, additionalEntries);
        }
    }

    private static void FailExpectations(
        List<Expectation> missingExpectations,
        List<ZSpanMock> expectationsMet,
        List<ZSpanMock> additionalEntries)
    {
        var message = new StringBuilder();
        message.AppendLine();

        message.AppendLine("Missing expectations:");
        foreach (var logline in missingExpectations)
        {
            message.AppendLine($"  - \"{logline.Description}\"");
        }

        message.AppendLine("Entries meeting expectations:");
        foreach (var logline in expectationsMet)
        {
            message.AppendLine($"    \"{logline}\"");
        }

        message.AppendLine("Additional entries:");
        foreach (var logline in additionalEntries)
        {
            message.AppendLine($"  + \"{logline}\"");
        }

        Assert.Fail(message.ToString());
    }

#if NETFRAMEWORK
    private void HandleHttpRequests(HttpListenerContext ctx)
    {
        HandleJsonStream(ctx.Request.InputStream);

        ctx.GenerateEmptyJsonResponse();
    }
#endif

#if NETCOREAPP3_1_OR_GREATER
    private async Task HandleHttpRequests(HttpContext ctx)
    {
        using var bodyStream = await ctx.ReadBodyToMemoryAsync();
        HandleJsonStream(bodyStream);

        await ctx.GenerateEmptyJsonResponseAsync();
    }
#endif

    private void HandleJsonStream(Stream bodyStream)
    {
        using var reader = new StreamReader(bodyStream);
        var zspans = JsonConvert.DeserializeObject<List<ZSpanMock>>(reader.ReadToEnd());
        foreach (var span in zspans ?? Enumerable.Empty<ZSpanMock>())
        {
            _spans.Add(span);
        }
    }

    private void WriteOutput(string msg)
    {
        const string name = nameof(MockZipkinCollector);
        _output.WriteLine($"[{name}]: {msg}");
    }

    [DebuggerDisplay("TraceId={TraceId}, SpanId={SpanId}, Service={Service}, Name={Name}")]
    public class ZSpanMock
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

        public string Service
        {
            get => _zipkinData["localEndpoint"]["serviceName"].ToString();
        }

        public string Library { get; set; }

        public ActivityKind Kind
        {
            get
            {
                if (_zipkinData.TryGetValue("kind", out var value))
                {
                    return (ActivityKind)Enum.Parse(typeof(ActivityKind), value.ToString(), true);
                }

                return ActivityKind.Internal;
            }
        }

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

        public Dictionary<DateTimeOffset, Dictionary<string, object>> Logs
        {
            get
            {
                var logs = new Dictionary<DateTimeOffset, Dictionary<string, object>>();

                if (_zipkinData.TryGetValue("annotations", out JToken annotations))
                {
                    foreach (var item in annotations.ToObject<List<Dictionary<string, object>>>())
                    {
                        DateTimeOffset timestamp = ((long)item["timestamp"]).UnixMicrosecondsToDateTimeOffset();
                        item.Remove("timestamp");
                        logs[timestamp] = item;
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
            sb.AppendLine($"Library: {Library}");
            sb.AppendLine($"Kind: {Kind}");
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
            Library = Tags.GetValueOrDefault("otel.library.name");

            var error = Tags.GetValueOrDefault("error") ?? "false";
            Error = (byte)(error.ToLowerInvariant().Equals("true") ? 1 : 0);

            var spanKind = _zipkinData.GetValueOrDefault("kind")?.ToString();
            if (spanKind != null)
            {
                Tags["span.kind"] = spanKind.ToLowerInvariant();
            }
        }
    }

    private class Expectation
    {
        public Func<ZSpanMock, bool> Predicate { get; set; }

        public string Description { get; set; }
    }
}
