// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using OpenTelemetry.Proto.Collector.Metrics.V1;
using OpenTelemetry.Proto.Metrics.V1;
using Xunit.Abstractions;

#if NETFRAMEWORK
using System.Net;
#else
using Microsoft.AspNetCore.Http;
#endif

namespace IntegrationTests.Helpers;

internal sealed class MockMetricsCollector : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly TestHttpServer _listener;

    private readonly List<Expectation> _expectations = new();
    private readonly BlockingCollection<List<Collected>> _metricsSnapshots = new(10); // bounded to avoid memory leak; contains protobuf type
    private Func<ICollection<Collected>, bool>? _additionalEntriesExpectation;

    public MockMetricsCollector(ITestOutputHelper output, string host = "localhost")
    {
        _output = output;
#if NETFRAMEWORK
        _listener = new(output, HandleHttpRequests, host, "/v1/metrics/");
#else
        _listener = new(output, nameof(MockMetricsCollector), new PathHandler(HandleHttpRequests, "/v1/metrics"));
#endif
    }

    /// <summary>
    /// Gets the TCP port that this collector is listening on.
    /// </summary>
    public int Port { get => _listener.Port; }

    public OtlpResourceExpector ResourceExpector { get; } = new();

    public void Dispose()
    {
        WriteOutput("Shutting down.");
        ResourceExpector.Dispose();
        _metricsSnapshots.Dispose();
        _listener.Dispose();
    }

    public void Expect(string instrumentationScopeName, Func<Metric, bool>? predicate = null, string? description = null)
    {
        predicate ??= x => true;
        description ??= instrumentationScopeName;

        _expectations.Add(new Expectation(instrumentationScopeName, predicate, description));
    }

    public void ExpectAdditionalEntries(Func<ICollection<Collected>, bool> additionalEntriesExpectation)
    {
        _additionalEntriesExpectation = additionalEntriesExpectation;
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

        timeout ??= TestTimeout.Expectation;
        using var cts = new CancellationTokenSource();

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
                    if (_additionalEntriesExpectation != null && !_additionalEntriesExpectation(additionalEntries))
                    {
                        FailAdditionalEntriesExpectation(additionalEntries);
                    }

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

    internal void AssertEmpty(TimeSpan? timeout = null)
    {
        timeout ??= TestTimeout.NoExpectation;
        while (_metricsSnapshots.TryTake(out var metricsResource, timeout.Value))
        {
            if (metricsResource.Count > 0)
            {
                Assert.Fail($"Expected nothing, but got: {metricsResource}");
            }
        }
    }

    private static void FailAdditionalEntriesExpectation(List<Collected> expectationsMet)
    {
        var message = new StringBuilder();
        message.AppendLine("Additional entries - metrics expectation failed.");
        message.AppendLine("Additional entries -  metrics:");
        foreach (var line in expectationsMet)
        {
            message.AppendLine(CultureInfo.InvariantCulture, $"    \"{line}\"");
        }

        Assert.Fail(message.ToString());
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
            message.AppendLine(CultureInfo.InvariantCulture, $"  - \"{logline.Description}\"");
        }

        message.AppendLine("Entries meeting expectations:");
        foreach (var logline in expectationsMet)
        {
            message.AppendLine(CultureInfo.InvariantCulture, $"    \"{logline}\"");
        }

        message.AppendLine("Additional entries:");
        foreach (var logline in additionalEntries)
        {
            message.AppendLine(CultureInfo.InvariantCulture, $"  + \"{logline}\"");
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
        using var bodyStream = await ctx.ReadBodyToMemoryAsync().ConfigureAwait(false);
        var metricsMessage = ExportMetricsServiceRequest.Parser.ParseFrom(bodyStream);
        HandleMetricsMessage(metricsMessage);

        await ctx.GenerateEmptyProtobufResponseAsync<ExportMetricsServiceResponse>().ConfigureAwait(false);
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
                    metricsSnapshot.Add(new Collected(scopeMetrics.Scope.Name, metric));
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

    internal sealed class Collected
    {
        public Collected(string instrumentationScopeName, Metric metric)
        {
            InstrumentationScopeName = instrumentationScopeName;
            Metric = metric;
        }

        public string InstrumentationScopeName { get; }

        public Metric Metric { get; } // protobuf type

        public override string ToString()
        {
            return $"InstrumentationScopeName = {InstrumentationScopeName}, Metric = {Metric}";
        }
    }

    private sealed class Expectation
    {
        public Expectation(string instrumentationScopeName, Func<Metric, bool> predicate, string? description)
        {
            InstrumentationScopeName = instrumentationScopeName;
            Predicate = predicate;
            Description = description;
        }

        public string InstrumentationScopeName { get; }

        public Func<Metric, bool> Predicate { get; }

        public string? Description { get; }
    }
}
