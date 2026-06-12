// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET || NETCOREAPP
using OpenTelemetry.AutoInstrumentation.Logging;
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
    public static bool IsStandaloneDeployment(IOtelLogger? logger = null)
    {
        foreach (var path in TrustedPlatformAssembliesHelper.TpaPaths)
        {
            var assemblyName = Path.GetFileNameWithoutExtension(path);
            if (assemblyName.Equals(AutoInstrumentationAssemblyName, StringComparison.OrdinalIgnoreCase))
            {
                logger?.Information($"Detected non-standalone (e.g., NuGet-based) deployment: {AutoInstrumentationAssemblyName} found in Trusted Platform Assemblies.");
                return false;
            }
        }

        logger?.Information($"Detected standalone deployment: {AutoInstrumentationAssemblyName} is NOT found in Trusted Platform Assemblies.");
        return true;
    }
}
#endif
