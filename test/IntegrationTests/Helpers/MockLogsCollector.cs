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
using System.Threading.Tasks;
using Google.Protobuf;
using OpenTelemetry.Proto.Collector.Logs.V1;
using Xunit;
using Xunit.Abstractions;

namespace IntegrationTests.Helpers;

public class MockLogsCollector : IDisposable
{
    private static readonly TimeSpan DefaultWaitTimeout = TimeSpan.FromMinutes(1);

    private readonly ITestOutputHelper _output;
    private readonly TestHttpListener _listener;
    private readonly BlockingCollection<global::OpenTelemetry.Proto.Logs.V1.LogRecord> _logs = new(100); // bounded to avoid memory leak
    private readonly List<Expectation> _expectations = new();

    private MockLogsCollector(ITestOutputHelper output, string host = "localhost")
    {
        _output = output;
        _listener = new(output, HandleHttpRequests, host);
    }

    /// <summary>
    /// Gets the TCP port that this collector is listening on.
    /// </summary>
    public int Port { get => _listener.Port; }

    /// <summary>
    /// IsStrict defines if all entries must be expected.
    /// </summary>
    public bool IsStrict { get; set; }

    public static async Task<MockLogsCollector> Start(ITestOutputHelper output, string host = "localhost")
    {
        var collector = new MockLogsCollector(output, host);

        var healthzResult = await collector._listener.VerifyHealthzAsync();

        if (!healthzResult)
        {
            collector.Dispose();
            throw new InvalidOperationException($"Cannot start {nameof(MockLogsCollector)}!");
        }

        return collector;
    }

    public void Dispose()
    {
        _listener.Dispose();
        WriteOutput($"Shutting down. Total logs requests received: '{_logs.Count}'");
        _logs.Dispose();
    }

    public void Expect(Func<global::OpenTelemetry.Proto.Logs.V1.LogRecord, bool> predicate, string description = null)
    {
        _expectations.Add(new Expectation { Predicate = predicate, Description = description });
    }

    public void AssertExpectations(TimeSpan? timeout = null)
    {
        if (_expectations.Count == 0)
        {
            throw new InvalidOperationException("Expectations were not set");
        }

        var missingExpectations = new List<Expectation>(_expectations);
        var expectationsMet = new List<global::OpenTelemetry.Proto.Logs.V1.LogRecord>();
        var additionalEntries = new List<global::OpenTelemetry.Proto.Logs.V1.LogRecord>();
        var fail = () =>
        {
            var message = new StringBuilder();
            message.AppendLine();

            message.AppendLine("Missing expectations:");
            foreach (var logline in missingExpectations)
            {
                message.AppendLine($"  - \"{logline.Description ?? "<no description>"}\"");
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
        };

        timeout ??= DefaultWaitTimeout;
        var cts = new CancellationTokenSource();

        try
        {
            cts.CancelAfter(timeout.Value);
            foreach (var logRecord in _logs.GetConsumingEnumerable(cts.Token))
            {
                bool found = false;
                for (int i = 0; i < missingExpectations.Count; i++)
                {
                    if (!missingExpectations[i].Predicate(logRecord))
                    {
                        continue;
                    }

                    expectationsMet.Add(logRecord);
                    missingExpectations.RemoveAt(i);
                    found = true;
                }

                if (!found)
                {
                    additionalEntries.Add(logRecord);
                    continue;
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
            // CancelAfter called with non-positive value
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
        if (ctx.Request.RawUrl.Equals("/v1/logs", StringComparison.OrdinalIgnoreCase))
        {
            var logsMessage = ExportLogsServiceRequest.Parser.ParseFrom(ctx.Request.InputStream);
            if (logsMessage.ResourceLogs != null)
            {
                foreach (var rLogs in logsMessage.ResourceLogs)
                {
                    if (rLogs.ScopeLogs != null)
                    {
                        foreach (var sLogs in rLogs.ScopeLogs)
                        {
                            if (sLogs.LogRecords != null)
                            {
                                foreach (var logRecord in sLogs.LogRecords)
                                {
                                    _logs.Add(logRecord);
                                }
                            }
                        }
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

    private void WriteOutput(string msg)
    {
        const string name = nameof(MockLogsCollector);
        _output.WriteLine($"[{name}]: {msg}");
    }

    private class Expectation
    {
        public Func<global::OpenTelemetry.Proto.Logs.V1.LogRecord, bool> Predicate { get; set; }

        public string Description { get; set; }
    }
}
