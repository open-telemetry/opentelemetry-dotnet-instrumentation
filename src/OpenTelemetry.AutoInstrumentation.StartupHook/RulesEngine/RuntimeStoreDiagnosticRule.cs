// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using OpenTelemetry.AutoInstrumentation.Logging;

namespace OpenTelemetry.AutoInstrumentation.RulesEngine;

// TODO remove Store rule after we get rid of Store approach altogether
internal class RuntimeStoreDiagnosticRule : Rule
{
    private const string RuntimeStoreEnvironmentVariable = "DOTNET_SHARED_STORE";
    private static readonly IOtelLogger Logger = OtelLogging.GetLogger("StartupHook");

    public RuntimeStoreDiagnosticRule()
    {
        Name = "Runtime Store Diagnostic Rule";
        Description = "Logs detail that assembly versions in the runtime store are not lower than the version the Application uses.";
    }

    internal override bool Evaluate()
    {
        try
        {
            // Skip rule evaluation if the application is running in self-contained mode.
            if (IsSelfContained())
            {
                Logger.Debug("Rule Engine: Skipping rule evaluation for self-contained application.");
                return true;
            }

            var configuredStoreDirectory = GetConfiguredStoreDirectory();
            if (configuredStoreDirectory == null)
            {
                // Store location not found, skip rule evaluation
                Logger.Debug("Rule Engine: Skipping rule evaluation as runtime store location is not found.");
                return true;
            }

            var storeFiles = Directory.GetFiles(configuredStoreDirectory, "Microsoft.Extensions*.dll", SearchOption.AllDirectories);

            foreach (var file in storeFiles)
            {
                var assemblyName = Path.GetFileNameWithoutExtension(file);
                Assembly appInstrumentationAssembly;

                try
                {
                    appInstrumentationAssembly = Assembly.Load(assemblyName);
                }
                catch (Exception ex)
                {
                    Logger.Warning(ex, $"Rule Engine: Assembly load failed. Skipping rule evaluation for assembly - {assemblyName}");
                    continue;
                }

                var appInstrumentationFileVersionInfo = FileVersionInfo.GetVersionInfo(appInstrumentationAssembly.Location);
                var appInstrumentationFileVersion = new Version(appInstrumentationFileVersionInfo.FileVersion);

                if (appInstrumentationFileVersion.Major < 5)
                {
                    // Special case to handle runtime store version 3.1.x.x package references in app.
                    // Skip rule evaluation for assemblies with version 3.1.x.x.
                    Logger.Debug(
                        "Rule Engine: Skipping rule evaluation for runtime store assembly {0} with version {1}.",
                        appInstrumentationFileVersionInfo.FileName,
                        appInstrumentationFileVersion);
                    continue;
                }

                var runTimeStoreFileVersionInfo = FileVersionInfo.GetVersionInfo(file);
                var runTimeStoreFileVersion = new Version(runTimeStoreFileVersionInfo.FileVersion);

                if (appInstrumentationFileVersion < runTimeStoreFileVersion)
                {
                    Logger.Warning($"Rule Engine: Application has direct or indirect reference to lower version of runtime store assembly {runTimeStoreFileVersionInfo.FileName} - {appInstrumentationFileVersion}. ");
                }
                else
                {
                    Logger.Debug(
                        "Rule Engine: Runtime store assembly {0} is validated successfully.",
                        runTimeStoreFileVersionInfo.FileName);
                }
            }
        }
        catch (Exception ex)
        {
            // Exception in rule evaluation should not impact the result of the rule.
            Logger.Warning(ex, "Rule Engine: Couldn't evaluate reference to runtime store assemblies in an app.");
        }

        // This a diagnostic rule, so we always return true.
        return true;
    }

    private static string? GetConfiguredStoreDirectory()
    {
        try
        {
            var storeDirectory = Environment.GetEnvironmentVariable(RuntimeStoreEnvironmentVariable);
            // Skip rule evaluation if the store directory is not configured.
            if (storeDirectory == null)
            {
                Logger.Debug($"Rule Engine: {RuntimeStoreEnvironmentVariable} environment variable not found. Skipping rule evaluation.");
                return null;
            }

            // Check if the store directory exists
            if (!Directory.Exists(storeDirectory))
            {
                Logger.Debug(
                    "Rule Engine: Runtime store directory not found at {0}. Skipping rule evaluation.",
                    storeDirectory);
                return null;
            }

            var architecture = RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.X86 => "x86",
                Architecture.Arm64 => "arm64",
                _ => "x64" // Default to x64 for architectures not explicitly handled
            };

            var targetFramework = $"net{Environment.Version.Major}.{Environment.Version.Minor}";
            var finalPath = Path.Combine(storeDirectory, architecture, targetFramework);

            return finalPath;
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "Error getting store directory location");
            throw;
        }
    }

    private static bool IsSelfContained()
    {
        var assemblyPath = Assembly.GetExecutingAssembly().Location;
        var directory = Path.GetDirectoryName(assemblyPath);

        // Check for the presence of a known .NET runtime file
        if (directory != null &&
            (File.Exists(Path.Combine(directory, "coreclr.dll")) ||
            File.Exists(Path.Combine(directory, "libcoreclr.so")) ||
            File.Exists(Path.Combine(directory, "libcoreclr.dylib"))))
        {
            return true;
        }

        return false;
    }
}
