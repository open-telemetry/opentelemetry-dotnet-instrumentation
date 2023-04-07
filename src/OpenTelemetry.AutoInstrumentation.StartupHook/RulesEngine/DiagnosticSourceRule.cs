// <copyright file="DiagnosticSourceRule.cs" company="OpenTelemetry Authors">
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

using System.Diagnostics;
using System.Reflection;
using OpenTelemetry.AutoInstrumentation.Logging;

namespace OpenTelemetry.AutoInstrumentation.RulesEngine;

internal class DiagnosticSourceRule : Rule
{
    private static readonly IOtelLogger Logger = OtelLogging.GetLogger("StartupHook");

    public DiagnosticSourceRule()
    {
        Name = "System.Diagnostics.DiagnosticSource Validator";
        Description = "Ensure that the System.Diagnostics.DiagnosticSource version is not older than the version used by the Auto-Instrumentation";
    }

    internal override bool Evaluate()
    {
        string? olderDiagnosticSourcePackageVersion = null;

        try
        {
            // Look up for type with an assembly name, will load the library.
            // The loaded version depends on the app's reference to the package.
            var diagnosticSourceType = Type.GetType("System.Diagnostics.DiagnosticSource, System.Diagnostics.DiagnosticSource");
            if (diagnosticSourceType != null)
            {
                var loadedDiagnosticSourceAssembly = Assembly.GetAssembly(diagnosticSourceType);
                var loadedDiagnosticSourceAssemblyFileVersionAttribute = loadedDiagnosticSourceAssembly?.GetCustomAttribute<AssemblyFileVersionAttribute>();
                if (loadedDiagnosticSourceAssemblyFileVersionAttribute != null)
                {
                    var loadedDiagnosticSourceFileVersion = new Version(loadedDiagnosticSourceAssemblyFileVersionAttribute.Version);

                    var autoInstrumentationDiagnosticSourceLocation = Path.Combine(StartupHook.LoaderAssemblyLocation ?? string.Empty, "System.Diagnostics.DiagnosticSource.dll");
                    var autoInstrumentationDiagnosticSourceFileVersionInfo = FileVersionInfo.GetVersionInfo(autoInstrumentationDiagnosticSourceLocation);
                    var autoInstrumentationDiagnosticSourceFileVersion = new Version(autoInstrumentationDiagnosticSourceFileVersionInfo.FileVersion);

                    if (loadedDiagnosticSourceFileVersion < autoInstrumentationDiagnosticSourceFileVersion)
                    {
                        olderDiagnosticSourcePackageVersion = loadedDiagnosticSourceAssemblyFileVersionAttribute.Version;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // Exception in evaluation should not throw or crash the process.
            Logger.Information($"Couldn't evaluate reference to System.Diagnostics.DiagnosticSource in an app. Exception: {ex}");
        }

        if (olderDiagnosticSourcePackageVersion != null)
        {
            Logger.Error($"Application has direct or indirect reference to older version of System.Diagnostics.DiagnosticSource.dll {olderDiagnosticSourcePackageVersion}.");
            return false;
        }

        return true;
    }
}
