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
                Logger.LogInformation($"Error in StartupHook initialization: {appTargetFramework} is not supported");
                return;
            }
        }

        var applicationName = GetApplicationName();
        Logger.LogInformation($"StartupHook loaded for application with name {applicationName}.");

        if (IsApplicationInExcludeList(applicationName))
        {
            Logger.LogInformation("Application is in the exclusion list. Skipping initialization.");
            return;
        }

        Logger.LogInformation("Attempting initialization.");

        string loaderAssemblyLocation = GetLoaderAssemblyLocation();

        try
        {
            // Check Instrumentation is already initialized with native profiler.
            var profilerType = Type.GetType("OpenTelemetry.AutoInstrumentation.Instrumentation, OpenTelemetry.AutoInstrumentation");

            if (profilerType == null)
            {
                ThrowIfReferenceIncorrectOpenTelemetryVersion(loaderAssemblyLocation);

                // Instrumentation is not initialized.
                // Creating an instance of OpenTelemetry.AutoInstrumentation.Loader.Startup
                // will initialize Instrumentation through its static constructor.
                string loaderFilePath = Path.Combine(loaderAssemblyLocation, "OpenTelemetry.AutoInstrumentation.Loader.dll");
                Assembly loaderAssembly = Assembly.LoadFrom(loaderFilePath);
                loaderAssembly.CreateInstance("OpenTelemetry.AutoInstrumentation.Loader.Startup");
                Logger.LogInformation("StartupHook initialized successfully!");
            }
            else
            {
                Logger.LogInformation("OpenTelemetry.AutoInstrumentation.Instrumentation initialized before startup hook");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error in StartupHook initialization: LoaderFolderLocation: {loaderAssemblyLocation}, Error: {ex}");
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
            Logger.LogError($"Error getting loader directory location: {ex}");
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
            Logger.LogError($"Error getting AppDomain.CurrentDomain.FriendlyName: {ex}");
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
            Logger.LogError($"Error getting environment variable {variableName}: {ex}");
            return null;
        }
    }

    private static void ThrowIfReferenceIncorrectOpenTelemetryVersion(string loaderAssemblyLocation)
    {
        string? oTelPackageVersion = null;

        try
        {
            // Look up for type with an assembly name, will load the library.
            // OpenTelemetry assembly load happens only if app has reference to the package.
            var openTelemetryType = Type.GetType("OpenTelemetry.Sdk, OpenTelemetry");
            if (openTelemetryType != null)
            {
                var loadedOTelAssembly = Assembly.GetAssembly(openTelemetryType);
                var loadedOTelFileVersionInfo = FileVersionInfo.GetVersionInfo(loadedOTelAssembly?.Location);
                var loadedOTelFileVersion = new Version(loadedOTelFileVersionInfo.FileVersion);

                var autoInstrumentationOTelLocation = Path.Combine(loaderAssemblyLocation, "OpenTelemetry.dll");
                var autoInstrumentationOTelFileVersionInfo = FileVersionInfo.GetVersionInfo(autoInstrumentationOTelLocation);
                var autoInstrumentationOTelFileVersion = new Version(autoInstrumentationOTelFileVersionInfo.FileVersion);

                if (loadedOTelFileVersion < autoInstrumentationOTelFileVersion)
                {
                    oTelPackageVersion = loadedOTelFileVersionInfo.FileVersion;
                }
            }
        }
        catch (Exception ex)
        {
            // Exception in evaluation should not throw or crash the process.
            Logger.LogInformation($"Couldn't evaluate reference to OpenTelemetry Sdk in an app. Exception: {ex}");
        }

        if (oTelPackageVersion != null)
        {
            throw new NotSupportedException($"Application has direct or indirect reference to older version of OpenTelemetry package {oTelPackageVersion}.");
        }
    }
}
