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
using OpenTelemetry.Proto.Metrics.V1;
using Xunit;
using Xunit.Abstractions;

namespace IntegrationTests.Helpers;

public class MockMetricsCollector : IDisposable
{
    private static readonly TimeSpan DefaultWaitTimeout = TimeSpan.FromMinutes(1);

    private readonly ITestOutputHelper _output;
    private readonly TestHttpListener _listener;

    private readonly List<Expectation> _expectations = new();
    private readonly List<ResourceExpectation> _resourceExpectations = new();

    private readonly BlockingCollection<List<CollectedMetric>> _metricsSnapshots = new(10); // bounded to avoid memory leak; contains protobuf type

    private readonly ManualResetEvent _resourceAttributesEvent = new(false); // synchronizes access to _resourceAttributes
    private RepeatedField<KeyValue> _resourceAttributes; // protobuf type

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
            throw new InvalidOperationException($"Cannot start {nameof(MockMetricsCollector)}!");
        }

        return collector;
    }

    public void Dispose()
    {
        WriteOutput($"Shutting down.");
        _metricsSnapshots.Dispose();
        _resourceAttributesEvent.Dispose();
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
        var expectationsMet = new List<CollectedMetric>();
        var additionalEntries = new List<CollectedMetric>();

        timeout ??= DefaultWaitTimeout;
        var cts = new CancellationTokenSource();

        try
        {
            cts.CancelAfter(timeout.Value);

            // loop until expectations met or timeout
            while (true)
            {
                var metrics = _metricsSnapshots.Take(cts.Token); // get the metrics snapshot

                missingExpectations = new List<Expectation>(_expectations);
                expectationsMet = new List<CollectedMetric>();
                additionalEntries = new List<CollectedMetric>();

                foreach (var metric in metrics)
                {
                    bool found = false;
                    for (int i = missingExpectations.Count - 1; i >= 0; i--)
                    {
                        if (metric.InstrumentationScopeName != missingExpectations[i].InstrumentationScopeName)
                        {
                            continue;
                        }

                        if (!missingExpectations[i].Predicate(metric.Metric))
                        {
                            continue;
                        }

                        expectationsMet.Add(metric);
                        missingExpectations.RemoveAt(i);
                        found = true;
                        break;
                    }

                    if (!found)
                    {
                        additionalEntries.Add(metric);
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

        timeout ??= DefaultWaitTimeout;

        try
        {
            if (!_resourceAttributesEvent.WaitOne(timeout.Value))
            {
                FailResourceMetrics(_resourceExpectations, null);
                return;
            }

            AssertResourceMetrics(_resourceExpectations, _resourceAttributes);
        }
        catch (ArgumentOutOfRangeException)
        {
            // WaitOne called with non-positive value
            FailResourceMetrics(_resourceExpectations, null);
        }
    }

    private static void AssertResourceMetrics(List<ResourceExpectation> resourceExpectations, RepeatedField<KeyValue> actualResourceAttributes)
    {
        var missingExpectations = new List<ResourceExpectation>(resourceExpectations);
        foreach (var resourceAttribute in actualResourceAttributes)
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
            FailResourceMetrics(missingExpectations, actualResourceAttributes);
        }
    }

    private static void FailMetrics(
        List<Expectation> missingExpectations,
        List<CollectedMetric> expectationsMet,
        List<CollectedMetric> additionalEntries)
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

    private static void FailResourceMetrics(List<ResourceExpectation> missingExpectations, RepeatedField<KeyValue> attributes)
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
                foreach (var resourceMetric in metricsMessage.ResourceMetrics)
                {
                    if (resourceMetric.ScopeMetrics != null)
                    {
                        // resource metrics are always the same. set them only once.
                        if (_resourceAttributes == null)
                        {
                            _resourceAttributes = resourceMetric.Resource.Attributes;
                            _resourceAttributesEvent.Set();
                        }

                        // process metrics snapshot
                        var metricsSnapshot = new List<CollectedMetric>();
                        foreach (var scopeMetrics in resourceMetric.ScopeMetrics)
                        {
                            if (scopeMetrics.Metrics != null)
                            {
                                foreach (var metric in scopeMetrics.Metrics)
                                {
                                    metricsSnapshot.Add(new CollectedMetric
                                    {
                                        InstrumentationScopeName = scopeMetrics.Scope.Name,
                                        Metric = metric
                                    });
                                }
                            }
                        }

                        _metricsSnapshots.Add(metricsSnapshot);
                    }
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

        public Func<Metric, bool> Predicate { get; set; }

        public string Description { get; set; }
    }

    private class ResourceExpectation
    {
        public string Key { get; set; }

        public string Value { get; set; }
    }

    private class CollectedMetric
    {
        public string InstrumentationScopeName { get; set; }

        public Metric Metric { get; set; } // protobuf type

        public override string ToString()
        {
            return $"InstrumentationScopeName = {InstrumentationScopeName}, Metric = {Metric}";
        }
    }
}
