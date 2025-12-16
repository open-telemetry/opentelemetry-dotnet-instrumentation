// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0
#if NET
using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using OpenTelemetry.Proto.Collector.Logs.V1;
using OpenTelemetry.Proto.Collector.Trace.V1;
using OpenTelemetry.Proto.Logs.V1;
using OpenTelemetry.Proto.Trace.V1;
using Xunit.Abstractions;

namespace IntegrationTests.Helpers;

internal sealed class MockCorrelationCollector : IDisposable
{
    private readonly BlockingCollection<LogRecord> _logs = new(100);
    private readonly BlockingCollection<MockSpansCollector.Collected> _spans = new(100);

    private readonly TestHttpServer _listener;
    private Func<MockSpansCollector.Collected, bool>? _spanFilter;
    private Func<LogRecord, bool>? _logRecordFilter;

    public MockCorrelationCollector(ITestOutputHelper helper)
    {
        _listener = new(
            helper,
            nameof(MockCorrelationCollector),
            new PathHandler(HandleLogHttpRequests, "/v1/logs"),
            new PathHandler(HandleSpanHttpRequests, "/v1/traces"));
    }

    public int Port => _listener.Port;

    public void Dispose()
    {
        _logs.Dispose();
        _spans.Dispose();
        _listener.Dispose();
    }

    public void ExpectSpan(Func<MockSpansCollector.Collected, bool> spanFilter)
    {
        _spanFilter = spanFilter;
    }

    public void ExpectLogRecord(Func<LogRecord, bool> logRecordFilter)
    {
        _logRecordFilter = logRecordFilter;
    }

    public void AssertCorrelation()
    {
        if (_logRecordFilter == null || _spanFilter == null)
        {
            throw new InvalidOperationException("Filters must be set before asserting correlation.");
        }

        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));

        var expectedLogRecord = _logs.GetConsumingEnumerable(cts.Token).First(_logRecordFilter);
        var expectedSpan = _spans.GetConsumingEnumerable(cts.Token).First(_spanFilter);

        Assert.Equal(expectedSpan.Span.SpanId, expectedLogRecord.SpanId);
        Assert.Equal(expectedSpan.Span.TraceId, expectedLogRecord.TraceId);
        Assert.Equal((uint)ActivityTraceFlags.Recorded, expectedLogRecord.Flags);
    }

    private async Task HandleLogHttpRequests(HttpContext ctx)
    {
        using var bodyStream = await ctx.ReadBodyToMemoryAsync().ConfigureAwait(false);
        var logsMessage = ExportLogsServiceRequest.Parser.ParseFrom(bodyStream);
        foreach (var resourceLogs in logsMessage.ResourceLogs ?? Enumerable.Empty<ResourceLogs>())
        {
            foreach (var scopeLogs in resourceLogs.ScopeLogs ?? Enumerable.Empty<ScopeLogs>())
            {
                foreach (var logRecord in scopeLogs.LogRecords ?? Enumerable.Empty<LogRecord>())
                {
                    _logs.Add(logRecord);
                }
            }
        }

        await ctx.GenerateEmptyProtobufResponseAsync<ExportLogsServiceResponse>().ConfigureAwait(false);
    }

    private async Task HandleSpanHttpRequests(HttpContext ctx)
    {
        using var bodyStream = await ctx.ReadBodyToMemoryAsync().ConfigureAwait(false);
        var traceMessage = ExportTraceServiceRequest.Parser.ParseFrom(bodyStream);
        foreach (var resourceSpan in traceMessage.ResourceSpans ?? Enumerable.Empty<ResourceSpans>())
        {
            foreach (var scopeSpans in resourceSpan.ScopeSpans ?? Enumerable.Empty<ScopeSpans>())
            {
                foreach (var span in scopeSpans.Spans ?? Enumerable.Empty<Span>())
                {
                    _spans.Add(new MockSpansCollector.Collected(scopeSpans.Scope.Name, span));
                }
            }
        }

        await ctx.GenerateEmptyProtobufResponseAsync<ExportTraceServiceResponse>().ConfigureAwait(false);
    }
}
#endif
