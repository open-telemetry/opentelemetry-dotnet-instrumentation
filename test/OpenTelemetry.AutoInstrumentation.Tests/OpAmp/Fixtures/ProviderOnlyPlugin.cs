// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Net.Http;
using OpenTelemetry.AutoInstrumentation.PluginApi.OpAmp;
using OpenTelemetry.OpAmp.Client.Messages;
using OpenTelemetry.OpAmp.Client.Settings;

namespace OpenTelemetry.AutoInstrumentation.Tests.OpAmp.Fixtures;

#pragma warning disable CA1515 // Consider making public types internal. Needed for plugin loading.
public sealed class ProviderOnlyPlugin : CountingOpAmpPlugin, IProvideEffectiveConfig, IProvideRemoteConfigStatus
{
    private readonly TaskCompletionSource<bool> _started = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _effectiveConfigRequestCount;
    private int _remoteConfigStatusRequestCount;

    public bool ObservedEffectiveConfigReporting { get; private set; }

    public bool ObservedRemoteConfigStatusReporting { get; private set; }

    public int EffectiveConfigRequestCount => Volatile.Read(ref _effectiveConfigRequestCount);

    public int RemoteConfigStatusRequestCount => Volatile.Read(ref _remoteConfigStatusRequestCount);

    public override void ConfigureOpAmpOptions(OpAmpClientSettings settings)
    {
        base.ConfigureOpAmpOptions(settings);
        ObservedEffectiveConfigReporting = settings.EffectiveConfigurationReporting.EnableReporting;
        ObservedRemoteConfigStatusReporting = settings.RemoteConfiguration.ReportsRemoteConfigStatus;
        settings.RemoteConfiguration.AcceptsRemoteConfig = true;
        settings.Heartbeat.IsEnabled = false;
        settings.HttpClientFactory = () => new HttpClient(new OpAmpTestHttpMessageHandler(), disposeHandler: true);
    }

    public override void AfterOpAmpClientStarted()
    {
        _started.TrySetResult(true);
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

    public bool WaitForStarted(TimeSpan timeout)
    {
        return _started.Task.Wait(timeout);
    }
}
#pragma warning restore CA1515 // Consider making public types internal. Needed for plugin loading.
