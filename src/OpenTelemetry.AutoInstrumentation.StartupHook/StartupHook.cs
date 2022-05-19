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

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using OpenTelemetry.AutoInstrumentation.StartupHook;

/// <summary>
/// Dotnet StartupHook
/// </summary>
internal class StartupHook
{
    /// <summary>
    /// Load and initialize OpenTelemetry.AutoInstrumentation assembly to bring OpenTelemetry SDK
    /// with a pre-defined set of exporters, shims, and instrumentations.
    /// </summary>
    public static void Initialize()
    {
        var applicationName = GetApplicationName();
        StartupHookEventSource.Log.Trace($"StartupHook loaded for application with name {applicationName}.");

        if (IsApplicationInExcludeList(applicationName))
        {
            StartupHookEventSource.Log.Trace("Application is in the exclusion list. Skipping initialization.");
            return;
        }

        StartupHookEventSource.Log.Trace("Attempting initialization.");

        string loaderAssemblyLocation = GetLoaderAssemblyLocation();

        try
        {
            // Check Instrumentation is already initialized with native profiler.
            Type profilerType = Type.GetType("OpenTelemetry.AutoInstrumentation.Instrumentation, OpenTelemetry.AutoInstrumentation");

            if (profilerType == null)
            {
                // Instrumentation is not initialized.
                // Creating an instance of OpenTelemetry.AutoInstrumentation.Loader.Startup
                // will initialize Instrumentation through its static constructor.
                string loaderFilePath = Path.Combine(loaderAssemblyLocation, "OpenTelemetry.AutoInstrumentation.Loader.dll");
                Assembly loaderAssembly = Assembly.LoadFrom(loaderFilePath);
                loaderAssembly.CreateInstance("OpenTelemetry.AutoInstrumentation.Loader.Startup");
                StartupHookEventSource.Log.Trace("StartupHook initialized successfully!");
            }
            else
            {
                StartupHookEventSource.Log.Trace("OpenTelemetry.AutoInstrumentation.Instrumentation initialized before startup hook");
            }
        }
        catch (Exception ex)
        {
            StartupHookEventSource.Log.Error($"Error in StartupHook initialization: LoaderFolderLocation: {loaderAssemblyLocation}, Error: {ex}");
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
            StartupHookEventSource.Log.Error($"Error getting loader directory location: {ex}");
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
            StartupHookEventSource.Log.Error($"Error getting AppDomain.CurrentDomain.FriendlyName: {ex}");
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

        // The separator should always match the Settings.Separator. However, to avoid
        // creating a dependecy in the StartupHook project the separator is being redefined here.
        const char settingsSeparator = ',';
        foreach (var processName in environmentValue.Split(settingsSeparator))
        {
            if (!string.IsNullOrWhiteSpace(processName))
            {
                excludedProcesses.Add(processName.Trim());
            }
        }

        return excludedProcesses;
    }

    private static string GetEnvironmentVariable(string variableName)
    {
        try
        {
            return Environment.GetEnvironmentVariable(variableName);
        }
        catch (Exception ex)
        {
            StartupHookEventSource.Log.Error($"Error getting environment variable {variableName}: {ex}");
            return null;
        }
    }
}
