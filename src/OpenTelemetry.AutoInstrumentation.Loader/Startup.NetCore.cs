// <copyright file="Startup.NetCore.cs" company="OpenTelemetry Authors">
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

#if NETCOREAPP
using System;
using System.IO;
using System.Reflection;

namespace OpenTelemetry.AutoInstrumentation.Loader
{
    /// <summary>
    /// A class that attempts to load the OpenTelemetry.AutoInstrumentation .NET assembly.
    /// </summary>
    public partial class Startup
    {
        internal static System.Runtime.Loader.AssemblyLoadContext DependencyLoadContext { get; } = new ManagedProfilerAssemblyLoadContext();

        private static string ResolveManagedProfilerDirectory()
        {
            string tracerFrameworkDirectory = "netcoreapp3.1";
            string tracerHomeDirectory = ReadEnvironmentVariable("OTEL_DOTNET_AUTO_HOME") ?? string.Empty;

            return Path.Combine(tracerHomeDirectory, tracerFrameworkDirectory);
        }

        private static Assembly AssemblyResolve_ManagedProfilerDependencies(object sender, ResolveEventArgs args)
        {
            var assemblyName = new AssemblyName(args.Name);

            // On .NET Framework, having a non-US locale can cause mscorlib
            // to enter the AssemblyResolve event when searching for resources
            // in its satellite assemblies. This seems to have been fixed in
            // .NET Core in the 2.0 servicing branch, so we should not see this
            // occur, but guard against it anyways. If we do see it, exit early
            // so we don't cause infinite recursion.
            if (string.Equals(assemblyName.Name, "System.Private.CoreLib.resources", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(assemblyName.Name, "System.Net.Http", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var path = Path.Combine(ManagedProfilerDirectory, $"{assemblyName.Name}.dll");

            // Only load the main profiler into the default Assembly Load Context.
            // If OpenTelemetry.AutoInstrumentation or other libraries are provided by the NuGet package their loads are handled in the following two ways.
            // 1) The AssemblyVersion is greater than or equal to the version used by OpenTelemetry.AutoInstrumentation, the assembly
            //    will load successfully and will not invoke this resolve event.
            // 2) The AssemblyVersion is lower than the version used by OpenTelemetry.AutoInstrumentation, the assembly will fail to load
            //    and invoke this resolve event. It must be loaded in a separate AssemblyLoadContext since the application will only
            //    load the originally referenced version
            if (assemblyName.Name.StartsWith("OpenTelemetry.AutoInstrumentation", StringComparison.OrdinalIgnoreCase) && File.Exists(path))
            {
                StartupLogger.Debug("Loading {0} with Assembly.LoadFrom", path);
                return Assembly.LoadFrom(path);
            }
            else if (File.Exists(path))
            {
                StartupLogger.Debug("Loading {0} with DependencyLoadContext.LoadFromAssemblyPath", path);
                return DependencyLoadContext.LoadFromAssemblyPath(path); // Load unresolved framework and third-party dependencies into a custom Assembly Load Context
            }

            return null;
        }
    }
}

#endif
