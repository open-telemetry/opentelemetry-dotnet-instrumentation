// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using OpenTelemetry.Proto.Collector.Trace.V1;
using OpenTelemetry.Proto.Trace.V1;
using Xunit.Abstractions;

#if NETFRAMEWORK
using System.Net;
#else
using Microsoft.AspNetCore.Http;
#endif

namespace IntegrationTests.Helpers;

#pragma warning disable CA1812 // Mark members as static. There is some issue in dotnet format.
internal sealed class MockSpansCollector : IDisposable
#pragma warning restore CA1812 // Mark members as static. There is some issue in dotnet format.
{
    private readonly ITestOutputHelper _output;
    private readonly TestHttpServer _listener;

    private readonly BlockingCollection<Collected> _spans = new(100); // bounded to avoid memory leak
    private readonly List<Expectation> _expectations = new();
    private Func<ICollection<Collected>, bool>? _collectedExpectation;

    public MockSpansCollector(ITestOutputHelper output, string host = "localhost")
    {
        _output = output;

#if NET
        _listener = new TestHttpServer(output, nameof(MockSpansCollector), new PathHandler(HandleHttpRequests, "/v1/traces"));
#else
        _listener = new TestHttpServer(output, HandleHttpRequests, host, "/v1/traces/");
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
        _spans.Dispose();
        _listener.Dispose();
    }

    public void Expect(string instrumentationScopeName, Func<Span, bool>? predicate = null, string? description = null)
    {
        description ??= $"<no description> Instrumentation Scope Name: '{instrumentationScopeName}', predicate is null: '{predicate == null}'";
        predicate ??= x => true;

        _expectations.Add(new Expectation(instrumentationScopeName, predicate, description));
    }

    public void ExpectCollected(Func<ICollection<Collected>, bool> collectedExpectation)
    {
        _collectedExpectation = collectedExpectation;
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
            foreach (var resourceSpans in _spans.GetConsumingEnumerable(cts.Token))
            {
                var found = false;
                for (var i = missingExpectations.Count - 1; i >= 0; i--)
                {
                    if (missingExpectations[i].InstrumentationScopeName != resourceSpans.InstrumentationScopeName)
                    {
                        continue;
                    }

                    if (!missingExpectations[i].Predicate(resourceSpans.Span))
                    {
                        continue;
                    }

                    expectationsMet.Add(resourceSpans);
                    missingExpectations.RemoveAt(i);
                    found = true;
                    break;
                }

                if (!found)
                {
                    additionalEntries.Add(resourceSpans);
                    continue;
                }

                if (missingExpectations.Count == 0)
                {
                    if (_collectedExpectation != null && !_collectedExpectation(expectationsMet))
                    {
                        FailCollectedExpectation(expectationsMet);
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

    public void AssertEmpty(TimeSpan? timeout = null)
    {
        timeout ??= TestTimeout.NoExpectation;
        if (_spans.TryTake(out var resourceSpan, timeout.Value))
        {
            Assert.Fail($"Expected nothing, but got: {resourceSpan}");
        }
    }

    private static void FailCollectedExpectation(List<Collected> expectationsMet)
    {
        var message = new StringBuilder();
        message.AppendLine("Collected spans expectation failed.");
        message.AppendLine("Collected spans:");
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
        var traceMessage = ExportTraceServiceRequest.Parser.ParseFrom(ctx.Request.InputStream);
        HandleTraceMessage(traceMessage);

        ctx.GenerateEmptyProtobufResponse<ExportTraceServiceResponse>();
    }
#else
    private async Task HandleHttpRequests(HttpContext ctx)
    {
        using var bodyStream = await ctx.ReadBodyToMemoryAsync().ConfigureAwait(false);
        var traceMessage = ExportTraceServiceRequest.Parser.ParseFrom(bodyStream);
        HandleTraceMessage(traceMessage);

        await ctx.GenerateEmptyProtobufResponseAsync<ExportTraceServiceResponse>().ConfigureAwait(false);
    }
#endif

    private void HandleTraceMessage(ExportTraceServiceRequest traceMessage)
    {
        foreach (var resourceSpan in traceMessage.ResourceSpans ?? Enumerable.Empty<ResourceSpans>())
        {
            ResourceExpector.Collect(resourceSpan.Resource);
            foreach (var scopeSpans in resourceSpan.ScopeSpans ?? Enumerable.Empty<ScopeSpans>())
            {
                foreach (var span in scopeSpans.Spans ?? Enumerable.Empty<Span>())
                {
                    _spans.Add(new Collected(scopeSpans.Scope.Name, span));
                }
            }
        }
    }

    private void WriteOutput(string msg)
    {
        const string name = nameof(MockSpansCollector);
        _output.WriteLine($"[{name}]: {msg}");
    }

    internal sealed class Collected
    {
        public Collected(string instrumentationScopeName, Span span)
        {
            InstrumentationScopeName = instrumentationScopeName;
            Span = span;
        }

        public string InstrumentationScopeName { get; }

        public Span Span { get; } // protobuf type

        public override string ToString()
        {
            return $"InstrumentationScopeName = {InstrumentationScopeName}, Span = {Span}";
        }
    }

    private sealed class Expectation
    {
        public Expectation(string instrumentationScopeName, Func<Span, bool> predicate, string? description)
        {
            InstrumentationScopeName = instrumentationScopeName;
            Predicate = predicate;
            Description = description;
        }

        public string InstrumentationScopeName { get; }

        public Func<Span, bool> Predicate { get; }

        public string? Description { get; }
    }
}
