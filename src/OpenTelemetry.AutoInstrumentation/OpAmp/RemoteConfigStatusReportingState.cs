// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics.CodeAnalysis;
using OpenTelemetry.AutoInstrumentation.Logging;
using OpenTelemetry.AutoInstrumentation.PluginApi.OpAmp;
using OpenTelemetry.OpAmp.Client.Messages;

namespace OpenTelemetry.AutoInstrumentation.OpAmp;

internal sealed class RemoteConfigStatusReportingState
{
    private static readonly IOtelLogger Logger = OtelLogging.GetLogger("OpAmp");
    private readonly IProvideRemoteConfigStatus? _provider;
    private RemoteConfigStatusSnapshot? _snapshot;
    private bool _initialized;

    public RemoteConfigStatusReportingState(IProvideRemoteConfigStatus? provider)
    {
        _provider = provider;
    }

    public bool Enabled { get; private set; }

    public void SetEnabled(bool enabled)
    {
        Enabled = enabled && _provider != null;
    }

    public bool TryUpdateSnapshot()
    {
        if (!Enabled || _provider == null)
        {
            return false;
        }

        RemoteConfigStatusReport? remoteConfigStatus;
        try
        {
            remoteConfigStatus = _provider.GetRemoteConfigStatus();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "The OpAmp remote configuration status provider failed.");
            return false;
        }

        var newSnapshot = remoteConfigStatus == null ? null : RemoteConfigStatusSnapshot.Create(remoteConfigStatus);
        var changed = !_initialized ||
                      (_snapshot == null) != (newSnapshot == null) ||
                      (_snapshot != null && newSnapshot != null && !_snapshot.HasSameContent(newSnapshot));

        _initialized = true;
        if (!changed)
        {
            return false;
        }

        _snapshot = newSnapshot;
        return true;
    }

    public bool TryGetSnapshot([NotNullWhen(true)] out RemoteConfigStatusReport? remoteConfigStatus)
    {
        remoteConfigStatus = _snapshot?.Report;
        return remoteConfigStatus != null;
    }
}
