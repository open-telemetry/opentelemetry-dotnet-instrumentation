// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Net.Http;
using OpenTelemetry.AutoInstrumentation.PluginApi.OpAmp;
using OpenTelemetry.OpAmp.Client.Messages;
using OpenTelemetry.OpAmp.Client.Settings;

namespace OpenTelemetry.AutoInstrumentation.Tests.OpAmp.Fixtures;

#pragma warning disable CA1515 // Consider making public types internal. Needed for plugin loading.
public sealed class BlockingEffectiveConfigPlugin : TestOpAmpPlugin, IProvideEffectiveConfig
{
    private readonly TaskCompletionSource<bool> _requestEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource<bool> _releaseRequest = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource<bool> _started = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource<bool> _beforeStopped = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _beforeStoppedCount;
    private int _requestCount;

    public IOpAmpClient? Client { get; private set; }

    public int BeforeStoppedCount => Volatile.Read(ref _beforeStoppedCount);

    public int RequestCount => Volatile.Read(ref _requestCount);

    public bool ServerAcceptsEffectiveConfig { get; set; }

    public override void ConfigureOpAmpOptions(OpAmpClientSettings settings)
    {
        settings.EffectiveConfigurationReporting.EnableReporting = true;
        settings.Heartbeat.IsEnabled = false;
        settings.HttpClientFactory = () => new HttpClient(
            new OpAmpTestHttpMessageHandler(
                () => ServerAcceptsEffectiveConfig
                    ? [0x38, 0x05]
                    : []),
            disposeHandler: true);
    }

    public override void ConfigureOpAmpClient(IOpAmpClient client)
    {
        Client = client;
    }

    public override void AfterOpAmpClientStarted()
    {
        _started.TrySetResult(true);
    }

    public override void BeforeOpAmpClientStopped()
    {
        Interlocked.Increment(ref _beforeStoppedCount);
        _beforeStopped.TrySetResult(true);
    }

    public IReadOnlyCollection<EffectiveConfigFile> GetEffectiveConfig()
    {
        Interlocked.Increment(ref _requestCount);
        _requestEntered.TrySetResult(true);
        _releaseRequest.Task.GetAwaiter().GetResult();
        return [];
    }

    public bool WaitForRequest(TimeSpan timeout)
    {
        return _requestEntered.Task.Wait(timeout);
    }

    public bool WaitForStarted(TimeSpan timeout)
    {
        return _started.Task.Wait(timeout);
    }

    public bool WaitForBeforeStopped(TimeSpan timeout)
    {
        return _beforeStopped.Task.Wait(timeout);
    }

    public void ReleaseRequest()
    {
        _releaseRequest.TrySetResult(true);
    }
}
#pragma warning restore CA1515 // Consider making public types internal. Needed for plugin loading.
