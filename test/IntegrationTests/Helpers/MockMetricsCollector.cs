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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using Google.Protobuf.Collections;
using OpenTelemetry.Proto.Collector.Metrics.V1;
using OpenTelemetry.Proto.Common.V1;
using Xunit;
using Xunit.Abstractions;

namespace IntegrationTests.Helpers;

public class MockMetricsCollector : IDisposable
{
    private static readonly TimeSpan DefaultWaitTimeout = TimeSpan.FromMinutes(1);

    private readonly ITestOutputHelper _output;
    private readonly TestHttpListener _listener;
    private readonly BlockingCollection<global::OpenTelemetry.Proto.Metrics.V1.ResourceMetrics> _metrics = new(10); // bounded to avoid memory leak
    private readonly List<Expectation> _expectations = new();
    private readonly List<ResourceExpectation> _resourceExpectations = new();

    private MockMetricsCollector(ITestOutputHelper output, string host = "localhost")
    {
        _output = output;
        _listener = new(output, HandleHttpRequests, host);
    }

    /// <summary>
    /// Gets the TCP port that this collector is listening on.
    /// </summary>
    public int Port { get => _listener.Port; }

    public static async Task<MockMetricsCollector> Start(ITestOutputHelper output, string host = "localhost")
    {
        var collector = new MockMetricsCollector(output, host);

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
        WriteOutput($"Shutting down.");
        _metrics.Dispose();
        _listener.Dispose();
    }

    public void Expect(string instrumentationScopeName, Func<global::OpenTelemetry.Proto.Metrics.V1.Metric, bool> predicate = null, string description = null)
    {
        predicate ??= x => true;
        description ??= instrumentationScopeName;

        _expectations.Add(new Expectation { InstrumentationScopeName = instrumentationScopeName, Predicate = predicate, Description = description });
    }

    public void AssertExpectations(TimeSpan? timeout = null)
    {
        if (_expectations.Count == 0)
        {
            throw new InvalidOperationException("Expectations were not set");
        }

        if (_resourceExpectations.Count > 0)
        {
            throw new InvalidOperationException("Currently you can only assert for metrics or resouce attributes");
        }

        var missingExpectations = new List<Expectation>(_expectations);
        var expectationsMet = new List<Collected>();
        var additionalEntries = new List<Collected>();

        timeout ??= DefaultWaitTimeout;
        var cts = new CancellationTokenSource();

        try
        {
            cts.CancelAfter(timeout.Value);

            // loop until expectations met or timeout
            while (true)
            {
                var resourceMetrics = _metrics.Take(cts.Token); // get the metrics snapshot

                missingExpectations = new List<Expectation>(_expectations);
                expectationsMet = new List<Collected>();
                additionalEntries = new List<Collected>();

                foreach (var scopeMetrics in resourceMetrics.ScopeMetrics)
                {
                    foreach (var metric in scopeMetrics.Metrics)
                    {
                        var colleted = new Collected
                        {
                            InstrumentationScopeName = scopeMetrics.Scope.Name,
                            Metric = metric
                        };

                        bool found = false;
                        for (int i = missingExpectations.Count - 1; i >= 0; i--)
                        {
                            if (colleted.InstrumentationScopeName != missingExpectations[i].InstrumentationScopeName)
                            {
                                continue;
                            }

                            if (!missingExpectations[i].Predicate(colleted.Metric))
                            {
                                continue;
                            }

                            expectationsMet.Add(colleted);
                            missingExpectations.RemoveAt(i);
                            found = true;
                            break;
                        }

                        if (!found)
                        {
                            additionalEntries.Add(colleted);
                        }
                    }
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

    public void ExpectResourceAttribute(string key, string value)
    {
        _resourceExpectations.Add(new ResourceExpectation { Key = key, Value = value });
    }

    public void AssertResourceExpectations(TimeSpan? timeout = null)
    {
        if (_resourceExpectations.Count == 0)
        {
            throw new InvalidOperationException("Expectations were not set");
        }

        if (_expectations.Count > 0)
        {
            throw new InvalidOperationException("Currently you can only assert for metrics or resouce attributes");
        }

        var missingExpectations = new List<ResourceExpectation>(_resourceExpectations);

        timeout ??= DefaultWaitTimeout;
        var cts = new CancellationTokenSource();

        try
        {
            cts.CancelAfter(timeout.Value);
            var resourceMetrics = _metrics.Take(cts.Token); // get the metrics snapshot

            foreach (var resourceAttribute in resourceMetrics.Resource.Attributes)
            {
                for (int i = missingExpectations.Count - 1; i >= 0; i--)
                {
                    if (resourceAttribute.Key != missingExpectations[i].Key)
                    {
                        continue;
                    }

                    if (resourceAttribute.Value.StringValue != missingExpectations[i].Value)
                    {
                        continue;
                    }

                    missingExpectations.RemoveAt(i);
                    break;
                }
            }

            if (missingExpectations.Count > 0)
            {
                FailResourceExpectations(missingExpectations, resourceMetrics.Resource.Attributes);
            }
        }
        catch (ArgumentOutOfRangeException)
        {
            // CancelAfter called with non-positive value
            FailResourceExpectations(missingExpectations, null);
        }
        catch (OperationCanceledException)
        {
            // timeout
            FailResourceExpectations(missingExpectations, null);
        }
    }

    private static void FailExpectations(
        List<Expectation> missingExpectations,
        List<Collected> expectationsMet,
        List<Collected> additionalEntries)
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

    private void FailResourceExpectations(List<ResourceExpectation> missingExpectations, RepeatedField<KeyValue> attributes)
    {
        attributes ??= new();

        var message = new StringBuilder();
        message.AppendLine();

        message.AppendLine("Missing resource expectations:");
        foreach (var expectation in missingExpectations)
        {
            message.AppendLine($"  - \"{expectation.Key}={expectation.Value}\"");
        }

        message.AppendLine("Actual resource attributes:");
        foreach (var attribute in attributes)
        {
            message.AppendLine($"  + \"{attribute.Key}={attribute.Value.StringValue}\"");
        }

        Assert.Fail(message.ToString());
    }

    private void HandleHttpRequests(HttpListenerContext ctx)
    {
        if (ctx.Request.RawUrl.Equals("/v1/metrics", StringComparison.OrdinalIgnoreCase))
        {
            var metricsMessage = ExportMetricsServiceRequest.Parser.ParseFrom(ctx.Request.InputStream);
            if (metricsMessage.ResourceMetrics != null)
            {
                foreach (var metrics in metricsMessage.ResourceMetrics)
                {
                    _metrics.Add(metrics);
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

    private void WriteOutput(string msg)
    {
        const string name = nameof(MockMetricsCollector);
        _output.WriteLine($"[{name}]: {msg}");
    }

    private class Expectation
    {
        public string InstrumentationScopeName { get; set; }

        public Func<global::OpenTelemetry.Proto.Metrics.V1.Metric, bool> Predicate { get; set; }

        public string Description { get; set; }
    }

    private class ResourceExpectation
    {
        public string Key { get; set; }

        public string Value { get; set; }
    }

    private class Collected
    {
        public string InstrumentationScopeName { get; set; }

        public global::OpenTelemetry.Proto.Metrics.V1.Metric Metric { get; set; }

        public override string ToString()
        {
            return $"InstrumentationScopeName = {InstrumentationScopeName}, Metric = {Metric}";
        }
    }
}
