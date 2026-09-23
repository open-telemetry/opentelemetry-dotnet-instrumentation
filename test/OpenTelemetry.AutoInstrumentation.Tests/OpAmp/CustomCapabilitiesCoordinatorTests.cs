// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections;
using System.Collections.Concurrent;
using OpenTelemetry.AutoInstrumentation.OpAmp;
using OpenTelemetry.OpAmp.Client.Messages;

namespace OpenTelemetry.AutoInstrumentation.Tests.OpAmp;

public class CustomCapabilitiesCoordinatorTests
{
    private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(5);

    [Fact]
    public void ReportsOnlyCanonicalStateChangesAndClearsPreviousState()
    {
        var reports = new List<IReadOnlyCollection<string>>();
        var state = CreateReportingState(capabilities => reports.Add(capabilities));
        var mutableCapabilities = new List<string>
        {
            "com.example.second/1",
            "com.example.first",
            "com.example.first"
        };

        Assert.Equal(CustomCapabilitiesReportRequirement.NotRequired, state.StartClientReporting());
        UpdateAndSubmitIfRequested(state, mutableCapabilities);
        mutableCapabilities.Clear();
        UpdateAndSubmitIfRequested(state, ["com.example.first", "com.example.second/1"]);
        UpdateAndSubmitIfRequested(state, ["com.example.third"]);
        UpdateAndSubmitIfRequested(state, []);

        Assert.Collection(
            reports,
            report => Assert.Equal(["com.example.first", "com.example.second/1"], report),
            report => Assert.Equal(["com.example.third"], report),
            Assert.Empty);
    }

    [Fact]
    public void InvalidClientStateDoesNotReplaceLastValidSnapshot()
    {
        var reports = new List<IReadOnlyCollection<string>>();
        var state = CreateReportingState(capabilities => reports.Add(capabilities));
        Assert.Equal(CustomCapabilitiesReportRequirement.NotRequired, state.StartClientReporting());
        UpdateAndSubmitIfRequested(state, ["com.example.valid"]);

        UpdateAndSubmitIfRequested(state, null!);
        UpdateAndSubmitIfRequested(state, [string.Empty]);
        UpdateAndSubmitIfRequested(state, [null!]);
        UpdateAndSubmitIfRequested(state, new ThrowingCollection());
        UpdateAndSubmitIfRequested(state, ["com.example.valid"]);

        Assert.Single(reports);
    }

    [Fact]
    public void ServerStateIsCopiedAndReplacedUsingOrdinalMatching()
    {
        var state = CreateState();
        var mutableCapabilities = new List<string> { "com.example.supported" };
        state.UpdateClientCapabilities(["com.example.supported", "COM.EXAMPLE.SUPPORTED"]);
        SubmitClientCapabilities(state);
        state.UpdateServerCapabilities(mutableCapabilities);
        mutableCapabilities.Clear();

        Assert.Equal(CustomMessageEligibility.Allowed, GetCustomMessageEligibility(state, "com.example.supported"));
        Assert.Equal(CustomMessageEligibility.UnsupportedByServer, GetCustomMessageEligibility(state, "COM.EXAMPLE.SUPPORTED"));

        state.UpdateServerCapabilities([]);

        Assert.Equal(CustomMessageEligibility.UnsupportedByServer, GetCustomMessageEligibility(state, "com.example.supported"));
    }

    [Fact]
    public void CustomMessageEligibilityUsesLastSubmittedClientState()
    {
        var state = CreateState();
        state.StartClientReporting();
        state.UpdateServerCapabilities(["com.example.first", "com.example.second"]);

        state.UpdateClientCapabilities(["com.example.first"]);

        Assert.Equal(
            CustomMessageEligibility.CapabilityPublicationPending,
            GetCustomMessageEligibility(state, "com.example.first"));

        SubmitClientCapabilities(state);
        state.UpdateClientCapabilities(["com.example.second"]);

        Assert.Equal(
            CustomMessageEligibility.CapabilityPublicationPending,
            GetCustomMessageEligibility(state, "com.example.second"));
        Assert.Equal(CustomMessageEligibility.Allowed, GetCustomMessageEligibility(state, "com.example.first"));

        SubmitClientCapabilities(state);
        state.UpdateClientCapabilities([]);

        Assert.Equal(CustomMessageEligibility.Allowed, GetCustomMessageEligibility(state, "com.example.second"));

        SubmitClientCapabilities(state);

        Assert.Equal(CustomMessageEligibility.CapabilityNotReported, GetCustomMessageEligibility(state, "com.example.second"));
        Assert.Equal(CustomMessageEligibility.CapabilityNotReported, GetCustomMessageEligibility(state, "COM.EXAMPLE.SECOND"));
    }

    [Fact]
    public void InvalidServerStateDoesNotReplaceLastValidSnapshot()
    {
        var state = CreateState();
        state.UpdateClientCapabilities(["com.example.valid"]);
        SubmitClientCapabilities(state);
        state.UpdateServerCapabilities(["com.example.valid"]);

        state.UpdateServerCapabilities(null!);
        state.UpdateServerCapabilities([string.Empty]);
        state.UpdateServerCapabilities([null!]);
        state.UpdateServerCapabilities(new ThrowingCollection());

        Assert.Equal(CustomMessageEligibility.Allowed, GetCustomMessageEligibility(state, "com.example.valid"));
    }

    [Fact]
    public void EquivalentClientCapabilitySubmissionIsSuppressed()
    {
        var submissions = new List<IReadOnlyCollection<string>>();
        var state = CreateState(sendClientCapabilities: capabilities => submissions.Add(capabilities));
        var mutableCapabilities = new List<string> { "com.example.second", "com.example.first" };

        state.UpdateClientCapabilities(mutableCapabilities);
        state.SubmitClientCapabilitiesIfChanged();
        mutableCapabilities.Clear();
        state.UpdateClientCapabilities(["com.example.first", "com.example.second", "com.example.first"]);
        state.SubmitClientCapabilitiesIfChanged();

        Assert.Collection(
            submissions,
            submission => Assert.Equal(["com.example.first", "com.example.second"], submission));
    }

    [Fact]
    public void FullStateSubmissionUsesLatestStateAndAlwaysRuns()
    {
        const string FirstCapability = "com.example.first";
        const string SecondCapability = "com.example.second";
        var submissions = new List<IReadOnlyCollection<string>>();
        var state = CreateState(sendFullStateReport: report => submissions.Add(report.CustomCapabilities!.ToArray()));
        state.UpdateServerCapabilities([FirstCapability, SecondCapability]);

        state.UpdateClientCapabilities([FirstCapability]);
        state.SubmitFullStateWithCapabilities(new FullStateReport());
        state.UpdateClientCapabilities([SecondCapability]);
        state.SubmitFullStateWithCapabilities(new FullStateReport());
        state.SubmitFullStateWithCapabilities(new FullStateReport());

        Assert.Collection(
            submissions,
            submission => Assert.Equal([FirstCapability], submission),
            submission => Assert.Equal([SecondCapability], submission),
            submission => Assert.Equal([SecondCapability], submission));
        Assert.Equal(CustomMessageEligibility.Allowed, GetCustomMessageEligibility(state, SecondCapability));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FailedSubmissionDoesNotAdvanceSubmittedState(bool fullState)
    {
        const string FirstCapability = "com.example.first";
        const string SecondCapability = "com.example.second";
        var failSubmission = false;

        void ThrowIfRequested()
        {
            if (failSubmission)
            {
                throw new InvalidOperationException("Test failure.");
            }
        }

        var state = CreateState(
            sendClientCapabilities: _ => ThrowIfRequested(),
            sendFullStateReport: _ => ThrowIfRequested());
        state.UpdateServerCapabilities([FirstCapability, SecondCapability]);
        state.UpdateClientCapabilities([FirstCapability]);
        SubmitClientCapabilities(state);
        state.UpdateClientCapabilities([SecondCapability]);
        failSubmission = true;

        if (fullState)
        {
            Assert.Throws<InvalidOperationException>(() => state.SubmitFullStateWithCapabilities(new FullStateReport()));
        }
        else
        {
            Assert.Throws<InvalidOperationException>(() => state.SubmitClientCapabilitiesIfChanged());
        }

        Assert.Equal(CustomMessageEligibility.Allowed, GetCustomMessageEligibility(state, FirstCapability));
        Assert.Equal(CustomMessageEligibility.CapabilityPublicationPending, GetCustomMessageEligibility(state, SecondCapability));
    }

    [Fact]
    public void CustomMessageSendFailurePropagates()
    {
        const string Capability = "com.example.supported";
        var state = CreateState(
            sendCustomMessage: (_, _, _) => throw new InvalidOperationException("Test failure."));
        state.UpdateClientCapabilities([Capability]);
        SubmitClientCapabilities(state);
        state.UpdateServerCapabilities([Capability]);

        Assert.Throws<InvalidOperationException>(() =>
            state.SendCustomMessageIfEligible(Capability, "test", ReadOnlyMemory<byte>.Empty));
    }

    [Fact]
    public async Task ServerCapabilityReplacementCannotInterleaveWithCustomMessageSend()
    {
        const string Capability = "com.example.supported";
        using var sendEntered = new ManualResetEventSlim();
        using var releaseSend = new ManualResetEventSlim();
        using var serverUpdateStarted = new ManualResetEventSlim();
        var events = new ConcurrentQueue<string>();
        var state = CreateState(sendCustomMessage: (_, _, _) =>
        {
            sendEntered.Set();
            releaseSend.Wait();
            events.Enqueue("message-sent");
        });
        state.UpdateClientCapabilities([Capability]);
        SubmitClientCapabilities(state);
        state.UpdateServerCapabilities([Capability]);

        var sendTask = Task.Run(() => state.SendCustomMessageIfEligible(
            Capability,
            "test",
            ReadOnlyMemory<byte>.Empty));
        Task? serverUpdateTask = null;
        try
        {
            Assert.True(sendEntered.Wait(TestTimeout));
            serverUpdateTask = Task.Run(() =>
            {
                serverUpdateStarted.Set();
                state.UpdateServerCapabilities([]);
                events.Enqueue("server-updated");
            });
            Assert.True(serverUpdateStarted.Wait(TestTimeout));
            Assert.False(serverUpdateTask.IsCompleted);

            releaseSend.Set();
            await Task.WhenAll(sendTask, serverUpdateTask).ConfigureAwait(true);
        }
        finally
        {
            releaseSend.Set();
        }

        Assert.Equal(CustomMessageEligibility.Allowed, await sendTask.ConfigureAwait(true));
        Assert.Equal(["message-sent", "server-updated"], events);
        Assert.Equal(CustomMessageEligibility.UnsupportedByServer, GetCustomMessageEligibility(state, Capability));
    }

    [Fact]
    public void SubmissionUsesLatestDesiredState()
    {
        var reports = new List<IReadOnlyCollection<string>>();
        var state = CreateReportingState(capabilities => reports.Add(capabilities));

        state.UpdateClientCapabilities(["com.example.first"]);
        state.StartClientReporting();
        state.UpdateClientCapabilities(["com.example.second"]);
        state.SubmitClientCapabilitiesIfChanged();

        var report = Assert.Single(reports);
        Assert.Equal(["com.example.second"], report);
    }

    private static CustomCapabilitiesCoordinator CreateState(
        Action<IReadOnlyCollection<string>>? sendClientCapabilities = null,
        Action<FullStateReport>? sendFullStateReport = null,
        Action<string, string, ReadOnlyMemory<byte>>? sendCustomMessage = null)
    {
        return new CustomCapabilitiesCoordinator(
            new TestCustomCapabilitiesSink(
                sendClientCapabilities ?? (_ => { }),
                sendFullStateReport ?? (_ => { }),
                sendCustomMessage ?? ((_, _, _) => { })));
    }

    private static CustomCapabilitiesCoordinator CreateReportingState(Action<IReadOnlyCollection<string>> report)
    {
        return CreateState(sendClientCapabilities: report);
    }

    private static CustomMessageEligibility GetCustomMessageEligibility(CustomCapabilitiesCoordinator state, string capability)
    {
        return state.SendCustomMessageIfEligible(capability, "test", ReadOnlyMemory<byte>.Empty);
    }

    private static void SubmitClientCapabilities(CustomCapabilitiesCoordinator state)
    {
        state.SubmitClientCapabilitiesIfChanged();
    }

    private static void UpdateAndSubmitIfRequested(
        CustomCapabilitiesCoordinator state,
        IReadOnlyCollection<string> capabilities)
    {
        if (state.UpdateClientCapabilities(capabilities) == CustomCapabilitiesReportRequirement.Required)
        {
            state.SubmitClientCapabilitiesIfChanged();
        }
    }

    private sealed class TestCustomCapabilitiesSink(
        Action<IReadOnlyCollection<string>> sendClientCapabilities,
        Action<FullStateReport> sendFullStateReport,
        Action<string, string, ReadOnlyMemory<byte>> sendCustomMessage) : ICustomCapabilitiesSink
    {
        public void SubmitCapabilities(IReadOnlyCollection<string> capabilities)
        {
            sendClientCapabilities(capabilities);
        }

        public void SubmitFullState(FullStateReport report)
        {
            sendFullStateReport(report);
        }

        public void SendMessage(string capability, string type, ReadOnlyMemory<byte> data)
        {
            sendCustomMessage(capability, type, data);
        }
    }

    private sealed class ThrowingCollection : ICollection<string>, IReadOnlyCollection<string>
    {
        public int Count => 1;

        public bool IsReadOnly => true;

        public void Add(string item) => throw new NotSupportedException();

        public void Clear() => throw new NotSupportedException();

        public bool Contains(string item) => false;

        public void CopyTo(string[] array, int arrayIndex) => throw new NotSupportedException();

        public IEnumerator<string> GetEnumerator()
        {
            throw new InvalidOperationException("Enumeration failed.");
        }

        public bool Remove(string item) => throw new NotSupportedException();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
