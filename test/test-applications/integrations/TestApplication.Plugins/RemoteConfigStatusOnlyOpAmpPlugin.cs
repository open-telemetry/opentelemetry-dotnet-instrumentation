// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.PluginApi.OpAmp;
using OpenTelemetry.OpAmp.Client.Messages;
using OpenTelemetry.OpAmp.Client.Settings;

namespace TestApplication.Plugins;

#pragma warning disable CA1515 // Consider making public types internal. Needed for AutoInstrumentation plugin loading.
/// <summary>
/// An OpAMP plugin that only provides remote configuration status.
/// </summary>
public sealed class RemoteConfigStatusOnlyOpAmpPlugin : OpAmpPluginBase, IProvideRemoteConfigStatus
#pragma warning restore CA1515 // Consider making public types internal. Needed for AutoInstrumentation plugin loading.
{
    private const string ReportingEnabledEnvironmentVariable = "TEST_OPAMP_REMOTE_CONFIG_STATUS_REPORTING_ENABLED";

    public override void ConfigureOpAmpOptions(OpAmpClientSettings settings)
    {
        settings.RemoteConfiguration.ReportsRemoteConfigStatus = string.Equals(
            Environment.GetEnvironmentVariable(ReportingEnabledEnvironmentVariable),
            "true",
            StringComparison.OrdinalIgnoreCase);
    }

    public RemoteConfigStatusReport? GetRemoteConfigStatus()
    {
        return new RemoteConfigStatusReport("temporary"u8, RemoteConfigStatusCode.Unset);
    }
}
