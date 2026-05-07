// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.OpAmp.Client;
using OpenTelemetry.OpAmp.Client.Settings;

namespace OpenTelemetry.AutoInstrumentation.Plugins;

internal partial class PluginManager
{
    public void ConfigureOpAmpOptions(OpAmpClientSettings options)
    {
        CallPlugins("ConfigureOpAmpOptions", (typeof(OpAmpClientSettings), options));
    }

    public void AfterOpAmpClientStarted(OpAmpClient client)
    {
        CallPlugins("AfterOpAmpClientStarted", (typeof(OpAmpClient), client));
    }

    public void BeforeOpAmpClientStopped()
    {
        CallPlugins("BeforeOpAmpClientStopped");
    }
}
