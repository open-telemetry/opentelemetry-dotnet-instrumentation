// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using System.Reflection;
using OpenTelemetry.AutoInstrumentation.Logging;

namespace OpenTelemetry.AutoInstrumentation.RulesEngine;

internal class RuntimeStoreVersionRule : Rule
{
    private const string RuntimeStoreEnvironmentVariable = "DOTNET_SHARED_STORE";
    private static readonly IOtelLogger Logger = OtelLogging.GetLogger("StartupHook");

    public RuntimeStoreVersionRule()
    {
        Name = "Runtime Store Assembly Validator";
        Description = "Ensure that the runtime store assembly versions are not lower than the version used by the Application";
    }

    internal override bool Evaluate()
    {
        var result = true;

        try
        {
            var configuredStoreDirectory = GetConfiguredStoreDirectory();
            if (configuredStoreDirectory == null)
            {
                // Store location not found, skip rule evaluation
                return result;
            }

            var storeFiles = Directory.GetFiles(configuredStoreDirectory, "Microsoft.Extensions*.dll", SearchOption.AllDirectories);

            foreach (var file in storeFiles)
            {
                var assemblyName = Path.GetFileNameWithoutExtension(file);
                var appInstrumentationAssembly = Assembly.Load(assemblyName);
                var appInstrumentationFileVersionInfo = FileVersionInfo.GetVersionInfo(appInstrumentationAssembly.Location);
                var appInstrumentationFileVersion = new Version(appInstrumentationFileVersionInfo.FileVersion);

                if (appInstrumentationFileVersion.Major < 5)
                {
                    // Special case to handle runtime store version 3.1.x.x package references in app.
                    // Skip rule evaluation for assemblies with version 3.1.x.x.
                    Logger.Debug($"Rule Engine: Skipping rule evaluation for runtime store assembly {appInstrumentationFileVersionInfo.FileName} with version {appInstrumentationFileVersion}.");
                    continue;
                }

                var runTimeStoreFileVersionInfo = FileVersionInfo.GetVersionInfo(file);
                var runTimeStoreFileVersion = new Version(runTimeStoreFileVersionInfo.FileVersion);

                if (appInstrumentationFileVersion < runTimeStoreFileVersion)
                {
                    result = false;
                    Logger.Error($"Rule Engine: Application has direct or indirect reference to lower version of runtime store assembly {runTimeStoreFileVersionInfo.FileName} - {appInstrumentationFileVersion}.");
                }
                else
                {
                    Logger.Debug($"Rule Engine: Runtime store assembly {runTimeStoreFileVersionInfo.FileName} is validated successfully.");
                }
            }
        }
        catch (Exception)
        {
            // Exception in rule evaluation should not impact the result of the rule.
            Logger.Warning("Rule Engine: Couldn't evaluate reference to runtime store assemblies in an app.");
            throw;
        }

        return result;
    }

    private static string? GetConfiguredStoreDirectory()
    {
        try
        {
            var storeDirectory = Environment.GetEnvironmentVariable(RuntimeStoreEnvironmentVariable);
            if (storeDirectory == null)
            {
                Logger.Warning($"Rule Engine: {RuntimeStoreEnvironmentVariable} environment variable not found. Skipping rule evaluation.");
                return null;
            }

            // Check if the store directory exists
            if (!Directory.Exists(storeDirectory))
            {
                Logger.Warning($"Rule Engine: Runtime store directory not found at {storeDirectory}. Skipping rule evaluation.");
                return null;
            }

            var architecture = Environment.Is64BitProcess ? "x64" : "x86";
            var targetFramework = "net" + Environment.Version.Major.ToString() + "." + Environment.Version.Minor.ToString();
            var finalPath = Path.Combine(storeDirectory, architecture.ToString(), targetFramework);

            return finalPath;
        }
        catch (Exception ex)
        {
            Logger.Error($"Error getting store directory location: {ex}");
            throw;
        }
    }
}
