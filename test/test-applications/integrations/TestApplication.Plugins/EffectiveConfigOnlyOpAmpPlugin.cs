// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.PluginApi.OpAmp;
using OpenTelemetry.OpAmp.Client.Messages;
using OpenTelemetry.OpAmp.Client.Settings;

namespace TestApplication.Plugins;

#pragma warning disable CA1515 // Consider making public types internal. Needed for AutoInstrumentation plugin loading.
/// <summary>
/// An OpAMP plugin that only provides effective configuration.
/// </summary>
public sealed class EffectiveConfigOnlyOpAmpPlugin : OpAmpPluginBase, IProvideEffectiveConfig
#pragma warning restore CA1515 // Consider making public types internal. Needed for AutoInstrumentation plugin loading.
{
    public override void ConfigureOpAmpOptions(OpAmpClientSettings settings)
    {
        settings.EffectiveConfigurationReporting.EnableReporting = true;
    }

    public IReadOnlyCollection<EffectiveConfigFile> GetEffectiveConfig()
    {
        return [];
    }
}
