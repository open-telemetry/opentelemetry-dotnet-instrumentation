// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.OpAmp.Client.Settings;

namespace OpenTelemetry.AutoInstrumentation.Tests.OpAmp.Fixtures;

#pragma warning disable CA1515 // Consider making public types internal. Needed for plugin loading.
public sealed class PrimaryOpAmpPlugin : CountingOpAmpPlugin
{
    public override void ConfigureOpAmpOptions(OpAmpClientSettings settings)
    {
        base.ConfigureOpAmpOptions(settings);
        settings.EffectiveConfigurationReporting.EnableReporting = true;
        settings.RemoteConfiguration.AcceptsRemoteConfig = true;
        settings.RemoteConfiguration.ReportsRemoteConfigStatus = true;
    }
}
#pragma warning restore CA1515 // Consider making public types internal. Needed for plugin loading.
