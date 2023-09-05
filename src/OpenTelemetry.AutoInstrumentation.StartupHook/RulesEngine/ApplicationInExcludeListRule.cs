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

using System.Diagnostics;
using OpenTelemetry.AutoInstrumentation.Helpers;
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
        var appDomainName = GetAppDomainName();
        if (appDomainName.Equals("dotnet", StringComparison.InvariantCultureIgnoreCase))
        {
            Logger.Information($"Rule Engine: AppDomain name is dotnet. Skipping initialization.");
            return false;
        }

        var processModuleName = GetProcessModuleName();
        if (GetExcludedApplicationNames().Contains(processModuleName, StringComparer.InvariantCultureIgnoreCase))
        {
            Logger.Information($"Rule Engine: {processModuleName} is in the exclusion list. Skipping initialization.");
            return false;
        }

        Logger.Debug($"Rule Engine: {processModuleName} is not in the exclusion list. ApplicationInExcludeListRule evaluation success.");
        return true;
    }

    private static string GetProcessModuleName()
    {
        try
        {
            return Process.GetCurrentProcess().MainModule.ModuleName;
        }
        catch (Exception ex)
        {
            Logger.Error($"Error getting Process.MainModule.ModuleName: {ex}");
            return string.Empty;
        }
    }

    private static string GetAppDomainName()
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

    private static ICollection<string> GetExcludedApplicationNames()
    {
        var excludedProcesses = new List<string>();

        var environmentValue = EnvironmentHelper.GetEnvironmentVariable("OTEL_DOTNET_AUTO_EXCLUDE_PROCESSES");

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
}
