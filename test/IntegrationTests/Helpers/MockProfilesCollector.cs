// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET

using System.Collections.Concurrent;
using System.Text;
using Microsoft.AspNetCore.Http;
using OpenTelemetry.Proto.Collector.Profiles.V1Development;
using OpenTelemetry.Proto.Profiles.V1Development;
using Xunit.Abstractions;

namespace IntegrationTests.Helpers;

public class MockProfilesCollector : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly TestHttpServer _listener;

    private readonly List<Expectation> _expectations = new();
    private readonly BlockingCollection<Collected> _profilesSnapshots = new(10); // bounded to avoid memory leak; contains protobuf type
    private CollectedExpectation? _collectedExpectation;

    public MockProfilesCollector(ITestOutputHelper output)
    {
        _output = output;
        _listener = new(output, new PathHandler(HandleHttpRequests, "/v1/profiles"));
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

    private static void FailCollectedExpectation(string? collectedExpectationDescription, ExportProfilesServiceRequest[] collectedExportProfilesServiceRequests)
    {
        var message = new StringBuilder();
        message.AppendLine($"Collected profiles expectation failed: {collectedExpectationDescription}");
        message.AppendLine("Collected profiles:");
        foreach (var logRecord in collectedExportProfilesServiceRequests)
        {
            message.AppendLine($"    \"{logRecord}\"");
        }

        Assert.Fail(message.ToString());
    }

    private async Task HandleHttpRequests(HttpContext ctx)
    {
        using var bodyStream = await ctx.ReadBodyToMemoryAsync();
        var profilesMessage = ExportProfilesServiceRequest.Parser.ParseFrom(bodyStream);
        HandleProfilesMessage(profilesMessage);

        await ctx.GenerateEmptyProtobufResponseAsync<ExportProfilesServiceResponse>();
    }

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

    public class Collected
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

    private class Expectation
    {
        public Expectation(Func<ExportProfilesServiceRequest, bool> predicate, string? description)
        {
            Predicate = predicate;
            Description = description;
        }

        public Func<ExportProfilesServiceRequest, bool> Predicate { get; }

        public string? Description { get; }
    }

    private class CollectedExpectation
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

#endif
