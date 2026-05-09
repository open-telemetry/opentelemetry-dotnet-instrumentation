// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.OpAmp.Client;
using OpenTelemetry.OpAmp.Client.Settings;

namespace TestApplication.Plugins;

/// <summary>
/// OpAmp partial of the plugin.
/// </summary>
public partial class Plugin
{
    public void ConfigureOpAmpOptions(OpAmpClientSettings options)
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
