// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using OpenTelemetry.Proto.Collector.Profiles.V1Development;
using OpenTelemetry.Proto.Profiles.V1Development;
using Xunit.Abstractions;

#if NETFRAMEWORK
using System.Net;
#else
using Microsoft.AspNetCore.Http;
#endif

namespace IntegrationTests.Helpers;

internal sealed class MockProfilesCollector : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly TestHttpServer _listener;

    private readonly List<Expectation> _expectations = new();
    private readonly BlockingCollection<Collected> _profilesSnapshots = new(10); // bounded to avoid memory leak; contains protobuf type
    private CollectedExpectation? _collectedExpectation;

    private MockProfilesCollector(ITestOutputHelper output, string host)
    {
        _output = output;
#if NETFRAMEWORK
        _listener = new(output, HandleHttpRequests, host, "/v1development/profiles/");
#else
        _listener = new(output, nameof(MockProfilesCollector), new PathHandler(HandleHttpRequests, "/v1development/profiles"));
#endif
    }

    /// <summary>
    /// Gets the TCP port that this collector is listening on.
    /// </summary>
    public int Port { get => _listener.Port; }

    public OtlpResourceExpector ResourceExpector { get; } = new();

#if NET
    public static async Task<MockProfilesCollector> InitializeAsync(ITestOutputHelper output, string host = "localhost")
#else
    public static Task<MockProfilesCollector> InitializeAsync(ITestOutputHelper output, string host = "localhost")
#endif
    {
        var collector = new MockProfilesCollector(output, host);
#if NET
        await MockCollectorHealthZ.WarmupHealthZEndpoint(output, host, collector.Port);
        return collector;
#else
        return Task.FromResult(collector);
#endif
    }

    public void Dispose()
    {
        WriteOutput("Shutting down.");
        ResourceExpector.Dispose();
        _profilesSnapshots.Dispose();
        _listener.Dispose();
    }

    public void Expect(Func<ExportProfilesServiceRequest, bool>? predicate = null, string? description = null)
    {
        predicate ??= x => true;

        _expectations.Add(new Expectation(predicate, description));
    }

    public void ExpectCollected(Func<ICollection<ExportProfilesServiceRequest>, bool> collectedExpectation, string description)
    {
        _collectedExpectation = new(collectedExpectation, description);
    }

    public void AssertCollected()
    {
        if (_collectedExpectation == null)
        {
            throw new InvalidOperationException("Expectation for collected profiling snapshot was not set");
        }

        var collected = _profilesSnapshots.Select(collected => collected.ExportProfilesServiceRequest).ToArray();

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
        var expectationsMet = new List<Collected>();
        var additionalEntries = new List<Collected>();

        timeout ??= TestTimeout.Expectation;
        using var cts = new CancellationTokenSource();

        try
        {
            cts.CancelAfter(timeout.Value);
            foreach (var collectedProfilesDataSnapshot in _profilesSnapshots.GetConsumingEnumerable(cts.Token))
            {
                var found = false;
                for (var i = missingExpectations.Count - 1; i >= 0; i--)
                {
                    if (!missingExpectations[i].Predicate(collectedProfilesDataSnapshot.ExportProfilesServiceRequest))
                    {
                        continue;
                    }

                    expectationsMet.Add(collectedProfilesDataSnapshot);
                    missingExpectations.RemoveAt(i);
                    found = true;
                    break;
                }

                if (!found)
                {
                    additionalEntries.Add(collectedProfilesDataSnapshot);
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
        timeout ??= TestTimeout.Expectation;

        if (_profilesSnapshots.TryTake(out var collected, timeout.Value))
        {
            var message = new StringBuilder();
            message.AppendLine("Expected no profiles to be collected, but found:");
            message.AppendLine($"  \"{collected}\"");

            // Drain any additional items
            var additionalCount = 0;
            while (_profilesSnapshots.TryTake(out _, TimeSpan.Zero))
            {
                additionalCount++;
            }

            if (additionalCount > 0)
            {
                message.AppendLine($"  ... and {additionalCount} more profile batch(es)");
            }

            Assert.Fail(message.ToString());
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

    private static void FailCollectedExpectation(string? collectedExpectationDescription, ExportProfilesServiceRequest[] collectedExportProfilesServiceRequests)
    {
        var message = new StringBuilder();
        message.AppendLine(CultureInfo.InvariantCulture, $"Collected profiles expectation failed: {collectedExpectationDescription}");
        message.AppendLine("Collected profiles:");
        foreach (var logRecord in collectedExportProfilesServiceRequests)
        {
            message.AppendLine(CultureInfo.InvariantCulture, $"    \"{logRecord}\"");
        }

        Assert.Fail(message.ToString());
    }

#if NETFRAMEWORK
    private void HandleHttpRequests(HttpListenerContext ctx)
    {
        var profilesMessage = ExportProfilesServiceRequest.Parser.ParseFrom(ctx.Request.InputStream);
        HandleProfilesMessage(profilesMessage);

        ctx.GenerateEmptyProtobufResponse<ExportProfilesServiceResponse>();
    }
#else
    private async Task HandleHttpRequests(HttpContext ctx)
    {
        using var bodyStream = await ctx.ReadBodyToMemoryAsync().ConfigureAwait(false);
        var profilesMessage = ExportProfilesServiceRequest.Parser.ParseFrom(bodyStream);
        HandleProfilesMessage(profilesMessage);

        await ctx.GenerateEmptyProtobufResponseAsync<ExportProfilesServiceResponse>().ConfigureAwait(false);
    }
#endif

    private void HandleProfilesMessage(ExportProfilesServiceRequest profileMessage)
    {
        foreach (var resourceProfile in profileMessage.ResourceProfiles ?? Enumerable.Empty<ResourceProfiles>())
        {
            ResourceExpector.Collect(resourceProfile.Resource);
        }

        _profilesSnapshots.Add(new Collected(profileMessage));
    }

    private void WriteOutput(string msg)
    {
        const string name = nameof(MockProfilesCollector);
        _output.WriteLine($"[{name}]: {msg}");
    }

    internal sealed class Collected
    {
        public Collected(ExportProfilesServiceRequest exportProfilesServiceRequest)
        {
            ExportProfilesServiceRequest = exportProfilesServiceRequest;
        }

        public ExportProfilesServiceRequest ExportProfilesServiceRequest { get; } // protobuf type

        public override string ToString()
        {
            return $"ExportProfilesServiceRequest = {ExportProfilesServiceRequest}";
        }
    }

    private sealed class Expectation
    {
        public Expectation(Func<ExportProfilesServiceRequest, bool> predicate, string? description)
        {
            Predicate = predicate;
            Description = description;
        }

        public Func<ExportProfilesServiceRequest, bool> Predicate { get; }

        public string? Description { get; }
    }

    private sealed class CollectedExpectation
    {
        public CollectedExpectation(Func<ICollection<ExportProfilesServiceRequest>, bool> predicate, string? description)
        {
            Predicate = predicate;
            Description = description;
        }

        public Func<ICollection<ExportProfilesServiceRequest>, bool> Predicate { get; }

        public string? Description { get; }
    }
}
