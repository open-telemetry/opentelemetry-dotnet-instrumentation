// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.PluginApi;
using OpenTelemetry.AutoInstrumentation.PluginApi.OpAmp;
using OpenTelemetry.OpAmp.Client.Settings;

namespace TestApplication.Plugins;

#pragma warning disable CA1515 // Consider making public types internal. Needed for AutoInstrumentation plugin loading.
public sealed class SelectedOpAmpPlugin : IPlugin, IOpAmpPlugin
#pragma warning restore CA1515 // Consider making public types internal. Needed for AutoInstrumentation plugin loading.
{
    public void Initializing()
    {
        Console.WriteLine($"{nameof(SelectedOpAmpPlugin)}.{nameof(Initializing)}() invoked.");
    }

    public void Initialized()
    {
        Console.WriteLine($"{nameof(SelectedOpAmpPlugin)}.{nameof(Initialized)}() invoked.");
    }

    public void ConfigureOpAmpOptions(OpAmpClientSettings settings)
    {
        Console.WriteLine($"{nameof(SelectedOpAmpPlugin)}.{nameof(ConfigureOpAmpOptions)}() invoked.");
    }

    public void ConfigureOpAmpClient(IOpAmpClient client)
    {
        Console.WriteLine($"{nameof(SelectedOpAmpPlugin)}.{nameof(ConfigureOpAmpClient)}() invoked.");
    }

    public void AfterOpAmpClientStarted()
    {
        Console.WriteLine($"{nameof(SelectedOpAmpPlugin)}.{nameof(AfterOpAmpClientStarted)}() invoked.");
    }

    public void BeforeOpAmpClientStopped()
    {
        Console.WriteLine($"{nameof(SelectedOpAmpPlugin)}.{nameof(BeforeOpAmpClientStopped)}() invoked.");
    }
}
