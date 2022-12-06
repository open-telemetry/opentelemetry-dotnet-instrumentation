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
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OpenTelemetry.Proto.Collector.Metrics.V1;
using OpenTelemetry.Proto.Metrics.V1;
using Xunit;
using Xunit.Abstractions;

#if NETFRAMEWORK
using System.Net;
#else
using Microsoft.AspNetCore.Http;
#endif

namespace IntegrationTests.Helpers;

public class MockMetricsCollector : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly TestHttpServer _listener;

    private readonly List<Expectation> _expectations = new();
    private readonly BlockingCollection<List<Collected>> _metricsSnapshots = new(10); // bounded to avoid memory leak; contains protobuf type

    public MockMetricsCollector(ITestOutputHelper output, string host = "localhost")
    {
        _output = output;
#if NETFRAMEWORK
        _listener = new(output, HandleHttpRequests, host, "/v1/metrics/");
#else
        _listener = new(output, HandleHttpRequests, "/v1/metrics");
#endif
    }

    /// <summary>
    /// Gets the TCP port that this collector is listening on.
    /// </summary>
    public int Port { get => _listener.Port; }

    public OtlpResourceExpector ResourceExpector { get; } = new();

    public void Dispose()
    {
        WriteOutput($"Shutting down.");
        ResourceExpector.Dispose();
        _metricsSnapshots.Dispose();
        _listener.Dispose();
    }

    public void Expect(string instrumentationScopeName, Func<Metric, bool> predicate = null, string description = null)
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

        var missingExpectations = new List<Expectation>(_expectations);
        var expectationsMet = new List<Collected>();
        var additionalEntries = new List<Collected>();

        timeout ??= Timeout.Expectation;
        var cts = new CancellationTokenSource();

        try
        {
            cts.CancelAfter(timeout.Value);
            foreach (var collectedMetricsSnapshot in _metricsSnapshots.GetConsumingEnumerable(cts.Token))
            {
                missingExpectations = new List<Expectation>(_expectations);
                expectationsMet = new List<Collected>();
                additionalEntries = new List<Collected>();

                foreach (var collected in collectedMetricsSnapshot)
                {
                    bool found = false;
                    for (int i = missingExpectations.Count - 1; i >= 0; i--)
                    {
                        if (collected.InstrumentationScopeName != missingExpectations[i].InstrumentationScopeName)
                        {
                            continue;
                        }

                        if (!missingExpectations[i].Predicate(collected.Metric))
                        {
                            continue;
                        }

                        expectationsMet.Add(collected);
                        missingExpectations.RemoveAt(i);
                        found = true;
                        break;
                    }

                    if (!found)
                    {
                        additionalEntries.Add(collected);
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
            FailMetrics(missingExpectations, expectationsMet, additionalEntries);
        }
        catch (OperationCanceledException)
        {
            // timeout
            FailMetrics(missingExpectations, expectationsMet, additionalEntries);
        }
    }

    internal void AssertEmpty(TimeSpan? timeout = null)
    {
        timeout ??= Timeout.NoExpectation;
        while (_metricsSnapshots.TryTake(out var metricsResource, timeout.Value))
        {
            if (metricsResource.Count > 0)
            {
                Assert.Fail($"Expected nothing, but got: {metricsResource}");
            }
        }
    }

    private static void FailMetrics(
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

#if NETFRAMEWORK
    private void HandleHttpRequests(HttpListenerContext ctx)
    {
        var metricsMessage = ExportMetricsServiceRequest.Parser.ParseFrom(ctx.Request.InputStream);
        HandleMetricsMessage(metricsMessage);

        ctx.GenerateEmptyProtobufResponse<ExportMetricsServiceResponse>();
    }
#else
    private async Task HandleHttpRequests(HttpContext ctx)
    {
        using var bodyStream = await ctx.ReadBodyToMemoryAsync();
        var metricsMessage = ExportMetricsServiceRequest.Parser.ParseFrom(bodyStream);
        HandleMetricsMessage(metricsMessage);

        await ctx.GenerateEmptyProtobufResponseAsync<ExportMetricsServiceResponse>();
    }
#endif

    private void HandleMetricsMessage(ExportMetricsServiceRequest metricsMessage)
    {
        foreach (var resourceMetric in metricsMessage.ResourceMetrics ?? Enumerable.Empty<ResourceMetrics>())
        {
            ResourceExpector.Collect(resourceMetric.Resource);

            // process metrics snapshot
            var metricsSnapshot = new List<Collected>();
            foreach (var scopeMetrics in resourceMetric.ScopeMetrics ?? Enumerable.Empty<ScopeMetrics>())
            {
                foreach (var metric in scopeMetrics.Metrics ?? Enumerable.Empty<Metric>())
                {
                    metricsSnapshot.Add(new Collected
                    {
                        InstrumentationScopeName = scopeMetrics.Scope.Name,
                        Metric = metric
                    });
                }
            }

            _metricsSnapshots.Add(metricsSnapshot);
        }
    }

    private void WriteOutput(string msg)
    {
        const string name = nameof(MockMetricsCollector);
        _output.WriteLine($"[{name}]: {msg}");
    }

    private class Expectation
    {
        public string InstrumentationScopeName { get; set; }

        public Func<Metric, bool> Predicate { get; set; }

        public string Description { get; set; }
    }

    private class Collected
    {
        public string InstrumentationScopeName { get; set; }

        public Metric Metric { get; set; } // protobuf type

        public override string ToString()
        {
            return $"InstrumentationScopeName = {InstrumentationScopeName}, Metric = {Metric}";
        }
    }
}
