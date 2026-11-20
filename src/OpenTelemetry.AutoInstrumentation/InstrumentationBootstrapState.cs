// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation;

internal static class InstrumentationBootstrapState
{
    // AppDomain data shares the failure marker between separate copies of this type.
    // On .NET Framework, each AppDomain has its own marker.
    private const string DisabledDataName = "OpenTelemetry.AutoInstrumentation.Bootstrap.Disabled";

    public static bool IsDisabled => AppDomain.CurrentDomain.GetData(DisabledDataName) is true;

    public static void Disable() => AppDomain.CurrentDomain.SetData(DisabledDataName, true);
}
