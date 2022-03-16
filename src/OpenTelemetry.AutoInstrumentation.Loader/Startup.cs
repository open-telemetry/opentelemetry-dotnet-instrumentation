// <copyright file="Startup.cs" company="OpenTelemetry Authors">
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
using System.IO;
using System.Reflection;

namespace OpenTelemetry.AutoInstrumentation.Loader;

/// <summary>
/// A class that attempts to load the OpenTelemetry.AutoInstrumentation .NET assembly.
/// </summary>
public partial class Startup
{
    /// <summary>
    /// Initializes static members of the <see cref="Startup"/> class.
    /// This method also attempts to load the OpenTelemetry.AutoInstrumentation .NET assembly.
    /// </summary>
    static Startup()
    {
        ManagedProfilerDirectory = ResolveManagedProfilerDirectory();

        try
        {
            AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolve_ManagedProfilerDependencies;
        }
        catch (Exception ex)
        {
            StartupLogger.Log(ex, "Unable to register a callback to the CurrentDomain.AssemblyResolve event.");
        }

        TryLoadManagedAssembly();
    }

    internal static string ManagedProfilerDirectory { get; }

    private static void TryLoadManagedAssembly()
    {
        StartupLogger.Log("Managed Loader TryLoadManagedAssembly()");

        try
        {
            var assembly = Assembly.Load("OpenTelemetry.AutoInstrumentation");
            if (assembly == null)
            {
                throw new FileNotFoundException("The assembly OpenTelemetry.AutoInstrumentation could not be loaded");
            }

            var type = assembly.GetType("OpenTelemetry.AutoInstrumentation.Instrumentation", throwOnError: false);
            if (type == null)
            {
                throw new TypeLoadException("The type OpenTelemetry.AutoInstrumentation.Instrumentation could not be loaded");
            }

            var method = type.GetRuntimeMethod("Initialize", new Type[0]);
            if (method == null)
            {
                throw new MissingMethodException("The method OpenTelemetry.AutoInstrumentation.Instrumentation.Initialize could not be loaded");
            }

            method.Invoke(obj: null, parameters: null);
        }
        catch (Exception ex)
        {
            StartupLogger.Log(ex, $"Error when loading managed assemblies. {ex.Message}");
            throw;
        }
    }

    private static string ReadEnvironmentVariable(string key)
    {
        try
        {
            return Environment.GetEnvironmentVariable(key);
        }
        catch (Exception ex)
        {
            StartupLogger.Log(ex, "Error while loading environment variable " + key);
        }

        return null;
    }
}
