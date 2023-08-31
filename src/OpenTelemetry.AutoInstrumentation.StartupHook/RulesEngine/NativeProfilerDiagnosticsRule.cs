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
        if (!isProfilerEnabled)
        {
            Logger.Debug($"CLR profiler is not enabled.");
            return true;
        }

        var profilerId = EnvironmentHelper.GetEnvironmentVariable(ProfilerIdVariable);
        if (profilerId != ProfilerId)
        {
            Logger.Warning($"Detected CLR profiler is enabled but different profiler ID is provided '{profilerId}'.");

            // Different native profiler not assosiated to OTel might be used. We don't want to fail here.
            return true;
        }

        var profilerPath = EnvironmentHelper.GetEnvironmentVariable(ProfilerPathVariable);
        var profilerPath32 = EnvironmentHelper.GetEnvironmentVariable(Profiler32BitPathVariable);
        var profilerPath64 = EnvironmentHelper.GetEnvironmentVariable(Profiler64BitPathVariable);

        var isPathUndefined = string.IsNullOrWhiteSpace(profilerPath);
        var isPath32Undefined = string.IsNullOrWhiteSpace(profilerPath32);
        var isPath64Undefined = string.IsNullOrWhiteSpace(profilerPath64);

        if (isPathUndefined)
        {
            if (isPath32Undefined && isPath64Undefined)
            {
                Logger.Error($"CLR profiler path is not defined. Define '{ProfilerPathVariable}', '{Profiler32BitPathVariable}' or '{Profiler64BitPathVariable}'.");
                return false;
            }

            if (Environment.Is64BitProcess && isPath64Undefined)
            {
                Logger.Error($"CLR profiler (64bit) path is not defined. Define '{Profiler64BitPathVariable}'.");
                return false;
            }

            if (!Environment.Is64BitProcess && isPath32Undefined)
            {
                Logger.Error($"CLR profiler (32bit) path is not defined. Define '{Profiler32BitPathVariable}'.");
                return false;
            }
        }

        if (!isPathUndefined && !File.Exists(profilerPath))
        {
            Logger.Error($"CLR profiler is not found at '{profilerPath}'. Recheck '{ProfilerPathVariable}'.");
            return false;
        }

        if (!isPath64Undefined && !File.Exists(profilerPath64))
        {
            Logger.Error($"CLR profiler (64bit) is not found at '{profilerPath64}'. Recheck '{Profiler64BitPathVariable}'.");
            return false;
        }

        if (!isPath32Undefined && !File.Exists(profilerPath32))
        {
            Logger.Error($"CLR profiler (32bit) is not found at '{profilerPath32}'. Recheck '{Profiler32BitPathVariable}'.");
            return false;
        }

        // TODO: Maybe it's possible to check if located profiler dll and process bitnesses are matching.

        return true;
    }
}
