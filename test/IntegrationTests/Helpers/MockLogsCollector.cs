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
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using OpenTelemetry.Proto.Collector.Logs.V1;
using OpenTelemetry.Proto.Logs.V1;
using Xunit;
using Xunit.Abstractions;

namespace IntegrationTests.Helpers;

public class MockLogsCollector : IDisposable
{
    private static readonly TimeSpan DefaultWaitTimeout = TimeSpan.FromMinutes(1);

    private readonly ITestOutputHelper _output;
    private readonly TestHttpListener _listener;
    private readonly BlockingCollection<LogRecord> _logs = new(100); // bounded to avoid memory leak
    private readonly List<Expectation> _expectations = new();

    private MockLogsCollector(ITestOutputHelper output, string host = "localhost")
    {
        _output = output;
        _listener = new(output, HandleHttpRequests, host, "/v1/logs/");
    }

    /// <summary>
    /// Gets the TCP port that this collector is listening on.
    /// </summary>
    public int Port { get => _listener.Port; }

    public OtlpResourceExpector ResourceExpector { get; } = new();

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
        WriteOutput($"Shutting down. Total logs requests received: '{_logs.Count}'");
        ResourceExpector.Dispose();
        _logs.Dispose();
        _listener.Dispose();
    }

    public void Expect(Func<global::OpenTelemetry.Proto.Logs.V1.LogRecord, bool> predicate, string description = null)
    {
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
        var expectationsMet = new List<global::OpenTelemetry.Proto.Logs.V1.LogRecord>();
        var additionalEntries = new List<global::OpenTelemetry.Proto.Logs.V1.LogRecord>();

        timeout ??= DefaultWaitTimeout;
        var cts = new CancellationTokenSource();

        try
        {
            cts.CancelAfter(timeout.Value);
            foreach (var logRecord in _logs.GetConsumingEnumerable(cts.Token))
            {
                bool found = false;
                for (int i = missingExpectations.Count - 1; i >= 0; i--)
                {
                    if (!missingExpectations[i].Predicate(logRecord))
                    {
                        continue;
                    }

                    expectationsMet.Add(logRecord);
                    missingExpectations.RemoveAt(i);
                    found = true;
                    break;
                }

                if (!found)
                {
                    additionalEntries.Add(logRecord);
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
        List<global::OpenTelemetry.Proto.Logs.V1.LogRecord> expectationsMet,
        List<global::OpenTelemetry.Proto.Logs.V1.LogRecord> additionalEntries)
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

    private void HandleHttpRequests(HttpListenerContext ctx)
    {
        var logsMessage = ExportLogsServiceRequest.Parser.ParseFrom(ctx.Request.InputStream);
        foreach (var resourceLogs in logsMessage.ResourceLogs ?? Enumerable.Empty<ResourceLogs>())
        {
            ResourceExpector.Collect(resourceLogs.Resource);
            foreach (var scopeLogs in resourceLogs.ScopeLogs ?? Enumerable.Empty<ScopeLogs>())
            {
                foreach (var logRecord in scopeLogs.LogRecords ?? Enumerable.Empty<LogRecord>())
                {
                    _logs.Add(logRecord);
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
    }

    private void WriteOutput(string msg)
    {
        const string name = nameof(MockLogsCollector);
        _output.WriteLine($"[{name}]: {msg}");
    }

    private class Expectation
    {
        public Func<LogRecord, bool> Predicate { get; set; }

        public string Description { get; set; }
    }
}
