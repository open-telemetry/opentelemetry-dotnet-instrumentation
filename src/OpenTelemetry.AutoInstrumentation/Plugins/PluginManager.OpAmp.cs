// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.PluginApi.OpAmp;
using OpenTelemetry.OpAmp.Client;
using OpenTelemetry.OpAmp.Client.Settings;

namespace OpenTelemetry.AutoInstrumentation.Plugins;

internal partial class PluginManager
{
    public void ConfigureOpAmpOptions(OpAmpClientSettings options)
    {
        CallPlugins<IOpAmpPlugin>(plugin => plugin.ConfigureOpAmpOptions(options));
    }

    public void AfterOpAmpClientStarted(OpAmpClient client)
    {
        CallPlugins<IOpAmpPlugin>(plugin => plugin.AfterOpAmpClientStarted(client));
    }

    public void BeforeOpAmpClientStopped()
    {
        CallPlugins<IOpAmpPlugin>(plugin => plugin.BeforeOpAmpClientStopped());
    }
}
