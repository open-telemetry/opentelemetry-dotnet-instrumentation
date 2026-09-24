// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.PluginApi;
using OpenTelemetry.AutoInstrumentation.PluginApi.OpAmp;
using OpenTelemetry.OpAmp.Client.Listeners;
using OpenTelemetry.OpAmp.Client.Messages;
using OpenTelemetry.OpAmp.Client.Settings;

namespace TestApplication.Plugins;

#pragma warning disable CA1515 // Consider making public types internal. Needed for AutoInstrumentation plugin loading.
/// <summary>
/// OpAMP extensions of the plugin.
/// </summary>
public partial class Plugin : IPlugin, IOpAmpPlugin, IOpAmpListener<CustomMessageMessage>
#pragma warning restore CA1515 // Consider making public types internal. Needed for AutoInstrumentation plugin loading.
{
    private IOpAmpClient? _opAmpClient;

    public void ConfigureOpAmpOptions(OpAmpClientSettings settings)
    {
        ThrowIfMissing(settings);
        Console.WriteLine($"{nameof(Plugin)}.{nameof(ConfigureOpAmpOptions)}() invoked.");
        Console.WriteLine($"{nameof(settings.MaxPendingCustomMessages)}: {settings.MaxPendingCustomMessages}");
        Console.WriteLine($"{nameof(settings.MaxPendingCustomMessageBytes)}: {settings.MaxPendingCustomMessageBytes}");
    }

    public void ConfigureOpAmpClient(IOpAmpClient client)
    {
        ThrowIfMissing(client);
        _opAmpClient = client;
        client.Subscribe<CustomMessageMessage>(this);
        Console.WriteLine($"{nameof(Plugin)}.{nameof(ConfigureOpAmpClient)}() invoked.");
    }

    public void AfterOpAmpClientStarted()
    {
        Console.WriteLine($"{nameof(Plugin)}.{nameof(AfterOpAmpClientStarted)}() invoked.");
    }

    public void BeforeOpAmpClientStopped()
    {
        _opAmpClient?.Unsubscribe<CustomMessageMessage>(this);
        _opAmpClient = null;
        Console.WriteLine($"{nameof(Plugin)}.{nameof(BeforeOpAmpClientStopped)}() invoked.");
    }

    public void HandleMessage(CustomMessageMessage message)
    {
        ThrowIfMissing(message);
        Console.WriteLine($"{nameof(Plugin)}.{nameof(HandleMessage)}({nameof(CustomMessageMessage)}) invoked: {message.Type}.");
    }
}
