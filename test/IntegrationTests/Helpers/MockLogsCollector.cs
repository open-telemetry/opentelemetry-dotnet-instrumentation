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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using Google.Protobuf;
using OpenTelemetry.Proto.Collector.Logs.V1;
using Xunit;
using Xunit.Abstractions;

namespace IntegrationTests.Helpers;

public class MockLogsCollector : IDisposable
{
    private static readonly TimeSpan DefaultWaitTimeout = TimeSpan.FromMinutes(1);

    private readonly BlockingCollection<string> _logs = new(100); // bounded to avoid memory leak
    private readonly ITestOutputHelper _output;
    private readonly TestHttpListener _listener;

    public MockLogsCollector(ITestOutputHelper output, string host = "localhost")
    {
        _output = output;
        _listener = new(output, HandleHttpRequests, host);
    }

    /// <summary>
    /// Gets the TCP port that this collector is listening on.
    /// </summary>
    public int Port { get => _listener.Port; }

    public bool IsStrict { get; set; }

    public List<string> Expectations { get; set; } = new List<string>();

    public void Dispose()
    {
        _listener.Dispose();
        WriteOutput($"Shutting down. Total logs requests received: '{_logs.Count}'");
        _logs.Dispose();
    }

    public void AssertExpectations(TimeSpan? timeout = null)
    {
        if (Expectations.Count == 0)
        {
            throw new InvalidOperationException("Expectations were not set");
        }

        var missingExpectations = new List<string>(Expectations);
        var expectationsMet = new List<string>();
        var additionalEntries = new List<string>();
        var fail = () =>
        {
            var message = new StringBuilder();

            message.AppendLine("Missing expectations:");
            foreach (var logline in missingExpectations)
            {
                message.AppendLine($"  - \"{logline}\"");
            }

            message.AppendLine("Expectations met:");
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
        };

        timeout ??= DefaultWaitTimeout;
        var cts = new CancellationTokenSource();

        try
        {
            cts.CancelAfter(timeout.Value);
            foreach (var logline in _logs.GetConsumingEnumerable(cts.Token))
            {
                if (missingExpectations.Remove(logline))
                {
                    expectationsMet.Add(logline);
                }
                else
                {
                    additionalEntries.Add(logline);
                }

                if (missingExpectations.Count == 0)
                {
                    if (IsStrict && additionalEntries.Count > 0)
                    {
                        fail();
                    }

                    return;
                }
            }
        }
        catch (ArgumentOutOfRangeException)
        {
            // CancelAfter for negative value
            fail();
        }
        catch (OperationCanceledException)
        {
            // timeout
            fail();
        }
    }

    private void HandleHttpRequests(HttpListenerContext ctx)
    {
        if (ctx.Request.RawUrl.Equals("/healthz", StringComparison.OrdinalIgnoreCase))
        {
            CreateHealthResponse(ctx);
            return;
        }

        if (ctx.Request.RawUrl.Equals("/v1/logs", StringComparison.OrdinalIgnoreCase))
        {
            var logsMessage = ExportLogsServiceRequest.Parser.ParseFrom(ctx.Request.InputStream);
            foreach (var rLogs in logsMessage.ResourceLogs)
            {
                foreach (var sLogs in rLogs.ScopeLogs)
                {
                    foreach (var logRecord in sLogs.LogRecords)
                    {
                        _logs.Add(logRecord.Body.ToString());
                    }
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
