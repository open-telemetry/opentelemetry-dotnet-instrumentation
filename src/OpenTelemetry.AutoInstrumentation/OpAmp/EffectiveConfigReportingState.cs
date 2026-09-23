// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics.CodeAnalysis;
using OpenTelemetry.AutoInstrumentation.Logging;
using OpenTelemetry.AutoInstrumentation.PluginApi.OpAmp;
using OpenTelemetry.OpAmp.Client.Messages;

namespace OpenTelemetry.AutoInstrumentation.OpAmp;

internal sealed class EffectiveConfigReportingState
{
    private static readonly IOtelLogger Logger = OtelLogging.GetLogger("OpAmp");
    private readonly IProvideEffectiveConfig? _provider;
    private EffectiveConfigSnapshot? _snapshot;

    public EffectiveConfigReportingState(IProvideEffectiveConfig? provider)
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

        IReadOnlyCollection<EffectiveConfigFile> effectiveConfig;
        try
        {
            effectiveConfig = _provider.GetEffectiveConfig();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "The OpAmp effective configuration provider failed.");
            return false;
        }

        if (!EffectiveConfigSnapshot.TryCreate(effectiveConfig, out var newSnapshot))
        {
            Logger.Error("The OpAmp effective configuration provider returned an invalid configuration.");
            return false;
        }

        if (_snapshot != null && _snapshot.HasSameContent(newSnapshot))
        {
            return false;
        }

        _snapshot = newSnapshot;
        return true;
    }

    public bool TryGetSnapshot([NotNullWhen(true)] out IReadOnlyCollection<EffectiveConfigFile>? effectiveConfig)
    {
        effectiveConfig = _snapshot?.Files;
        return effectiveConfig != null;
    }
}
