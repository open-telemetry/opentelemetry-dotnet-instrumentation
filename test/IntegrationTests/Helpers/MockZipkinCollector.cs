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
using System.Net;
using System.Text;
using System.Threading;
using IntegrationTests.Helpers.Mocks;
using IntegrationTests.Helpers.Models;
using Newtonsoft.Json;
using Xunit.Abstractions;

namespace IntegrationTests.Helpers;

public class MockZipkinCollector : IDisposable
{
    private static readonly TimeSpan DefaultSpanWaitTimeout = TimeSpan.FromSeconds(20);

    private readonly ITestOutputHelper _output;
    private readonly TestHttpListener _listener;

    public MockZipkinCollector(ITestOutputHelper output, string host = "localhost")
    {
        _output = output;
        _listener = new(output, HandleHttpRequests, host, "/api/v2/spans/");
    }

    public event EventHandler<EventArgs<HttpListenerContext>> RequestReceived;

    public event EventHandler<EventArgs<IList<IMockSpan>>> RequestDeserialized;

    /// <summary>
    /// Gets or sets a value indicating whether to skip serialization of traces.
    /// </summary>
    public bool ShouldDeserializeTraces { get; set; } = true;

    /// <summary>
    /// Gets the TCP port that this collector is listening on.
    /// </summary>
    public int Port { get => _listener.Port; }

    /// <summary>
    /// Gets the filters used to filter out spans we don't want to look at for a test.
    /// </summary>
    public List<Func<IMockSpan, bool>> SpanFilters { get; private set; } = new List<Func<IMockSpan, bool>>();

    public IImmutableList<IMockSpan> Spans { get; private set; } = ImmutableList<IMockSpan>.Empty;

    public IImmutableList<NameValueCollection> RequestHeaders { get; private set; } = ImmutableList<NameValueCollection>.Empty;

    /// <summary>
    /// Wait for the given number of spans to appear.
    /// </summary>
    /// <param name="count">The expected number of spans.</param>
    /// <param name="timeout">The timeout</param>
    /// <param name="operationName">The integration we're testing</param>
    /// <param name="minDateTime">Minimum time to check for spans from</param>
    /// <param name="returnAllOperations">When true, returns every span regardless of operation name</param>
    /// <returns>The list of spans.</returns>
    public IImmutableList<IMockSpan> WaitForSpans(
        int count,
        TimeSpan? timeout = null,
        string operationName = null,
        DateTimeOffset? minDateTime = null,
        bool returnAllOperations = false)
    {
        timeout ??= DefaultSpanWaitTimeout;
        var deadline = DateTime.Now.Add(timeout.Value);
        var minimumOffset = (minDateTime ?? DateTimeOffset.MinValue).ToUnixTimeNanoseconds();

        IImmutableList<IMockSpan> relevantSpans = ImmutableList<IMockSpan>.Empty;

        while (DateTime.Now < deadline)
        {
            relevantSpans =
                Spans
                    .Where(s => SpanFilters.All(shouldReturn => shouldReturn(s)))
                    .Where(s => s.Start > minimumOffset)
                    .ToImmutableList();

            if (relevantSpans.Count(s => operationName == null || s.Name == operationName) >= count)
            {
                break;
            }

            Thread.Sleep(500);
        }

        if (!returnAllOperations)
        {
            relevantSpans =
                relevantSpans
                    .Where(s => operationName == null || s.Name == operationName)
                    .ToImmutableList();
        }

        return relevantSpans;
    }

    public void Dispose()
    {
        WriteOutput($"Shutting down. Total spans received: '{Spans.Count}'");
        _listener.Dispose();
    }

    protected virtual void OnRequestReceived(HttpListenerContext context)
    {
        RequestReceived?.Invoke(this, new EventArgs<HttpListenerContext>(context));
    }

    protected virtual void OnRequestDeserialized(IList<IMockSpan> trace)
    {
        RequestDeserialized?.Invoke(this, new EventArgs<IList<IMockSpan>>(trace));
    }

    private void HandleHttpRequests(HttpListenerContext ctx)
    {
        if (ctx.Request.RawUrl.Equals("/healthz", StringComparison.OrdinalIgnoreCase))
        {
            CreateHealthResponse(ctx);
            return;
        }

        if (ShouldDeserializeTraces)
        {
            using (var reader = new StreamReader(ctx.Request.InputStream))
            {
                var zspans = JsonConvert.DeserializeObject<List<ZSpanMock>>(reader.ReadToEnd());
                if (zspans != null)
                {
                    IList<IMockSpan> spans = zspans.ConvertAll(x => (IMockSpan)x);
                    OnRequestDeserialized(spans);

                    lock (this)
                    {
                        // we only need to lock when replacing the span collection,
                        // not when reading it because it is immutable
                        Spans = Spans.AddRange(spans);
                        RequestHeaders = RequestHeaders.Add(new NameValueCollection(ctx.Request.Headers));
                    }
                }
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

    private void CreateHealthResponse(HttpListenerContext ctx)
    {
        ctx.Response.ContentType = "text/plain";
        var buffer = Encoding.UTF8.GetBytes("OK");
        ctx.Response.ContentLength64 = buffer.LongLength;
        ctx.Response.OutputStream.Write(buffer, 0, buffer.Length);
        ctx.Response.StatusCode = (int)HttpStatusCode.OK;
        ctx.Response.Close();
    }

    private void WriteOutput(string msg)
    {
        const string name = nameof(MockZipkinCollector);
        _output.WriteLine($"[{name}]: {msg}");
    }
}
