// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.PluginApi;
using OpenTelemetry.AutoInstrumentation.PluginApi.OpAmp;
using OpenTelemetry.OpAmp.Client.Settings;

namespace TestApplication.Plugins;

#pragma warning disable CA1515 // Consider making public types internal. Needed for AutoInstrumentation plugin loading.
/// <summary>
/// Common no-op OpAMP plugin lifecycle for capability tests.
/// </summary>
public abstract class OpAmpPluginBase : IPlugin, IOpAmpPlugin
#pragma warning restore CA1515 // Consider making public types internal. Needed for AutoInstrumentation plugin loading.
{
    public virtual void Initializing()
    {
    }

    public virtual void Initialized()
    {
    }

    public virtual void ConfigureOpAmpOptions(OpAmpClientSettings settings)
    {
    }

    public virtual void ConfigureOpAmpClient(IOpAmpClient client)
    {
    }

    public virtual void AfterOpAmpClientStarted()
    {
    }

    public virtual void BeforeOpAmpClientStopped()
    {
    }
}
