// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.OpAmp.Client.Settings;

namespace OpenTelemetry.AutoInstrumentation.Tests.OpAmp.Fixtures;

#pragma warning disable CA1515 // Consider making public types internal. Needed for plugin loading.
public sealed class CustomMessageLimitsPlugin : TestOpAmpPlugin
{
    public const int OverrideMaxPendingCustomMessages = 789;
    public const int OverrideMaxPendingCustomMessageBytes = 1011;

    public int ObservedMaxPendingCustomMessages { get; private set; }

    public int ObservedMaxPendingCustomMessageBytes { get; private set; }

    public OpAmpClientSettings? ConfiguredSettings { get; private set; }

    public override void ConfigureOpAmpOptions(OpAmpClientSettings settings)
    {
        ObservedMaxPendingCustomMessages = settings.MaxPendingCustomMessages;
        ObservedMaxPendingCustomMessageBytes = settings.MaxPendingCustomMessageBytes;
        settings.MaxPendingCustomMessages = OverrideMaxPendingCustomMessages;
        settings.MaxPendingCustomMessageBytes = OverrideMaxPendingCustomMessageBytes;
        ConfiguredSettings = settings;
    }
}
#pragma warning restore CA1515 // Consider making public types internal. Needed for plugin loading.
