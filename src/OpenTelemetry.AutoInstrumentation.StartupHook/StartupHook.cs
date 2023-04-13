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

using System.Reflection;
using System.Runtime.Versioning;
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
        try
        {
            LoaderAssemblyLocation = GetLoaderAssemblyLocation();

            var ruleEngine = new RuleEngine();
            if (!ruleEngine.ValidateRules())
            {
                Logger.Error("Rule Engine Failure: One or more rules failed validation. Auto-Instrumentation won't be loaded.");
                return;
            }

            Logger.Information("Initialization.");

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
}
