// <copyright file="MockCollector.cs" company="OpenTelemetry Authors">
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
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using Google.Protobuf;
using IntegrationTests.Helpers.Models;
using Opentelemetry.Proto.Collector.Metrics.V1;
using Xunit.Abstractions;

namespace IntegrationTests.Helpers;

public class MockCollector : IDisposable
{
    private static readonly TimeSpan DefaultWaitTimeout = TimeSpan.FromSeconds(20);

    private readonly ITestOutputHelper _output;
    private readonly HttpListener _listener;
    private readonly Thread _listenerThread;

    public MockCollector(ITestOutputHelper output, int port = 4318, int retries = 5, string host = "localhost")
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
                string prefix = new UriBuilder("http", host, port).ToString();
                listener.Prefixes.Add(prefix);

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

    public event EventHandler<EventArgs<ExportMetricsServiceRequest>> RequestDeserialized;

    /// <summary>
    /// Gets or sets a value indicating whether to skip deserialization of metrics.
    /// </summary>
    public bool ShouldDeserializeMetrics { get; set; } = true;

    /// <summary>
    /// Gets the TCP port that this Agent is listening on.
    /// Can be different from <see cref="MockCollector(ITestOutputHelper, int, int)"/>'s <c>initialPort</c>
    /// parameter if listening on that port fails.
    /// </summary>
    public int Port { get; }

    /// <summary>
    /// Gets the filters used to filter out metrics we don't want to look at for a test.
    /// </summary>
    public List<Func<ExportMetricsServiceRequest, bool>> MetricFilters { get; private set; } = new List<Func<ExportMetricsServiceRequest, bool>>();

    public IImmutableList<ExportMetricsServiceRequest> MetricsMessages { get; private set; } = ImmutableList<ExportMetricsServiceRequest>.Empty;

    public IImmutableList<NameValueCollection> RequestHeaders { get; private set; } = ImmutableList<NameValueCollection>.Empty;

    /// <summary>
    /// Wait for the given number of metric requests to appear.
    /// </summary>
    /// <param name="count">The expected number of metric requests.</param>
    /// <param name="timeout">The timeout</param>
    /// <returns>The list of metric requests.</returns>
    public IImmutableList<ExportMetricsServiceRequest> WaitForMetrics(
        int count,
        TimeSpan? timeout = null)
    {
        timeout ??= DefaultWaitTimeout;
        var deadline = DateTime.Now.Add(timeout.Value);

        IImmutableList<ExportMetricsServiceRequest> relevantMetricRequests = ImmutableList<ExportMetricsServiceRequest>.Empty;

        while (DateTime.Now < deadline)
        {
            relevantMetricRequests =
                MetricsMessages
                    .Where(m => MetricFilters.All(shouldReturn => shouldReturn(m)))
                    .ToImmutableList();

            if (relevantMetricRequests.Count >= count)
            {
                break;
            }

            Thread.Sleep(500);
        }

        return relevantMetricRequests;
    }

    public void Dispose()
    {
        WriteOutput($"Shutting down. Total metric requests received: '{MetricsMessages.Count}'");
        _listener?.Stop();
    }

    protected virtual void OnRequestReceived(HttpListenerContext context)
    {
        RequestReceived?.Invoke(this, new EventArgs<HttpListenerContext>(context));
    }

    protected virtual void OnRequestDeserialized(ExportMetricsServiceRequest metricsRequest)
    {
        RequestDeserialized?.Invoke(this, new EventArgs<ExportMetricsServiceRequest>(metricsRequest));
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

                if (ctx.Request.RawUrl.Equals("/v1/metrics", StringComparison.OrdinalIgnoreCase))
                {
                    if (ShouldDeserializeMetrics)
                    {
                        var metricsMessage = ExportMetricsServiceRequest.Parser.ParseFrom(ctx.Request.InputStream);
                        OnRequestDeserialized(metricsMessage);

                        lock (this)
                        {
                            // we only need to lock when replacing the metric collection,
                            // not when reading it because it is immutable
                            MetricsMessages = MetricsMessages.Add(metricsMessage);
                            RequestHeaders = RequestHeaders.Add(new NameValueCollection(ctx.Request.Headers));
                        }
                    }

                    // NOTE: HttpStreamRequest doesn't support Transfer-Encoding: Chunked
                    // (Setting content-length avoids that)
                    ctx.Response.ContentType = "application/x-protobuf";
                    ctx.Response.StatusCode = (int)HttpStatusCode.OK;
                    var responseMessage = new ExportMetricsServiceResponse();
                    ctx.Response.ContentLength64 = responseMessage.CalculateSize();
                    responseMessage.WriteTo(ctx.Response.OutputStream);
                    ctx.Response.Close();
                    continue;
                }

                // We received an unsupported request
                ctx.Response.StatusCode = (int)HttpStatusCode.NotImplemented;
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
        const string name = nameof(MockCollector);

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
