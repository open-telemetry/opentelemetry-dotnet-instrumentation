// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Logging;

namespace OpenTelemetry.AutoInstrumentation.RulesEngine;

internal class MinSupportedFrameworkRule : Rule
{
    private static readonly IOtelLogger Logger = OtelLogging.GetLogger("StartupHook");

    public MinSupportedFrameworkRule()
    {
        Name = "Minimum Supported Framework Version Validator";
        Description = "Verifies that the application is running on a supported version of the .NET runtime.";
    }

    internal override bool Evaluate()
    {
        Version minRequiredFrameworkVersion = new(6, 0);
        var frameworkVersion = Environment.Version;
        if (frameworkVersion < minRequiredFrameworkVersion)
        {
            Logger.Information($"Rule Engine: Error in StartupHook initialization: {frameworkVersion} is not supported");
            return false;
        }

        Logger.Information("Rule Engine: MinSupportedFrameworkRule evaluation success.");
        return true;
    }
}
