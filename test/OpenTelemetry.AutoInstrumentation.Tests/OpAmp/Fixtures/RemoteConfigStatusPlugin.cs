// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.PluginApi.OpAmp;
using OpenTelemetry.OpAmp.Client.Messages;
using OpenTelemetry.OpAmp.Client.Settings;

namespace OpenTelemetry.AutoInstrumentation.Tests.OpAmp.Fixtures;

#pragma warning disable CA1515 // Consider making public types internal. Needed for plugin loading.
public class RemoteConfigStatusPlugin : CountingOpAmpPlugin, IProvideRemoteConfigStatus
{
    public RemoteConfigStatusReport? GetRemoteConfigStatus()
    {
        return new RemoteConfigStatusReport(new byte[] { 1 }, RemoteConfigStatusCode.Unset);
    }
}

public sealed class RemoteConfigStatusReportingPlugin : RemoteConfigStatusPlugin
{
    public override void ConfigureOpAmpOptions(OpAmpClientSettings settings)
    {
        base.ConfigureOpAmpOptions(settings);
        settings.RemoteConfiguration.ReportsRemoteConfigStatus = true;
    }
}
#pragma warning restore CA1515 // Consider making public types internal. Needed for plugin loading.
