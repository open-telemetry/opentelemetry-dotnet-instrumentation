// <copyright file="NativeProfilerDiagnosticsRule.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

using OpenTelemetry.AutoInstrumentation.Helpers;
using OpenTelemetry.AutoInstrumentation.Logging;

namespace OpenTelemetry.AutoInstrumentation.RulesEngine;

/// <summary>
/// Diagnoses native profiler issues.
/// Diagnoser only supports .NET (Core) runtime.
/// </summary>
internal class NativeProfilerDiagnosticsRule : Rule
{
    private const string ProfilerEnabledVariable = "CORECLR_ENABLE_PROFILING";
    private const string ProfilerIdVariable = "CORECLR_PROFILER";
    private const string ProfilerPathVariable = "CORECLR_PROFILER_PATH";
    private const string Profiler32BitPathVariable = "CORECLR_PROFILER_PATH_32";
    private const string Profiler64BitPathVariable = "CORECLR_PROFILER_PATH_64";
    private const string ProfilerId = "{918728DD-259F-4A6A-AC2B-B85E1B658318}";

    private static readonly IOtelLogger Logger = OtelLogging.GetLogger("StartupHook");

    public NativeProfilerDiagnosticsRule()
    {
        Name = "Native profiler diagnoser";
        Description = "Verifies that native profiler is correctly setup in case it's enabled.";
    }

    internal override bool Evaluate()
    {
        var isProfilerEnabled = EnvironmentHelper.GetEnvironmentVariable(ProfilerEnabledVariable) == "1";
        if (isProfilerEnabled)
        {
            try
            {
                if (NativeMethods.IsProfilerAttached())
                {
                    return true;
                }
                else
                {
                    Logger.Debug("IsProfilerAttached returned false.");
                }
            }
            catch (Exception ex)
            {
                /* Native profiler is not attached. Continue with diagnosis */
                Logger.Debug(ex, "Error checking if native profiler is attached.");
            }
        }
        else
        {
            Logger.Warning("{0} environment variable is not set to '1'. The CLR Profiler is disabled and no bytecode instrumentations are going to be injected.", ProfilerEnabledVariable);
            return true;
        }

        var profilerId = EnvironmentHelper.GetEnvironmentVariable(ProfilerIdVariable);
        if (profilerId != ProfilerId)
        {
            Logger.Warning("The CLR profiler is enabled, but a different profiler ID was provided '{0}'.", profilerId);

            // Different native profiler not assosiated to OTel might be used. We don't want to fail here.
            return true;
        }

        if (Environment.Is64BitProcess)
        {
            Verify64BitVariables();
        }
        else
        {
            Verify32BitVariables();
        }

        return false;
    }

    private static void Verify32BitVariables()
    {
        VerifyVariables(Profiler32BitPathVariable, "32bit");
    }

    private static void Verify64BitVariables()
    {
        VerifyVariables(Profiler64BitPathVariable, "64bit");
    }

    private static void VerifyVariables(string archPathVariable, string expectedBitness)
    {
        var profilerPathGeneral = EnvironmentHelper.GetEnvironmentVariable(ProfilerPathVariable);
        var profilerPathArch = EnvironmentHelper.GetEnvironmentVariable(archPathVariable);

        var isPathUndefined = string.IsNullOrWhiteSpace(profilerPathGeneral);
        var isPathArchUndefined = string.IsNullOrWhiteSpace(profilerPathArch);

        if (isPathUndefined && isPathArchUndefined)
        {
            Logger.Error("CLR profiler path is not defined. Define '{0}' or '{1}'.", ProfilerPathVariable, archPathVariable);
            return;
        }

        var definedProfilerPath = isPathArchUndefined ? profilerPathGeneral : profilerPathArch;
        var definedProfilerVariable = isPathArchUndefined ? ProfilerPathVariable : archPathVariable;

        if (File.Exists(definedProfilerPath))
        {
            // File is found but profiler is not attaching.
            Logger.Error("CLR profiler is not attaching profiler found at '{0}'. Recheck that {1} process is attaching {1} native profiler via {2} or {3}.", new object[] { definedProfilerPath!, expectedBitness, ProfilerPathVariable, archPathVariable });
        }
        else
        {
            // File not found.
            Logger.Error("CLR profiler ({0}) is not found at '{1}'. Recheck '{2}'.", expectedBitness, definedProfilerPath, definedProfilerVariable);
        }
    }
}
