// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NETFRAMEWORK
using OpenTelemetry.AutoInstrumentation.Logging;
using OpenTelemetry.AutoInstrumentation.Util;

namespace OpenTelemetry.AutoInstrumentation;

internal static class DeploymentDetector
{
    private const string AutoInstrumentationAssemblyName = "OpenTelemetry.AutoInstrumentation";

    /// <summary>
    /// Detects whether the current deployment is standalone or not by checking the location
    /// of the OpenTelemetry.AutoInstrumentation assembly.
    /// </summary>
    public static bool IsStandaloneDeployment(IOtelLogger logger)
    {
        var location = ManagedProfilerLocationHelper.FindAssembly(AutoInstrumentationAssemblyName, logger);

        if (location == null)
        {
            logger.Information($"{AutoInstrumentationAssemblyName} not found at any expected location, treating as non-standalone.");
            return false;
        }

        logger.Information($"Detected IsStandaloneDeployment: {location.Value.IsStandalone}. {AutoInstrumentationAssemblyName} found at \"{location.Value.Path}\"");
        return location.Value.IsStandalone;
    }
}
#endif
