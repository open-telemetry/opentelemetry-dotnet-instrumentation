// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using System.Reflection;
using OpenTelemetry.AutoInstrumentation.Logging;

namespace OpenTelemetry.AutoInstrumentation.RulesEngine;

internal class RuntimeStoreVersionRule : Rule
{
    private const string AdditionalDepsEnvironmentVariable = "DOTNET_ADDITIONAL_DEPS";
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
            if (Environment.GetEnvironmentVariable(AdditionalDepsEnvironmentVariable) == null)
            {
                Logger.Warning($"Rule Engine: {AdditionalDepsEnvironmentVariable} environment variable not found. Skipping rule evaluation.");
                return result;
            }

            var configuredStoreDirectory = GetConfiguredStoreDirectory();
            if (configuredStoreDirectory == null)
            {
                // Store location not found, skip rule evaluation
                return result;
            }

            string[] storeFiles = Directory.GetFiles(configuredStoreDirectory, "*.dll", SearchOption.AllDirectories);

            foreach (string file in storeFiles)
            {
                var runTimeStoreFileVersionInfo = FileVersionInfo.GetVersionInfo(file);
                var runTimeStoreFileVersion = new Version(runTimeStoreFileVersionInfo.FileVersion);

                var assemblyName = Path.GetFileNameWithoutExtension(file);
                var appInstrumentationAssembly = Assembly.Load(assemblyName);
                var appInstrumentationFileVersionInfo = FileVersionInfo.GetVersionInfo(appInstrumentationAssembly.Location);
                var appInstrumentationFileVersion = new Version(appInstrumentationFileVersionInfo.FileVersion);

                if (appInstrumentationFileVersion < runTimeStoreFileVersion)
                {
                    result = false;
                    Logger.Error($"Rule Engine: Application has direct or indirect reference to lower version of runtime store assembly {runTimeStoreFileVersionInfo.FileName} - {appInstrumentationFileVersion}.");
                }
                else
                {
                    Logger.Information($"Rule Engine: Runtime store assembly {runTimeStoreFileVersionInfo.FileName} is validated successfully.");
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
            var tracerHomeDirectory = Directory.GetParent(StartupHook.LoaderAssemblyLocation).ToString().ToString();
            var storeDirectory = Path.Combine(tracerHomeDirectory, "store");

            // Check if the store directory exists
            if (!Directory.Exists(storeDirectory))
            {
                Logger.Warning($"Rule Engine: Runtime store directory not found at {storeDirectory}. Skipping rule evaluation.");
                return null;
            }

            var architecture = Environment.Is64BitProcess ? "x64" : "x86";
            string targetFramework = "net" + Environment.Version.Major.ToString() + "." + Environment.Version.Minor.ToString();
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
