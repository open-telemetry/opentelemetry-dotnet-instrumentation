// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

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
        if (appDomainName.Equals("dotnet", StringComparison.OrdinalIgnoreCase))
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

        Logger.Debug(
            "Rule Engine: {0} is not in the exclusion list. ApplicationInExcludeListRule evaluation success.",
            processModuleName);
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
