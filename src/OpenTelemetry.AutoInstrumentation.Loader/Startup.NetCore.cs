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
using System.Runtime.Loader;

namespace OpenTelemetry.AutoInstrumentation.Loader
{
    /// <summary>
    /// A class that attempts to load the OpenTelemetry.AutoInstrumentation .NET assembly.
    /// </summary>
    public partial class Startup
    {
        internal static AssemblyLoadContext OpenTelemetryLoadContext { get; set; }

        private static void Initialize()
        {
            OpenTelemetryLoadContext = new ManagedProfilerAssemblyLoadContext(ManagedProfilerDirectory);
        }

        private static Assembly LoadAssembly(string name)
        {
            return OpenTelemetryLoadContext.LoadFromAssemblyName(new AssemblyName(name));
        }

        private static string ResolveManagedProfilerDirectory()
        {
            string tracerFrameworkDirectory = "netcoreapp3.1";
            string tracerHomeDirectory = ReadEnvironmentVariable("OTEL_DOTNET_AUTO_HOME") ?? string.Empty;

            return Path.Combine(tracerHomeDirectory, tracerFrameworkDirectory);
        }

        private static Assembly AssemblyResolve_ManagedProfilerDependencies(object sender, ResolveEventArgs args)
        {
            var assemblyName = new AssemblyName(args.Name);

            return OpenTelemetryLoadContext.LoadFromAssemblyName(assemblyName);
        }
    }
}

#endif
