// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET || NETCOREAPP
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
    {
        foreach (var path in TrustedPlatformAssembliesHelper.TpaPaths)
        {
            var assemblyName = Path.GetFileNameWithoutExtension(path);
            if (assemblyName.Equals(AutoInstrumentationAssemblyName, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }
}
#endif
