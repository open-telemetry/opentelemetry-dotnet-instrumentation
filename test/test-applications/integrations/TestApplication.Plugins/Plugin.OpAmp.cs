// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.PluginApi;
using OpenTelemetry.AutoInstrumentation.PluginApi.OpAmp;
using OpenTelemetry.OpAmp.Client;
using OpenTelemetry.OpAmp.Client.Settings;

namespace TestApplication.Plugins;

#pragma warning disable CA1515 // Consider making public types internal. Needed for AutoInstrumentation plugin loading.
/// <summary>
/// OpAMP extensions of the plugin.
/// </summary>
public partial class Plugin : IPlugin, IOpAmpPlugin
#pragma warning restore CA1515 // Consider making public types internal. Needed for AutoInstrumentation plugin loading.
{
    public void ConfigureOpAmpOptions(OpAmpClientSettings settings)
    {
        Console.WriteLine($"{nameof(Plugin)}.{nameof(ConfigureOpAmpOptions)}() invoked.");
    }

    public void AfterOpAmpClientStarted(OpAmpClient client)
    {
        Console.WriteLine($"{nameof(Plugin)}.{nameof(AfterOpAmpClientStarted)}() invoked.");
    }

    public void BeforeOpAmpClientStopped()
    {
        Console.WriteLine($"{nameof(Plugin)}.{nameof(BeforeOpAmpClientStopped)}() invoked.");
    }
}
