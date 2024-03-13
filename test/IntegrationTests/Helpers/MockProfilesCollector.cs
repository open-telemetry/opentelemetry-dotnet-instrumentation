// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET6_0_OR_GREATER

using System.Collections.Concurrent;
using System.Text;
using Microsoft.AspNetCore.Http;
using OpenTelemetry.Proto.Collector.Profiles.V1;
using OpenTelemetry.Proto.Profiles.V1;
using Xunit.Abstractions;

namespace IntegrationTests.Helpers;

public class MockProfilesCollector : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly TestHttpServer _listener;

    private readonly List<Expectation> _expectations = new();
    private readonly BlockingCollection<Collected> _profilesSnapshots = new(10); // bounded to avoid memory leak; contains protobuf type

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

    public void Expect(Func<ProfilesData, bool>? predicate = null, string? description = null)
    {
        predicate ??= x => true;

        _expectations.Add(new Expectation(predicate, description));
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
                    if (!missingExpectations[i].Predicate(collectedProfilesDataSnapshot.ProfilesData))
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

    private async Task HandleHttpRequests(HttpContext ctx)
    {
        using var bodyStream = await ctx.ReadBodyToMemoryAsync();
        var profilesMessage = ExportProfilesServiceRequest.Parser.ParseFrom(bodyStream);
        HandleProfilesMessage(profilesMessage);

        await ctx.GenerateEmptyProtobufResponseAsync<ExportProfilesServiceResponse>();
    }

    private void HandleProfilesMessage(ExportProfilesServiceRequest metricsMessage)
    {
        foreach (var profilesData in metricsMessage.ProfilesData ?? Enumerable.Empty<ProfilesData>())
        {
            foreach (var resourceProfile in profilesData.ResourceProfiles)
            {
                ResourceExpector.Collect(resourceProfile.Resource);
            }

            _profilesSnapshots.Add(new Collected(profilesData));
        }
    }

    private void WriteOutput(string msg)
    {
        const string name = nameof(MockProfilesCollector);
        _output.WriteLine($"[{name}]: {msg}");
    }

    public class Collected
    {
        public Collected(ProfilesData profilesData)
        {
            ProfilesData = profilesData;
        }

        public ProfilesData ProfilesData { get; } // protobuf type

        public override string ToString()
        {
            return $"ProfilesData = {ProfilesData}";
        }
    }

    private class Expectation
    {
        public Expectation(Func<ProfilesData, bool> predicate, string? description)
        {
            Predicate = predicate;
            Description = description;
        }

        public Func<ProfilesData, bool> Predicate { get; }

        public string? Description { get; }
    }
}

#endif
