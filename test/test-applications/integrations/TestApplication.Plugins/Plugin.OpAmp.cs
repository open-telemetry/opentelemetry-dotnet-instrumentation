// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Text;
using OpenTelemetry.AutoInstrumentation.PluginApi;
using OpenTelemetry.AutoInstrumentation.PluginApi.OpAmp;
using OpenTelemetry.OpAmp.Client.Messages;
using OpenTelemetry.OpAmp.Client.Settings;

namespace TestApplication.Plugins;

#pragma warning disable CA1515 // Consider making public types internal. Needed for AutoInstrumentation plugin loading.
/// <summary>
/// OpAMP extensions of the plugin.
/// </summary>
public partial class Plugin : IPlugin, IProvideEffectiveConfig, IProvideRemoteConfigStatus
#pragma warning restore CA1515 // Consider making public types internal. Needed for AutoInstrumentation plugin loading.
{
    private IOpAmpClient? _opAmpClient;
    private string _effectiveConfig = "initial: true";
    private RemoteConfigStatusReport? _remoteConfigStatus;

    public void ConfigureOpAmpOptions(OpAmpClientSettings settings)
    {
        ThrowIfMissing(settings);
        Console.WriteLine($"{nameof(Plugin)}.{nameof(ConfigureOpAmpOptions)}() invoked.");
        Console.WriteLine($"{nameof(settings.MaxPendingCustomMessages)}: {settings.MaxPendingCustomMessages}");
        Console.WriteLine($"{nameof(settings.MaxPendingCustomMessageBytes)}: {settings.MaxPendingCustomMessageBytes}");
        settings.EffectiveConfigurationReporting.EnableReporting = true;
        settings.RemoteConfiguration.AcceptsRemoteConfig = true;
        settings.RemoteConfiguration.ReportsRemoteConfigStatus = true;
    }

    public void ConfigureOpAmpClient(IOpAmpClient client)
    {
        ThrowIfMissing(client);
        Console.WriteLine($"{nameof(Plugin)}.{nameof(ConfigureOpAmpClient)}() invoked.");
        _opAmpClient = client;
        client.NotifyEffectiveConfigChanged();
        client.NotifyRemoteConfigStatusChanged();
    }

    public void AfterOpAmpClientStarted()
    {
        Console.WriteLine($"{nameof(Plugin)}.{nameof(AfterOpAmpClientStarted)}() invoked.");
        Volatile.Write(ref _effectiveConfig, "updated: true");
        _opAmpClient!.NotifyEffectiveConfigChanged();
        Volatile.Write(ref _remoteConfigStatus, new RemoteConfigStatusReport(Encoding.UTF8.GetBytes("config-hash"), RemoteConfigStatusCode.Applied));
        _opAmpClient.NotifyRemoteConfigStatusChanged();
    }

    public void BeforeOpAmpClientStopped()
    {
        Console.WriteLine($"{nameof(Plugin)}.{nameof(BeforeOpAmpClientStopped)}() invoked.");
    }

    public IReadOnlyCollection<EffectiveConfigFile> GetEffectiveConfig()
    {
        Console.WriteLine($"{nameof(Plugin)}.{nameof(GetEffectiveConfig)}() invoked.");
        return [new EffectiveConfigFile(Encoding.UTF8.GetBytes(Volatile.Read(ref _effectiveConfig)), "text/yaml", "config.yaml")];
    }

    public RemoteConfigStatusReport? GetRemoteConfigStatus()
    {
        Console.WriteLine($"{nameof(Plugin)}.{nameof(GetRemoteConfigStatus)}() invoked.");
        return Volatile.Read(ref _remoteConfigStatus);
    }
}
