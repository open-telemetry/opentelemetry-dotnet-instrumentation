// <copyright file="MockLogsCollector.cs" company="OpenTelemetry Authors">
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
using OpenTelemetry.Proto.Collector.Logs.V1;
using Xunit.Abstractions;

namespace IntegrationTests.Helpers;

public class MockLogsCollector : IDisposable
{
    private static readonly TimeSpan DefaultWaitTimeout = TimeSpan.FromSeconds(20);

    private readonly object _syncRoot = new object();
    private readonly ITestOutputHelper _output;
    private readonly TestHttpListener _listener;

    public MockLogsCollector(ITestOutputHelper output, string host = "localhost")
    {
        _output = output;
        _listener = new(output, HandleHttpRequests, host);
    }

    public event EventHandler<EventArgs<HttpListenerContext>> RequestReceived;

    public event EventHandler<EventArgs<ExportLogsServiceRequest>> RequestDeserialized;

    /// <summary>
    /// Gets or sets a value indicating whether to skip deserialization of logs.
    /// </summary>
    public bool ShouldDeserializeLogs { get; set; } = true;

    /// <summary>
    /// Gets the TCP port that this collector is listening on.
    /// </summary>
    public int Port { get => _listener.Port; }

    /// <summary>
    /// Gets the filters used to filter out logs we don't want to look at for a test.
    /// </summary>
    public List<Func<ExportLogsServiceRequest, bool>> LogFilters { get; private set; } = new List<Func<ExportLogsServiceRequest, bool>>();

    private IImmutableList<ExportLogsServiceRequest> LogMessages { get; set; } = ImmutableList<ExportLogsServiceRequest>.Empty;

    private IImmutableList<NameValueCollection> RequestHeaders { get; set; } = ImmutableList<NameValueCollection>.Empty;

    /// <summary>
    /// Wait for the given number of logs to appear.
    /// </summary>
    /// <param name="count">The expected number of logs.</param>
    /// <param name="timeout">The timeout</param>
    /// <returns>The list of logs.</returns>
    public IImmutableList<ExportLogsServiceRequest> WaitForLogs(
        int count,
        TimeSpan? timeout = null)
    {
        timeout ??= DefaultWaitTimeout;
        var deadline = DateTime.Now.Add(timeout.Value);

        IImmutableList<ExportLogsServiceRequest> relevantLogs = ImmutableList<ExportLogsServiceRequest>.Empty;

        while (DateTime.Now < deadline)
        {
            lock (_syncRoot)
            {
                relevantLogs =
                    LogMessages
                        .Where(m => LogFilters.All(shouldReturn => shouldReturn(m)))
                        .ToImmutableList();
            }

            if (relevantLogs.Count >= count)
            {
                break;
            }

            Thread.Sleep(500);
        }

        return relevantLogs;
    }

    public void Dispose()
    {
        lock (_syncRoot)
        {
            WriteOutput($"Shutting down. Total logs requests received: '{LogMessages.Count}'");
        }

        _listener.Dispose();
    }

    protected virtual void OnRequestReceived(HttpListenerContext context)
    {
        RequestReceived?.Invoke(this, new EventArgs<HttpListenerContext>(context));
    }

    protected virtual void OnRequestDeserialized(ExportLogsServiceRequest logsRequest)
    {
        RequestDeserialized?.Invoke(this, new EventArgs<ExportLogsServiceRequest>(logsRequest));
    }

    private void HandleHttpRequests(HttpListenerContext ctx)
    {
        OnRequestReceived(ctx);

        if (ctx.Request.RawUrl.Equals("/healthz", StringComparison.OrdinalIgnoreCase))
        {
            CreateHealthResponse(ctx);
            return;
        }

        if (ctx.Request.RawUrl.Equals("/v1/logs", StringComparison.OrdinalIgnoreCase))
        {
            if (ShouldDeserializeLogs)
            {
                var logsMessage = ExportLogsServiceRequest.Parser.ParseFrom(ctx.Request.InputStream);
                OnRequestDeserialized(logsMessage);

                lock (_syncRoot)
                {
                    LogMessages = LogMessages.Add(logsMessage);
                    RequestHeaders = RequestHeaders.Add(new NameValueCollection(ctx.Request.Headers));
                }
            }

            // NOTE: HttpStreamRequest doesn't support Transfer-Encoding: Chunked
            // (Setting content-length avoids that)
            ctx.Response.ContentType = "application/x-protobuf";
            ctx.Response.StatusCode = (int)HttpStatusCode.OK;
            var responseMessage = new ExportLogsServiceResponse();
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
        const string name = nameof(MockLogsCollector);
        _output.WriteLine($"[{name}]: {msg}");
    }
}
