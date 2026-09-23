// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Text;
using OpenTelemetry.AutoInstrumentation.PluginApi;
using OpenTelemetry.AutoInstrumentation.PluginApi.OpAmp;
using OpenTelemetry.OpAmp.Client.Messages;
using OpenTelemetry.OpAmp.Client.Settings;

namespace IntegrationTests.Helpers;

#pragma warning disable CA1515 // Consider making public types internal. Needed for plugin loading.
public sealed class StateReportingOpAmpPlugin : IPlugin, IOpAmpPlugin, IProvideEffectiveConfig, IProvideRemoteConfigStatus
#pragma warning restore CA1515 // Consider making public types internal. Needed for plugin loading.
{
    private readonly TaskCompletionSource<bool> _effectiveConfigRequestEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource<bool> _releaseEffectiveConfigRequest = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource<bool> _started = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public StateReportingOpAmpPlugin()
    {
        EffectiveConfigState =
        [
            CreateEffectiveConfigFile(string.Empty, "one"),
            CreateEffectiveConfigFile("second", "two")
        ];
        RemoteConfigStatusState = new RemoteConfigStatusReport(
            Encoding.UTF8.GetBytes("initial-hash"),
            RemoteConfigStatusCode.Applied);
    }

    public IOpAmpClient Client { get; private set; } = null!;

    public bool BlockEffectiveConfigRequest { get; set; }

    public IReadOnlyCollection<EffectiveConfigFile> EffectiveConfigState { get; set; }

    public RemoteConfigStatusReport? RemoteConfigStatusState { get; set; }

    public bool ThrowOnStateRequest { get; set; }

    public bool ReturnInvalidEffectiveConfig { get; set; }

    public void Initializing()
    {
    }

    public void Initialized()
    {
    }

    public void ConfigureOpAmpOptions(OpAmpClientSettings settings)
    {
#if NET
        ArgumentNullException.ThrowIfNull(settings);
#else
        if (settings == null)
        {
            throw new ArgumentNullException(nameof(settings));
        }
#endif

        settings.EffectiveConfigurationReporting.EnableReporting = true;
        settings.RemoteConfiguration.ReportsRemoteConfigStatus = true;
    }

    public void ConfigureOpAmpClient(IOpAmpClient client)
    {
        Client = client;
    }

    public void AfterOpAmpClientStarted()
    {
        _started.TrySetResult(true);
    }

    public void BeforeOpAmpClientStopped()
    {
    }

    public IReadOnlyCollection<EffectiveConfigFile> GetEffectiveConfig()
    {
        if (BlockEffectiveConfigRequest)
        {
            _effectiveConfigRequestEntered.TrySetResult(true);
            _releaseEffectiveConfigRequest.Task.GetAwaiter().GetResult();
        }

        if (ThrowOnStateRequest)
        {
            throw new InvalidOperationException("Test effective configuration failure.");
        }

        if (ReturnInvalidEffectiveConfig)
        {
            return Enumerable.Range(0, 17)
                .Select(index => CreateEffectiveConfigFile($"invalid-{index}", "content"))
                .ToArray();
        }

        return EffectiveConfigState;
    }

    public RemoteConfigStatusReport? GetRemoteConfigStatus()
    {
        if (ThrowOnStateRequest)
        {
            throw new InvalidOperationException("Test remote configuration status failure.");
        }

        return RemoteConfigStatusState;
    }

    public bool WaitForStarted(TimeSpan timeout)
    {
        return _started.Task.Wait(timeout);
    }

    public bool WaitForEffectiveConfigRequest(TimeSpan timeout)
    {
        return _effectiveConfigRequestEntered.Task.Wait(timeout);
    }

    public void ReleaseEffectiveConfigRequest()
    {
        _releaseEffectiveConfigRequest.TrySetResult(true);
    }

    private static EffectiveConfigFile CreateEffectiveConfigFile(string fileName, string content)
    {
        return new EffectiveConfigFile(Encoding.UTF8.GetBytes(content), "text/plain", fileName);
    }
}
