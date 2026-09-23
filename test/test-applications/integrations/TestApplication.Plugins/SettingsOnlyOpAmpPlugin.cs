// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.PluginApi.OpAmp;
using OpenTelemetry.OpAmp.Client.Settings;

namespace TestApplication.Plugins;

#pragma warning disable CA1515 // Consider making public types internal. Needed for AutoInstrumentation plugin loading.
/// <summary>
/// An OpAMP plugin that attempts to enable reporting without providing the corresponding state.
/// </summary>
public sealed class SettingsOnlyOpAmpPlugin : OpAmpPluginBase
#pragma warning restore CA1515 // Consider making public types internal. Needed for AutoInstrumentation plugin loading.
{
    public override void Initializing()
    {
        Console.WriteLine($"{nameof(SettingsOnlyOpAmpPlugin)}.{nameof(Initializing)}() invoked.");
    }

    public override void Initialized()
    {
        Console.WriteLine($"{nameof(SettingsOnlyOpAmpPlugin)}.{nameof(Initialized)}() invoked.");
    }

    public override void ConfigureOpAmpOptions(OpAmpClientSettings settings)
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
        settings.RemoteConfiguration.AcceptsRemoteConfig = true;
        settings.RemoteConfiguration.ReportsRemoteConfigStatus = true;
        Console.WriteLine($"{nameof(SettingsOnlyOpAmpPlugin)}.{nameof(ConfigureOpAmpOptions)}() invoked.");
    }

    public override void ConfigureOpAmpClient(IOpAmpClient client)
    {
        Console.WriteLine($"{nameof(SettingsOnlyOpAmpPlugin)}.{nameof(ConfigureOpAmpClient)}() invoked.");
    }

    public override void AfterOpAmpClientStarted()
    {
        Console.WriteLine($"{nameof(SettingsOnlyOpAmpPlugin)}.{nameof(AfterOpAmpClientStarted)}() invoked.");
    }

    public override void BeforeOpAmpClientStopped()
    {
        Console.WriteLine($"{nameof(SettingsOnlyOpAmpPlugin)}.{nameof(BeforeOpAmpClientStopped)}() invoked.");
    }
}
