// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.PluginApi.OpAmp;
using OpenTelemetry.OpAmp.Client.Messages;
using OpenTelemetry.OpAmp.Client.Settings;

namespace OpenTelemetry.AutoInstrumentation.Tests.OpAmp.Fixtures;

#pragma warning disable CA1515 // Consider making public types internal. Needed for plugin loading.
public sealed class ReportingOpAmpPlugin : CountingOpAmpPlugin, IProvideEffectiveConfig, IProvideRemoteConfigStatus
{
    private int _effectiveConfigRequestCount;
    private int _remoteConfigStatusRequestCount;

    public int EffectiveConfigRequestCount => Volatile.Read(ref _effectiveConfigRequestCount);

    public int RemoteConfigStatusRequestCount => Volatile.Read(ref _remoteConfigStatusRequestCount);

    public override void ConfigureOpAmpOptions(OpAmpClientSettings settings)
    {
        base.ConfigureOpAmpOptions(settings);
        settings.EffectiveConfigurationReporting.EnableReporting = true;
        settings.RemoteConfiguration.ReportsRemoteConfigStatus = true;
    }

    public IReadOnlyCollection<EffectiveConfigFile> GetEffectiveConfig()
    {
        Interlocked.Increment(ref _effectiveConfigRequestCount);
        return [];
    }

    public RemoteConfigStatusReport? GetRemoteConfigStatus()
    {
        Interlocked.Increment(ref _remoteConfigStatusRequestCount);
        return new RemoteConfigStatusReport(new byte[] { 1 }, RemoteConfigStatusCode.Unset);
    }

    public bool WaitForProviderRequests(int count, TimeSpan timeout)
    {
        return SpinWait.SpinUntil(
            () => EffectiveConfigRequestCount >= count && RemoteConfigStatusRequestCount >= count,
            timeout);
    }
}
#pragma warning restore CA1515 // Consider making public types internal. Needed for plugin loading.
