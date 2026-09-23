// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Logging;
using OpenTelemetry.OpAmp.Client.Messages;

namespace OpenTelemetry.AutoInstrumentation.OpAmp;

internal enum CustomMessageEligibility
{
    Allowed,
    CapabilityNotReported,
    CapabilityPublicationPending,
    UnsupportedByServer
}

internal enum CustomCapabilitiesReportRequirement
{
    NotRequired,
    Required
}

internal sealed class CustomCapabilitiesCoordinator
{
    private static readonly IOtelLogger Logger = OtelLogging.GetLogger("OpAmp");
    private readonly object _lock = new();
    private readonly ICustomCapabilitiesSink _sink;
    // Stored snapshots are ordinally sorted and unique, enabling structural equality and binary search.
    private string[] _clientCapabilities = [];
    private string[] _submittedClientCapabilities = [];
    private string[] _serverCapabilities = [];
    private bool _clientReportingStarted;

    // Sink operations synchronously append a frame and may schedule an asynchronous flush,
    // but do not wait for transport I/O. Invoking them under _lock keeps upstream acceptance
    // atomic with eligibility checks and submitted-state advancement.
    public CustomCapabilitiesCoordinator(ICustomCapabilitiesSink sink)
    {
        _sink = sink;
    }

    public CustomCapabilitiesReportRequirement UpdateClientCapabilities(IReadOnlyCollection<string> capabilities)
    {
        if (!TryNormalizeCapabilities(capabilities, out var snapshot))
        {
            Logger.Error("Cannot report OpAMP custom capabilities because the supplied collection is invalid or could not be enumerated.");
            return CustomCapabilitiesReportRequirement.NotRequired;
        }

        lock (_lock)
        {
            if (_clientCapabilities.SequenceEqual(snapshot, StringComparer.Ordinal))
            {
                return CustomCapabilitiesReportRequirement.NotRequired;
            }

            _clientCapabilities = snapshot;
            return _clientReportingStarted
                ? CustomCapabilitiesReportRequirement.Required
                : CustomCapabilitiesReportRequirement.NotRequired;
        }
    }

    public CustomCapabilitiesReportRequirement StartClientReporting()
    {
        lock (_lock)
        {
            if (_clientReportingStarted)
            {
                return CustomCapabilitiesReportRequirement.NotRequired;
            }

            _clientReportingStarted = true;
            return _clientCapabilities.Length != 0
                ? CustomCapabilitiesReportRequirement.Required
                : CustomCapabilitiesReportRequirement.NotRequired;
        }
    }

    public void UpdateServerCapabilities(ICollection<string> capabilities)
    {
        if (!TryNormalizeCapabilities(capabilities, out var snapshot))
        {
            Logger.Error("Cannot update OpAMP server custom capabilities because the supplied collection is invalid or could not be enumerated.");
            return;
        }

        lock (_lock)
        {
            _serverCapabilities = snapshot;
        }
    }

    // Transport callbacks run under _lock so eligibility/submission and upstream acceptance form one ordered operation.
    public void SubmitClientCapabilitiesIfChanged()
    {
        lock (_lock)
        {
            if (_submittedClientCapabilities.SequenceEqual(_clientCapabilities, StringComparer.Ordinal))
            {
                return;
            }

            string[] snapshot = [.. _clientCapabilities];
            _sink.SubmitCapabilities(snapshot);
            _submittedClientCapabilities = snapshot;
        }
    }

    public void SubmitFullStateWithCapabilities(FullStateReport report)
    {
        lock (_lock)
        {
            string[] snapshot = [.. _clientCapabilities];
            report.CustomCapabilities = snapshot;
            _sink.SubmitFullState(report);
            _submittedClientCapabilities = snapshot;
        }
    }

    public CustomMessageEligibility SendCustomMessageIfEligible(
        string capability,
        string type,
        ReadOnlyMemory<byte> data)
    {
        lock (_lock)
        {
            var eligibility = GetCustomMessageEligibility(capability);
            if (eligibility == CustomMessageEligibility.Allowed)
            {
                _sink.SendMessage(capability, type, data);
            }

            return eligibility;
        }
    }

    private static bool IsCapabilitySupported(string capability, string[] capabilitiesSnapshot)
    {
        return Array.BinarySearch(capabilitiesSnapshot, capability, StringComparer.Ordinal) >= 0;
    }

    private static bool TryNormalizeCapabilities(IEnumerable<string>? capabilities, out string[] snapshot)
    {
        snapshot = [];
        if (capabilities == null)
        {
            return false;
        }

        try
        {
            var uniqueCapabilities = new HashSet<string>(StringComparer.Ordinal);
            foreach (var capability in capabilities)
            {
                if (string.IsNullOrEmpty(capability))
                {
                    return false;
                }

                uniqueCapabilities.Add(capability);
            }

            snapshot = [.. uniqueCapabilities];
            Array.Sort(snapshot, StringComparer.Ordinal);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private CustomMessageEligibility GetCustomMessageEligibility(string capability)
    {
        if (IsCapabilitySupported(capability, _submittedClientCapabilities))
        {
            return IsCapabilitySupported(capability, _serverCapabilities)
                ? CustomMessageEligibility.Allowed
                : CustomMessageEligibility.UnsupportedByServer;
        }

        return IsCapabilitySupported(capability, _clientCapabilities)
            ? CustomMessageEligibility.CapabilityPublicationPending
            : CustomMessageEligibility.CapabilityNotReported;
    }
}
