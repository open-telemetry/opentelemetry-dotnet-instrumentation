// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using OpenTelemetry.AutoInstrumentation.Logging;

namespace OpenTelemetry.AutoInstrumentation.RulesEngine;

internal class AssemblyFileVersionRule : Rule
{
    private static readonly IOtelLogger Logger = OtelLogging.GetLogger("StartupHook");

    public AssemblyFileVersionRule()
    {
        Name = "Assembly File Version Validator";
        Description = "Ensure that the version of key assemblies is not older than the version used by Automatic Instrumentation.";
    }

    internal override bool Evaluate()
    {
        var result = true;

        try
        {
            var ruleEngineFileLocation = Path.Combine(StartupHook.LoaderAssemblyLocation ?? string.Empty, "ruleEngine.json");
            var ruleEngineContent = File.ReadAllText(ruleEngineFileLocation);
            var ruleFileInfoList = JsonSerializer.Deserialize<List<RuleFileInfo>>(ruleEngineContent);
            var entryAssembly = Assembly.GetEntryAssembly();
            var referencedAssemblies = entryAssembly?.GetReferencedAssemblies();

            if (referencedAssemblies == null)
            {
                Logger.Warning($"Rule Engine: Could not get referenced assembly (GetReferencedAssemblies()) from an application. Skipping rule evaluation.");
                return result;
            }

            foreach (var referencedAssembly in referencedAssemblies)
            {
                var ruleFileInfo = ruleFileInfoList.FirstOrDefault(file => file.FileName == referencedAssembly.Name);
                if (ruleFileInfo != null)
                {
                    var autoInstrumentationFileVersion = new Version(ruleFileInfo.FileVersion);

                    var appInstrumentationAssembly = Assembly.Load(referencedAssembly);
                    var appInstrumentationFileVersionInfo = FileVersionInfo.GetVersionInfo(appInstrumentationAssembly.Location);
                    var appInstrumentationFileVersion = new Version(appInstrumentationFileVersionInfo.FileVersion);

                    if (appInstrumentationFileVersion < autoInstrumentationFileVersion)
                    {
                        result = false;
                        Logger.Error($"Rule Engine: Application has direct or indirect reference to older version of assembly {ruleFileInfo.FileName} - {ruleFileInfo.FileVersion}.");
                    }
                    else
                    {
                        Logger.Information($"Rule Engine: Application has reference to assembly {ruleFileInfo.FileName} and loaded successfully.");
                    }
                }
            }
        }
#pragma warning disable CA1031 // Do not catch general exception
        catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception
        {
            // An exception in rule evaluation should not impact the result of the rule.
            Logger.Warning($"Rule Engine: Couldn't evaluate assembly reference file version. Exception: {ex}");
        }

        return result;
    }

    private class RuleFileInfo
    {
        public string FileName { get; set; } = string.Empty;

        public string FileVersion { get; set; } = string.Empty;
    }
}
