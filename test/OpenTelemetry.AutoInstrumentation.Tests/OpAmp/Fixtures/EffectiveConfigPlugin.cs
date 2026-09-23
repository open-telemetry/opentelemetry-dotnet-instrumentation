// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.PluginApi.OpAmp;
using OpenTelemetry.OpAmp.Client.Messages;
using OpenTelemetry.OpAmp.Client.Settings;

namespace OpenTelemetry.AutoInstrumentation.Tests.OpAmp.Fixtures;

#pragma warning disable CA1515 // Consider making public types internal. Needed for plugin loading.
public class EffectiveConfigPlugin : CountingOpAmpPlugin, IProvideEffectiveConfig
{
    public IReadOnlyCollection<EffectiveConfigFile> GetEffectiveConfig()
    {
        return [];
    }
}

public sealed class EffectiveConfigReportingPlugin : EffectiveConfigPlugin
{
    public override void ConfigureOpAmpOptions(OpAmpClientSettings settings)
    {
        base.ConfigureOpAmpOptions(settings);
        settings.EffectiveConfigurationReporting.EnableReporting = true;
    }
}
#pragma warning restore CA1515 // Consider making public types internal. Needed for plugin loading.
