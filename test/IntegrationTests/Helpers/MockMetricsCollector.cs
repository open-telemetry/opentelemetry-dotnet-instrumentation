// <copyright file="MockMetricsCollector.cs" company="OpenTelemetry Authors">
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

public class MockMetricsCollector : IDisposable
{
    private static readonly TimeSpan DefaultWaitTimeout = TimeSpan.FromSeconds(20);

    private readonly ITestOutputHelper _output;
    private readonly TestHttpListener _listener;

    public MockMetricsCollector(ITestOutputHelper output)
    {
        _output = output;
        _listener = new(output, HandleHttpRequests);
    }

    public event EventHandler<EventArgs<HttpListenerContext>> RequestReceived;

    public event EventHandler<EventArgs<ExportMetricsServiceRequest>> RequestDeserialized;

    /// <summary>
    /// Gets or sets a value indicating whether to skip deserialization of metrics.
    /// </summary>
    public bool ShouldDeserializeMetrics { get; set; } = true;

    /// <summary>
    /// Gets the TCP port that this collector is listening on.
    /// </summary>
    public int Port { get => _listener.Port; }

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
        _listener.Dispose();
    }

    protected virtual void OnRequestReceived(HttpListenerContext context)
    {
        RequestReceived?.Invoke(this, new EventArgs<HttpListenerContext>(context));
    }

    protected virtual void OnRequestDeserialized(ExportMetricsServiceRequest metricsRequest)
    {
        RequestDeserialized?.Invoke(this, new EventArgs<ExportMetricsServiceRequest>(metricsRequest));
    }

    private void HandleHttpRequests(HttpListenerContext ctx)
    {
        OnRequestReceived(ctx);

        if (ctx.Request.RawUrl.Equals("/healthz", StringComparison.OrdinalIgnoreCase))
        {
            CreateHealthResponse(ctx);
            return;
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
            return;
        }

        // We received an unsupported request
        ctx.Response.StatusCode = (int)HttpStatusCode.NotImplemented;
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
        const string name = nameof(MockMetricsCollector);
        _output.WriteLine($"[{name}]: {msg}");
    }
}
