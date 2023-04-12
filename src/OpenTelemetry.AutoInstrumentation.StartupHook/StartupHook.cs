// <copyright file="StartupHook.cs" company="OpenTelemetry Authors">
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
using System.Runtime.Versioning;
using OpenTelemetry.AutoInstrumentation;
using OpenTelemetry.AutoInstrumentation.Logging;
using OpenTelemetry.AutoInstrumentation.RulesEngine;

/// <summary>
/// Dotnet StartupHook
/// </summary>
internal class StartupHook
{
    private static readonly IOtelLogger Logger = OtelLogging.GetLogger("StartupHook");

    // This property must be initialized before any rule is evaluated since it may be used during rule evaluation.
    internal static string? LoaderAssemblyLocation { get; set; }

    /// <summary>
    /// Load and initialize OpenTelemetry.AutoInstrumentation assembly to bring OpenTelemetry SDK
    /// with a pre-defined set of exporters, shims, and instrumentations.
    /// </summary>
    public static void Initialize()
    {
        var minSupportedFramework = new FrameworkName(".NETCoreApp,Version=v6.0");
        var appTargetFramework = Assembly.GetEntryAssembly()?.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName;
        // This is the best way to identify application's target framework.
        // If entry assembly framework is null, StartupHook should continue its execution.
        if (appTargetFramework != null)
        {
            var appTargetFrameworkName = new FrameworkName(appTargetFramework);
            var appTargetFrameworkVersion = appTargetFrameworkName.Version;

            if (appTargetFrameworkVersion < minSupportedFramework.Version)
            {
                Logger.Information($"Error in StartupHook initialization: {appTargetFramework} is not supported");
                return;
            }
        }

        var applicationName = GetApplicationName();
        Logger.Information($"StartupHook loaded for application with name {applicationName}.");

        if (IsApplicationInExcludeList(applicationName))
        {
            Logger.Information("Application is in the exclusion list. Skipping initialization.");
            return;
        }

        Logger.Information("Attempting initialization.");

        LoaderAssemblyLocation = GetLoaderAssemblyLocation();

        try
        {
            var ruleEngine = new RuleEngine();
            if (!ruleEngine.Validate())
            {
                Logger.Error("Rule Engine Failure: One or more rules failed validation. Auto-Instrumentation won't be loaded.");
                return;
            }

            // Creating an instance of OpenTelemetry.AutoInstrumentation.Loader.Startup
            // will initialize Instrumentation through its static constructor.
            string loaderFilePath = Path.Combine(LoaderAssemblyLocation, "OpenTelemetry.AutoInstrumentation.Loader.dll");
            Assembly loaderAssembly = Assembly.LoadFrom(loaderFilePath);
            var loaderInstance = loaderAssembly.CreateInstance("OpenTelemetry.AutoInstrumentation.Loader.Loader");
            if (loaderInstance is null)
            {
                Logger.Error("StartupHook failed to create an instance of the Loader");
            }
            else
            {
                Logger.Information("StartupHook initialized successfully!");
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Error in StartupHook initialization: LoaderFolderLocation: {LoaderAssemblyLocation}, Error: {ex}");
            throw;
        }
    }

    private static string GetLoaderAssemblyLocation()
    {
        try
        {
            var startupAssemblyFilePath = Assembly.GetExecutingAssembly().Location;
            if (startupAssemblyFilePath.StartsWith(@"\\?\"))
            {
                // This will only be used in case the local path exceeds max_path size limit
                startupAssemblyFilePath = startupAssemblyFilePath.Substring(4);
            }

            // StartupHook and Loader assemblies are in the same path
            var startupAssemblyDirectoryPath = Path.GetDirectoryName(startupAssemblyFilePath) ??
                                               throw new NullReferenceException("StartupAssemblyFilePath is NULL");
            return startupAssemblyDirectoryPath;
        }
        catch (Exception ex)
        {
            Logger.Error($"Error getting loader directory location: {ex}");
            throw;
        }
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
