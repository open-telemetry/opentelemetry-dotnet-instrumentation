// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.PluginApi;

namespace TestApplication.Plugins;

#pragma warning disable CA1515 // Consider making public types internal. Needed for AutoInstrumentation plugin loading.
/// <summary>
/// Core of the plugin.
/// </summary>
public partial class Plugin : IPlugin
#pragma warning restore CA1515 // Consider making public types internal. Needed for AutoInstrumentation plugin loading.
{
    public void Initializing()
    {
        Console.WriteLine($"{nameof(Plugin)}.{nameof(Initializing)}() invoked.");
    }

    public void Initialized()
    {
        Console.WriteLine($"{nameof(Plugin)}.{nameof(Initialized)}() invoked.");
    }
}
