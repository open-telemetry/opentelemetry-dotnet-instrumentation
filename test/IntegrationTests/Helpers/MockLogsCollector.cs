// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using OpenTelemetry.Proto.Collector.Logs.V1;
using OpenTelemetry.Proto.Logs.V1;
using Xunit.Abstractions;
#if NETFRAMEWORK
using System.Net;
#else
using Microsoft.AspNetCore.Http;
#endif

namespace IntegrationTests.Helpers;

internal sealed class MockLogsCollector : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly TestHttpServer _listener;
    private readonly BlockingCollection<LogRecord> _logs = new(100); // bounded to avoid memory leak
    private readonly List<Expectation> _expectations = new();
    private int _logsReceived;

    private CollectedExpectation? _collectedExpectation;

    public MockLogsCollector(ITestOutputHelper output, string host = "localhost")
    {
        _output = output;

#if NETFRAMEWORK
        _listener = new(output, HandleHttpRequests, host, "/v1/logs/");
#else
        _listener = new(output, nameof(MockLogsCollector), new PathHandler(HandleHttpRequests, "/v1/logs"));
#endif
    }

    /// <summary>
    /// Gets the TCP port that this collector is listening on.
    /// </summary>
    public int Port { get => _listener.Port; }

    public OtlpResourceExpector ResourceExpector { get; } = new();

    public void Dispose()
    {
        _listener.Dispose();
        WriteOutput($"Shutting down. Total logs received: '{_logsReceived}'");
        ResourceExpector.Dispose();
        _logs.Dispose();
    }

    public void Expect(Func<LogRecord, bool> predicate, string? description = null)
    {
        description ??= "<no description>";

        _expectations.Add(new Expectation(predicate, description));
    }

    public void ExpectCollected(Func<ICollection<LogRecord>, bool> collectedExpectation, string description)
    {
        _collectedExpectation = new(collectedExpectation, description);
    }

    public void AssertCollected()
    {
        if (_collectedExpectation == null)
        {
            throw new InvalidOperationException("Expectation for collected logs was not set");
        }

        var collected = _logs.ToArray();

        if (!_collectedExpectation.Predicate(collected))
        {
            FailCollectedExpectation(_collectedExpectation.Description, collected);
        }
    }

    public void AssertExpectations(TimeSpan? timeout = null)
    {
        if (_expectations.Count == 0)
        {
            throw new InvalidOperationException("Expectations were not set");
        }

        var missingExpectations = new List<Expectation>(_expectations);
        var expectationsMet = new List<LogRecord>();
        var additionalEntries = new List<LogRecord>();

        timeout ??= TestTimeout.Expectation;
        using var cts = new CancellationTokenSource();

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

    public void AssertEmpty(TimeSpan? timeout = null)
    {
        timeout ??= TestTimeout.NoExpectation;
        if (_logs.TryTake(out var logRecord, timeout.Value))
        {
            Assert.Fail($"Expected nothing, but got: {logRecord}");
        }
    }

    private static void FailCollectedExpectation(string? collectedExpectationDescription, LogRecord[] collectedLogRecords)
    {
        var message = new StringBuilder();
        message.AppendLine(CultureInfo.InvariantCulture, $"Collected logs expectation failed: {collectedExpectationDescription}");
        message.AppendLine("Collected logs:");
        foreach (var logRecord in collectedLogRecords)
        {
            message.AppendLine(CultureInfo.InvariantCulture, $"    \"{logRecord}\"");
        }

        Assert.Fail(message.ToString());
    }

    private static void FailExpectations(
        List<Expectation> missingExpectations,
        List<LogRecord> expectationsMet,
        List<LogRecord> additionalEntries)
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
        var logsMessage = ExportLogsServiceRequest.Parser.ParseFrom(ctx.Request.InputStream);
        HandleLogsMessage(logsMessage);

        ctx.GenerateEmptyProtobufResponse<ExportLogsServiceResponse>();
    }
#else
    private async Task HandleHttpRequests(HttpContext ctx)
    {
        using var bodyStream = await ctx.ReadBodyToMemoryAsync().ConfigureAwait(false);
        var metricsMessage = ExportLogsServiceRequest.Parser.ParseFrom(bodyStream);
        HandleLogsMessage(metricsMessage);

        await ctx.GenerateEmptyProtobufResponseAsync<ExportLogsServiceResponse>().ConfigureAwait(false);
    }
#endif

    private void HandleLogsMessage(ExportLogsServiceRequest logsMessage)
    {
        foreach (var resourceLogs in logsMessage.ResourceLogs ?? Enumerable.Empty<ResourceLogs>())
        {
            ResourceExpector.Collect(resourceLogs.Resource);
            foreach (var scopeLogs in resourceLogs.ScopeLogs ?? Enumerable.Empty<ScopeLogs>())
            {
                foreach (var logRecord in scopeLogs.LogRecords ?? Enumerable.Empty<LogRecord>())
                {
                    Interlocked.Increment(ref _logsReceived);
                    _logs.Add(logRecord);
                }
            }
        }
    }

    private void WriteOutput(string msg)
    {
        const string name = nameof(MockLogsCollector);
        _output.WriteLine($"[{name}]: {msg}");
    }

    private sealed class Expectation
    {
        public Expectation(Func<LogRecord, bool> predicate, string? description)
        {
            Predicate = predicate;
            Description = description;
        }

        public Func<LogRecord, bool> Predicate { get; }

        public string? Description { get; }
    }

    private sealed class CollectedExpectation
    {
        public CollectedExpectation(Func<ICollection<LogRecord>, bool> predicate, string? description)
        {
            Predicate = predicate;
            Description = description;
        }

        public Func<ICollection<LogRecord>, bool> Predicate { get; }

        public string? Description { get; }
    }
}
