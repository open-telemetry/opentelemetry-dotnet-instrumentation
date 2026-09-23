// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.PluginApi;
using OpenTelemetry.AutoInstrumentation.PluginApi.OpAmp;
using OpenTelemetry.OpAmp.Client.Settings;

namespace OpenTelemetry.AutoInstrumentation.Tests.OpAmp.Fixtures;

#pragma warning disable CA1515 // Consider making public types internal. Needed for plugin loading.
public abstract class TestOpAmpPlugin : IPlugin, IOpAmpPlugin
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

public abstract class CountingOpAmpPlugin : TestOpAmpPlugin
{
    public int ConfigureOptionsCount { get; private set; }

    public int ConfigureClientCount { get; private set; }

    public OpAmpClientSettings? ConfiguredSettings { get; private set; }

    public IOpAmpClient? Client { get; private set; }

    public override void ConfigureOpAmpOptions(OpAmpClientSettings settings)
    {
        ConfigureOptionsCount++;
        ConfiguredSettings = settings;
    }

    public override void ConfigureOpAmpClient(IOpAmpClient client)
    {
        ConfigureClientCount++;
        Client = client;
    }
}
#pragma warning restore CA1515 // Consider making public types internal. Needed for plugin loading.
