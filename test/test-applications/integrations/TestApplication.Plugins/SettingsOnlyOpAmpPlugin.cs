// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.PluginApi;
using OpenTelemetry.AutoInstrumentation.PluginApi.OpAmp;
using OpenTelemetry.OpAmp.Client.Settings;

namespace TestApplication.Plugins;

#pragma warning disable CA1515 // Consider making public types internal. Needed for AutoInstrumentation plugin loading.
public sealed class SettingsOnlyOpAmpPlugin : IPlugin, IOpAmpPlugin
#pragma warning restore CA1515 // Consider making public types internal. Needed for AutoInstrumentation plugin loading.
{
    public void Initializing()
    {
        Console.WriteLine($"{nameof(SettingsOnlyOpAmpPlugin)}.{nameof(Initializing)}() invoked.");
    }

    public void Initialized()
    {
        Console.WriteLine($"{nameof(SettingsOnlyOpAmpPlugin)}.{nameof(Initialized)}() invoked.");
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

        settings.RemoteConfiguration.AcceptsRemoteConfig = true;
        Console.WriteLine($"{nameof(SettingsOnlyOpAmpPlugin)}.{nameof(ConfigureOpAmpOptions)}() invoked.");
    }

    public void ConfigureOpAmpClient(IOpAmpClient client)
    {
        Console.WriteLine($"{nameof(SettingsOnlyOpAmpPlugin)}.{nameof(ConfigureOpAmpClient)}() invoked.");
    }

    public void AfterOpAmpClientStarted()
    {
        Console.WriteLine($"{nameof(SettingsOnlyOpAmpPlugin)}.{nameof(AfterOpAmpClientStarted)}() invoked.");
    }

    public void BeforeOpAmpClientStopped()
    {
        Console.WriteLine($"{nameof(SettingsOnlyOpAmpPlugin)}.{nameof(BeforeOpAmpClientStopped)}() invoked.");
    }
}
