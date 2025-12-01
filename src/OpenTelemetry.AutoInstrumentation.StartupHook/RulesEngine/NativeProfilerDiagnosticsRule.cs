// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Helpers;
using OpenTelemetry.AutoInstrumentation.Logging;

using static OpenTelemetry.AutoInstrumentation.Constants.EnvironmentVariables;

namespace OpenTelemetry.AutoInstrumentation.RulesEngine;

/// <summary>
/// Diagnoses native profiler issues.
/// Diagnoser only supports .NET (Core) runtime.
/// </summary>
internal class NativeProfilerDiagnosticsRule : Rule
{
    private static readonly IOtelLogger Logger = OtelLogging.GetLogger("StartupHook");

    public NativeProfilerDiagnosticsRule()
    {
        Name = "Native profiler diagnoser";
        Description = "Verifies that native profiler is correctly setup in case it's enabled.";
    }

    internal override bool Evaluate()
    {
        var isProfilerEnabled = EnvironmentHelper.GetEnvironmentVariable(ProfilerEnabledVariable) == "1";
        if (!isProfilerEnabled)
        {
            Logger.Warning("{0} environment variable is not set to '1'. The CLR Profiler is disabled and no bytecode instrumentations are going to be injected.", ProfilerEnabledVariable);
            return true;
        }

        var profilerId = EnvironmentHelper.GetEnvironmentVariable(ProfilerIdVariable);
        if (profilerId != ProfilerId)
        {
            Logger.Warning("The CLR profiler is enabled, but a different profiler ID was provided '{0}'.", profilerId);

            // Different native profiler not associated to OTel might be used. We don't want to fail here.
            return true;
        }

        try
        {
            if (NativeMethods.IsProfilerAttached())
            {
                return true;
            }

            Logger.Error("IsProfilerAttached returned false, the native log should describe the root cause.");
        }
#pragma warning disable CA1031 // Do not catch general exception
        catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception
        {
            /* Native profiler is not attached. Continue with diagnosis */
            Logger.Debug(ex, "Error checking if native profiler is attached.");
        }

        if (Environment.Is64BitProcess)
        {
            VerifyPathVariables(Profiler64BitPathVariable, "64bit");
        }
        else
        {
            VerifyPathVariables(Profiler32BitPathVariable, "32bit");
        }

        return false;
    }

    private static void VerifyPathVariables(string archPathVariable, string expectedBitness)
    {
        if (TryPathVariable(archPathVariable, expectedBitness))
        {
            return;
        }

        if (TryPathVariable(ProfilerPathVariable, expectedBitness))
        {
            return;
        }

        Logger.Error("CLR profiler path is not defined. Define '{0}' or '{1}'.", ProfilerPathVariable, archPathVariable);
    }

    private static bool TryPathVariable(string profilerPathVariable, string expectedBitness)
    {
        var profilerPath = EnvironmentHelper.GetEnvironmentVariable(profilerPathVariable);

        // Nothing to verify. Signal that VerifyVariables can continue searching for issues.
        if (string.IsNullOrWhiteSpace(profilerPath))
        {
            return false;
        }

        if (File.Exists(profilerPath))
        {
            // File is found but profiler is not attaching.
            Logger.Error("CLR profiler was not correctly loaded into the process. Profiler found at '{0}'. Recheck that {1} process is attaching {1} native profiler via {2}.", new object[] { profilerPath, expectedBitness, profilerPathVariable });
        }
        else
        {
            // File not found.
            Logger.Error("CLR profiler ({0}) is not found at '{1}'. Recheck '{2}'.", expectedBitness, profilerPath, profilerPathVariable);
        }

        // Path issue verified. VerifyVariables should not continue.
        return true;
    }
}
