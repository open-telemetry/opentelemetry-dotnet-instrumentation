// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Util;

namespace OpenTelemetry.AutoInstrumentation;

internal static class DeploymentDetector
{
    private const string AutoInstrumentationAssemblyName = "OpenTelemetry.AutoInstrumentation";

    /// <summary>
    /// Detects whether the current deployment is standalone or not by checking if the
    /// OpenTelemetry.AutoInstrumentation assembly is in the Trusted Platform Assemblies list.
    /// When NOT present in TPA, it's a standalone deployment.
    /// When present in TPA, it was automatically added (e.g., NuGet-based deployment).
    /// </summary>
    public static bool IsStandaloneDeployment()
        => !TrustedPlatformAssembliesHelper.TpaPaths.Any(it =>
            string.Equals(
                Path.GetFileNameWithoutExtension(it),
                AutoInstrumentationAssemblyName,
                StringComparison.OrdinalIgnoreCase));
}
