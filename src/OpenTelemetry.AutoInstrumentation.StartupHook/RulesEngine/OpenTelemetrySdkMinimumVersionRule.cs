// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using OpenTelemetry.AutoInstrumentation.Logging;
using OpenTelemetry.AutoInstrumentation.Util;

namespace OpenTelemetry.AutoInstrumentation.RulesEngine;

internal class OpenTelemetrySdkMinimumVersionRule : Rule
{
    private const string OpenTelemetryAssemblyFileName = "OpenTelemetry.dll";
    private static IOtelLogger logger = OtelLogging.GetLogger("StartupHook");

    public OpenTelemetrySdkMinimumVersionRule()
    {
        Name = "OpenTelemetry SDK Validator";
        Description = "Ensure that the OpenTelemetry SDK version is not older than the version used by the Automatic Instrumentation";
    }

    // This constructor is used for test purpose.
    protected OpenTelemetrySdkMinimumVersionRule(IOtelLogger otelLogger)
        : this()
    {
        logger = otelLogger;
    }

    internal override bool Evaluate()
    {
        try
        {
            var loadedOTelFileVersion = GetVersionFromApp();
            if (loadedOTelFileVersion != null)
            {
                var autoInstrumentationOTelFileVersion = GetVersionFromAutoInstrumentation();
                if (loadedOTelFileVersion < autoInstrumentationOTelFileVersion)
                {
                    logger.Error($"Rule Engine: Application has direct or indirect reference to older version of OpenTelemetry package {loadedOTelFileVersion}.");
                    return false;
                }
            }
        }
        catch (Exception ex)
        {
            // Exception in evaluation should not throw or crash the process.
            logger.Information($"Rule Engine: Couldn't evaluate reference to OpenTelemetry Sdk in an app. Exception: {ex}");
            return true;
        }

        logger.Information("Rule Engine: OpenTelemetrySdkMinimumVersionRule evaluation success.");
        return true;
    }

    protected virtual Version? GetVersionFromApp()
    {
        // if customer application has reference to OpenTelemetry SDK, we can get the file from TPA list
        var openTelemetryLocation = TrustedPlatformAssembliesHelper.TpaPaths.FirstOrDefault(path => Path.GetFileName(path).Equals(OpenTelemetryAssemblyFileName, StringComparison.OrdinalIgnoreCase));
        if (openTelemetryLocation != null)
        {
            var openTelemetryFileVersionInfo = FileVersionInfo.GetVersionInfo(openTelemetryLocation);
            var openTelemetryFileVersion = new Version(openTelemetryFileVersionInfo.FileVersion);
            return openTelemetryFileVersion;
        }

        return null;
    }

    protected virtual Version? GetVersionFromAutoInstrumentation()
    {
        var openTelemetryLocation = ManagedProfilerLocationHelper.GetAssemblyPath(OpenTelemetryAssemblyFileName, logger);
        var openTelemetryFileVersionInfo = FileVersionInfo.GetVersionInfo(openTelemetryLocation);
        var openTelemetryFileVersion = new Version(openTelemetryFileVersionInfo.FileVersion);

        return openTelemetryFileVersion;
    }
}
