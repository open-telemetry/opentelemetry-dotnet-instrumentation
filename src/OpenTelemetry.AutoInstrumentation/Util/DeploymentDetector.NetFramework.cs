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
    /// of the OpenTelemetry.AutoInstrumentation assembly in all expected locations.
    /// </summary>
    public static bool IsStandaloneDeployment(IOtelLogger logger)
    {
        var path = ManagedProfilerLocationHelper.GetAssemblyPath(AutoInstrumentationAssemblyName, logger);
        if (path == null)
        {
            logger.Information($"Detected Non-Standalone Deployment. {AutoInstrumentationAssemblyName} not found at any expected location.");
            return false;
        }

        logger.Information($"Detected Standalone Deployment.{AutoInstrumentationAssemblyName} found at \"{path}\"");
        return true;
    }
}
#endif
