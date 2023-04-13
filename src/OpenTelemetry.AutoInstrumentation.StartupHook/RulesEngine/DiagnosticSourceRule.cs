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
    private static IOtelLogger logger = OtelLogging.GetLogger("StartupHook");

    public DiagnosticSourceRule()
    {
        Name = "System.Diagnostics.DiagnosticSource Validator";
        Description = "Ensure that the System.Diagnostics.DiagnosticSource version is not older than the version used by the Auto-Instrumentation";
    }

    // This constructor is used for test purpose.
    protected DiagnosticSourceRule(IOtelLogger otelLogger)
        : this()
    {
        logger = otelLogger;
    }

    internal override bool Evaluate()
    {
        string? olderDiagnosticSourcePackageVersion = null;

        try
        {
            // NuGet package version is not available at runtime. AssemblyVersion is available but it only changes in major version updates.
            // Thus using AssemblyFileVersion to compare whether the loaded version is older than that of auto instrumentation.
            var loadedDiagnosticSourceFileVersion = GetResolvedVersion();
            if (loadedDiagnosticSourceFileVersion != null)
            {
                var autoInstrumentationDiagnosticSourceFileVersion = GetVersionFromAutoInstrumentation();
                if (loadedDiagnosticSourceFileVersion < autoInstrumentationDiagnosticSourceFileVersion)
                {
                    olderDiagnosticSourcePackageVersion = loadedDiagnosticSourceFileVersion.ToString();
                }
            }
        }
        catch (Exception ex)
        {
            // Exception in evaluation should not throw or crash the process.
            logger.Warning($"Rule Engine: Couldn't evaluate System.Diagnostics.DiagnosticSource version. Exception: {ex}");
            return false;
        }

        if (olderDiagnosticSourcePackageVersion != null)
        {
            logger.Error($"Rule Engine: Application has direct or indirect reference to older version of System.Diagnostics.DiagnosticSource.dll {olderDiagnosticSourcePackageVersion}.");
            return false;
        }

        logger.Information("Rule Engine: DiagnosticSourceRule evaluation success.");
        return true;
    }

    protected virtual Version? GetResolvedVersion()
    {
        // Look up for type with an assembly name, will load the library.
        var diagnosticSourceType = Type.GetType("System.Diagnostics.DiagnosticSource, System.Diagnostics.DiagnosticSource");
        if (diagnosticSourceType != null)
        {
            var loadedDiagnosticSourceAssembly = Assembly.GetAssembly(diagnosticSourceType);
            var loadedDiagnosticSourceAssemblyFileVersionAttribute = loadedDiagnosticSourceAssembly?.GetCustomAttribute<AssemblyFileVersionAttribute>();
            var versionString = loadedDiagnosticSourceAssemblyFileVersionAttribute?.Version;
            if (versionString != null)
            {
                return new Version(versionString);
            }
        }

        return null;
    }

    protected virtual Version GetVersionFromAutoInstrumentation()
    {
        var autoInstrumentationDiagnosticSourceLocation = Path.Combine(StartupHook.LoaderAssemblyLocation ?? string.Empty, "System.Diagnostics.DiagnosticSource.dll");
        var fileVersionInfo = FileVersionInfo.GetVersionInfo(autoInstrumentationDiagnosticSourceLocation);
        var autoInstrumentationDiagnosticSourceFileVersion = new Version(
            fileVersionInfo.FileMajorPart,
            fileVersionInfo.FileMinorPart,
            fileVersionInfo.FileBuildPart,
            fileVersionInfo.FilePrivatePart);
        return autoInstrumentationDiagnosticSourceFileVersion;
    }
}
