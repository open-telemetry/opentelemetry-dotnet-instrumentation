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
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using IntegrationTests.Helpers.Mocks;
using Newtonsoft.Json;
using Xunit.Abstractions;

#if NETFRAMEWORK
using System.Net;
using System.Text;
#else
using Microsoft.AspNetCore.Http;
#endif

namespace IntegrationTests.Helpers;

public class MockZipkinCollector : IDisposable
{
    protected static readonly TimeSpan DefaultSpanWaitTimeout = TimeSpan.FromMinutes(1);

    private readonly object _syncRoot = new object();
    private readonly ITestOutputHelper _output;
    private readonly TestHttpServer _listener;

    protected MockZipkinCollector(ITestOutputHelper output, string host)
    {
        _output = output;

#if NETFRAMEWORK
        _listener = new(output, HandleHttpRequests, host, "/api/v2/spans");
#else
        _listener = new(output, HandleHttpRequests, "/api/v2/spans");
#endif
    }

    /// <summary>
    /// Gets or sets a value indicating whether to skip serialization of traces.
    /// </summary>
    public bool ShouldDeserializeTraces { get; set; } = true;

    /// <summary>
    /// Gets the TCP port that this collector is listening on.
    /// </summary>
    public int Port => _listener.Port;

    /// <summary>
    /// Gets the filters used to filter out spans we don't want to look at for a test.
    /// </summary>
    public List<Func<IMockSpan, bool>> SpanFilters { get; private set; } = new List<Func<IMockSpan, bool>>();

    protected IImmutableList<IMockSpan> Spans { get; set; } = ImmutableList<IMockSpan>.Empty;

    protected IImmutableList<NameValueCollection> RequestHeaders { get; set; } = ImmutableList<NameValueCollection>.Empty;

    /// <summary>
    /// Gets the TCP port that this collector is listening on.
    /// </summary>
    /// <param name="output">Test output</param>
    /// <param name="host">Server host</param>
    /// <returns>representing the asynchronous Start operation</returns>
    public static async Task<MockZipkinCollector> Start(ITestOutputHelper output, string host = "localhost")
    {
        var collector = new MockZipkinCollector(output, host);

#if NETFRAMEWORK
        var healthzResult = await collector._listener.VerifyHealthzAsync();

        if (!healthzResult)
        {
            collector.Dispose();
            throw new InvalidOperationException($"Cannot start {nameof(MockLogsCollector)}!");
        }

        return collector;
#else
        return await Task.FromResult(collector);
#endif
    }

    /// <summary>
    /// Wait for the given number of spans to appear.
    /// </summary>
    /// <param name="count">The expected number of spans.</param>
    /// <param name="timeout">The timeout</param>
    /// <param name="instrumentationLibrary">The integration we're testing</param>
    /// <param name="minDateTime">Minimum time to check for spans from</param>
    /// <param name="returnAllOperations">When true, returns every span regardless of operation name</param>
    /// <returns>The list of spans.</returns>
    public async Task<IImmutableList<IMockSpan>> WaitForSpansAsync(
        int count,
        TimeSpan? timeout = null,
        string instrumentationLibrary = null,
        DateTimeOffset? minDateTime = null,
        bool returnAllOperations = false)
    {
        timeout ??= DefaultSpanWaitTimeout;
        var deadline = DateTime.Now.Add(timeout.Value);
        var minimumOffset = (minDateTime ?? DateTimeOffset.MinValue).ToUnixTimeNanoseconds();

        IImmutableList<IMockSpan> relevantSpans = ImmutableList<IMockSpan>.Empty;

        // Use a do-while to ensure at least one attempt at reading the data already
        // received. This is helpful for negative tests, ie.: no spans generated, when
        // the process emitting the spans already finished.
        do
        {
            lock (_syncRoot)
            {
                relevantSpans =
                    Spans
                        .Where(s => SpanFilters.All(shouldReturn => shouldReturn(s)))
                        .Where(s => s.Start > minimumOffset)
                        .ToImmutableList();
            }

            if (relevantSpans.Count(s => instrumentationLibrary == null || s.Library == instrumentationLibrary) >= count)
            {
                break;
            }

            await Task.Delay(500);
        }
        while (DateTime.Now < deadline);

        if (!returnAllOperations)
        {
            relevantSpans =
                relevantSpans
                    .Where(s => instrumentationLibrary == null || s.Library == instrumentationLibrary)
                    .ToImmutableList();
        }

        return relevantSpans;
    }

    public void Dispose()
    {
        lock (_syncRoot)
        {
            WriteOutput($"Shutting down. Total spans received: '{Spans.Count}'");
        }

        _listener.Dispose();
    }

#if NETFRAMEWORK
    private void HandleHttpRequests(HttpListenerContext ctx)
    {
        if (ShouldDeserializeTraces)
        {
            using (var reader = new StreamReader(ctx.Request.InputStream))
            {
                var json = reader.ReadToEnd();
                var headers = new NameValueCollection(ctx.Request.Headers);

                Deserialize(json, headers);
            }
        }

        // NOTE: HttpStreamRequest doesn't support Transfer-Encoding: Chunked
        // (Setting content-length avoids that)

        ctx.Response.ContentType = "application/json";
        var buffer = Encoding.UTF8.GetBytes("{}");
        ctx.Response.ContentLength64 = buffer.LongLength;
        ctx.Response.OutputStream.Write(buffer, 0, buffer.Length);
        ctx.Response.Close();
    }
#else
    private async Task HandleHttpRequests(HttpContext ctx)
    {
        if (ShouldDeserializeTraces)
        {
            if (!ctx.Request.Body.CanSeek)
            {
                // We only do this if the stream isn't *already* seekable,
                // as EnableBuffering will create a new stream instance
                // each time it's called
                ctx.Request.EnableBuffering();
            }

            ctx.Request.Body.Position = 0;

            using (var reader = new StreamReader(ctx.Request.Body))
            {
                var json = await reader.ReadToEndAsync();
                var headers = ctx.Request.Headers.Aggregate(new NameValueCollection(), (seed, current) =>
                {
                    seed.Add(current.Key, current.Value);
                    return seed;
                });

                Deserialize(json, headers);
            }
        }

        ctx.Response.ContentType = "application/json";
        await ctx.Response.WriteAsync("{}");
    }
#endif

    private void Deserialize(string json, NameValueCollection headers)
    {
        var zspans = JsonConvert.DeserializeObject<List<ZSpanMock>>(json);
        if (zspans != null)
        {
            IList<IMockSpan> spans = zspans.ConvertAll(x => (IMockSpan)x);

            lock (_syncRoot)
            {
                Spans = Spans.AddRange(spans);
                RequestHeaders = RequestHeaders.Add(headers);
            }
        }
    }

    private void WriteOutput(string msg)
    {
        const string name = nameof(MockZipkinCollector);
        _output.WriteLine($"[{name}]: {msg}");
    }
}
