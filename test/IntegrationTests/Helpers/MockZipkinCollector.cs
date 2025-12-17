// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.Serialization;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit.Abstractions;

#if NETFRAMEWORK
using System.Net;
using IntegrationTests.Helpers.Compatibility;
#else
using Microsoft.AspNetCore.Http;
#endif

namespace IntegrationTests.Helpers;

internal sealed class MockZipkinCollector : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly TestHttpServer _listener;

    private readonly BlockingCollection<ZSpanMock> _spans = new(100); // bounded to avoid memory leak
    private readonly List<Expectation> _expectations = new();

    public MockZipkinCollector(ITestOutputHelper output, string host = "localhost")
    {
        _output = output;
#if NETFRAMEWORK
        _listener = new TestHttpServer(output, HandleHttpRequests, host, "/api/v2/spans/");
#else
        _listener = new TestHttpServer(output, nameof(MockZipkinCollector), new PathHandler(HandleHttpRequests, "/api/v2/spans"));
#endif
    }

    /// <summary>
    /// Gets the TCP port that this collector is listening on.
    /// </summary>
    public int Port { get => _listener.Port; }

    public void Dispose()
    {
        WriteOutput("Shutting down.");
        _spans.Dispose();
        _listener.Dispose();
    }

    public void Expect(Func<ZSpanMock, bool>? predicate = null, string? description = null)
    {
        predicate ??= x => true;
        description ??= "<no description>";

        _expectations.Add(new Expectation(predicate, description));
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

        timeout ??= TestTimeout.Expectation;
        using var cts = new CancellationTokenSource();

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
            message.AppendLine(CultureInfo.InvariantCulture, $"  - \"{logline.Description}\"");
        }

        message.AppendLine("Entries meeting expectations:");
        foreach (var logline in expectationsMet)
        {
            message.AppendLine(CultureInfo.InvariantCulture, $"    \"{logline}\"");
        }

        message.AppendLine("Additional entries:");
        foreach (var logline in additionalEntries)
        {
            message.AppendLine(CultureInfo.InvariantCulture, $"  + \"{logline}\"");
        }

        Assert.Fail(message.ToString());
    }

#if NETFRAMEWORK
    private void HandleHttpRequests(HttpListenerContext ctx)
    {
        HandleJsonStream(ctx.Request.InputStream);

        ctx.GenerateEmptyJsonResponse();
    }
#else
    private async Task HandleHttpRequests(HttpContext ctx)
    {
        using var bodyStream = await ctx.ReadBodyToMemoryAsync().ConfigureAwait(false);
        HandleJsonStream(bodyStream);

        await ctx.GenerateEmptyJsonResponseAsync().ConfigureAwait(false);
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
    internal sealed class ZSpanMock
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

        public string? Name { get; set; }

        public string? Service
        {
            get => _zipkinData["localEndpoint"]?["serviceName"]?.ToString();
        }

        public string? Library { get; set; }

        public ActivityKind Kind
        {
            get
            {
                if (_zipkinData.TryGetValue("kind", out var value))
                {
#if NET
                    return Enum.Parse<ActivityKind>(value.ToString(), true);
#else
                    return (ActivityKind)Enum.Parse(typeof(ActivityKind), value.ToString(), true);
#endif
                }

                return ActivityKind.Internal;
            }
        }

        public long Start
        {
            get => Convert.ToInt64(_zipkinData["timestamp"].ToString(), CultureInfo.InvariantCulture);
        }

        public long Duration { get; set; }

        public ulong? ParentId
        {
            get
            {
                _zipkinData.TryGetValue("parentId", out var parentId);
                return parentId == null ? null : Convert.ToUInt64(parentId.ToString(), 16);
            }
        }

        public byte Error { get; set; }

        public Dictionary<string, string>? Tags { get; set; }

        public Dictionary<DateTimeOffset, Dictionary<string, object>> Logs
        {
            get
            {
                var logs = new Dictionary<DateTimeOffset, Dictionary<string, object>>();

                if (_zipkinData.TryGetValue("annotations", out var annotations) && annotations != null)
                {
                    var list = annotations.ToObject<List<Dictionary<string, object>>>();
                    if (list != null)
                    {
                        foreach (var item in list)
                        {
                            var timestamp = ((long)item["timestamp"]).UnixMicrosecondsToDateTimeOffset();
                            item.Remove("timestamp");
                            logs[timestamp] = item;
                        }
                    }
                }

                return logs;
            }
        }

        public Dictionary<string, double>? Metrics { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine(CultureInfo.InvariantCulture, $"TraceId: {TraceId}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"ParentId: {ParentId}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"SpanId: {SpanId}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"Service: {Service}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"Name: {Name}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"Library: {Library}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"Kind: {Kind}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"Start: {Start}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"Duration: {Duration}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"Error: {Error}");
            sb.AppendLine("Tags:");

            if (Tags?.Count > 0)
            {
                foreach (var kv in Tags)
                {
                    sb.Append(CultureInfo.InvariantCulture, $"\t{kv.Key}:{kv.Value}\n");
                }
            }

            sb.AppendLine("Logs:");
            foreach (var e in Logs)
            {
                sb.Append(CultureInfo.InvariantCulture, $"\t{e.Key}:\n");
                foreach (var kv in e.Value)
                {
                    sb.Append(CultureInfo.InvariantCulture, $"\t\t{kv.Key}:{kv.Value}\n");
                }
            }

            return sb.ToString();
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            if (Tags == null)
            {
                return;
            }

            Library = Tags.GetValueOrDefault("otel.library.name");

            var error = Tags.GetValueOrDefault("error") ?? "false";
#pragma warning disable CA1308 // Normalize strings to uppercase
            Error = (byte)(error.ToLowerInvariant().Equals("true", StringComparison.Ordinal) ? 1 : 0);
#pragma warning restore CA1308 // Normalize strings to uppercase

            var spanKind = _zipkinData.GetValueOrDefault("kind")?.ToString();
            if (spanKind != null)
            {
#pragma warning disable CA1308 // Normalize strings to uppercase
                Tags["span.kind"] = spanKind.ToLowerInvariant();
#pragma warning restore CA1308 // Normalize strings to uppercase
            }
        }
    }

    private sealed class Expectation
    {
        public Expectation(Func<ZSpanMock, bool> predicate, string? description)
        {
            Predicate = predicate;
            Description = description;
        }

        public Func<ZSpanMock, bool> Predicate { get; }

        public string? Description { get; }
    }
}
