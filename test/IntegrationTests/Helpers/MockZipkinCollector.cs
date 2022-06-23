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
    private readonly HttpListener _listener;
    private readonly Thread _listenerThread;

    public MockZipkinCollector(ITestOutputHelper output, int port = 9080, int retries = 5)
    {
        _output = output;

        // try up to 5 consecutive ports before giving up
        while (true)
        {
            // seems like we can't reuse a listener if it fails to start,
            // so create a new listener each time we retry
            var listener = new HttpListener();

            try
            {
                listener.Start();
                listener.Prefixes.Add($"http://*:{port}/"); // Warning: Requires admin access

                // successfully listening
                Port = port;
                _listener = listener;

                _listenerThread = new Thread(HandleHttpRequests);
                _listenerThread.Start();

                WriteOutput($"Running on port '{Port}'");

                return;
            }
            catch (HttpListenerException) when (retries > 0)
            {
                // only catch the exception if there are retries left
                port++;
                retries--;
            }

            // always close listener if exception is thrown,
            // whether it was caught or not
            listener.Close();

            WriteOutput("Listener shut down. Could not find available port.");
        }
    }

    public event EventHandler<EventArgs<HttpListenerContext>> RequestReceived;

    public event EventHandler<EventArgs<IList<IMockSpan>>> RequestDeserialized;

    /// <summary>
    /// Gets or sets a value indicating whether to skip serialization of traces.
    /// </summary>
    public bool ShouldDeserializeTraces { get; set; } = true;

    /// <summary>
    /// Gets the TCP port that this Agent is listening on.
    /// Can be different from <see cref="MockZipkinCollector(ITestOutputHelper, int, int)"/>'s <c>initialPort</c>
    /// parameter if listening on that port fails.
    /// </summary>
    public int Port { get; }

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
        _listener?.Stop();
    }

    protected virtual void OnRequestReceived(HttpListenerContext context)
    {
        RequestReceived?.Invoke(this, new EventArgs<HttpListenerContext>(context));
    }

    protected virtual void OnRequestDeserialized(IList<IMockSpan> trace)
    {
        RequestDeserialized?.Invoke(this, new EventArgs<IList<IMockSpan>>(trace));
    }

    private void AssertHeader(
        NameValueCollection headers,
        string headerKey,
        Func<string, bool> assertion)
    {
        var header = headers.Get(headerKey);

        if (string.IsNullOrEmpty(header))
        {
            throw new Exception($"Every submission to the agent should have a {headerKey} header.");
        }

        if (!assertion(header))
        {
            throw new Exception($"Failed assertion for {headerKey} on {header}");
        }
    }

    private void HandleHttpRequests()
    {
        while (_listener.IsListening)
        {
            try
            {
                var ctx = _listener.GetContext();
                OnRequestReceived(ctx);

                if (ctx.Request.RawUrl.Equals("/healthz", StringComparison.OrdinalIgnoreCase))
                {
                    CreateHealthResponse(ctx);

                    continue;
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
            catch (HttpListenerException)
            {
                // listener was stopped,
                // ignore to let the loop end and the method return
            }
            catch (ObjectDisposedException)
            {
                // the response has been already disposed.
            }
            catch (InvalidOperationException)
            {
                // this can occur when setting Response.ContentLength64, with the framework claiming that the response has already been submitted
                // for now ignore, and we'll see if this introduces downstream issues
            }
            catch (Exception) when (!_listener.IsListening)
            {
                // we don't care about any exception when listener is stopped
            }
        }
    }

    private void WriteOutput(string msg)
    {
        const string name = nameof(MockZipkinCollector);

        _output.WriteLine($"[{name}]: {msg}");
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
}
