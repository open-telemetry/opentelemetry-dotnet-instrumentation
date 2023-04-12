// <copyright file="ApplicationInExcludeListRule.cs" company="OpenTelemetry Authors">
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

using OpenTelemetry.AutoInstrumentation.Logging;

namespace OpenTelemetry.AutoInstrumentation.RulesEngine;

internal class ApplicationInExcludeListRule : Rule
{
    private static readonly IOtelLogger Logger = OtelLogging.GetLogger("StartupHook");

    public ApplicationInExcludeListRule()
    {
        Name = "Application is in exclude list validator";
        Description = "This rule checks if the application is included in the exclusion list specified by the OTEL_DOTNET_AUTO_EXCLUDE_PROCESSES environment variable. If the application is in the exclusion list, the rule skips initialization.";
    }

    internal override bool Evaluate()
    {
        var applicationName = GetApplicationName();
        Logger.Information($"Rule Engine: StartupHook loaded for application with name {applicationName}.");

        if (IsApplicationInExcludeList(applicationName))
        {
            Logger.Information("Rule Engine: Application is in the exclusion list. Skipping initialization.");
            return false;
        }

        Logger.Information("Rule Engine: ApplicationInExcludeListRule evaluation success.");
        return true;
    }

    private static string GetApplicationName()
    {
        try
        {
            return AppDomain.CurrentDomain.FriendlyName;
        }
        catch (Exception ex)
        {
            Logger.Error($"Error getting AppDomain.CurrentDomain.FriendlyName: {ex}");
            return string.Empty;
        }
    }

    private static bool IsApplicationInExcludeList(string applicationName)
    {
        return GetExcludedApplicationNames().Contains(applicationName);
    }

    private static List<string> GetExcludedApplicationNames()
    {
        var excludedProcesses = new List<string>();

        var environmentValue = GetEnvironmentVariable("OTEL_DOTNET_AUTO_EXCLUDE_PROCESSES");

        if (environmentValue == null)
        {
            return excludedProcesses;
        }

        foreach (var processName in environmentValue.Split(Constants.ConfigurationValues.Separator))
        {
            if (!string.IsNullOrWhiteSpace(processName))
            {
                excludedProcesses.Add(processName.Trim());
            }
        }

        return excludedProcesses;
    }

    private static string? GetEnvironmentVariable(string variableName)
    {
        try
        {
            return Environment.GetEnvironmentVariable(variableName);
        }
        catch (Exception ex)
        {
            Logger.Error($"Error getting environment variable {variableName}: {ex}");
            return null;
        }
    }
}
