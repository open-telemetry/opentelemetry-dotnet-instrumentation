// <copyright file="ManagedProfilerAssemblyLoadContext.cs" company="OpenTelemetry Authors">
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
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace OpenTelemetry.AutoInstrumentation.Loader
{
    internal class ManagedProfilerAssemblyLoadContext : AssemblyLoadContext
    {
        private AssemblyDependencyResolver _resolver;

        public ManagedProfilerAssemblyLoadContext(string managedHome)
            : base(name: nameof(ManagedProfilerAssemblyLoadContext))
        {
            string managedEntryModule = Path.Combine(managedHome, "OpenTelemetry.AutoInstrumentation.dll");

            _resolver = new AssemblyDependencyResolver(managedEntryModule);
        }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            // All exceptions here, libraries that must be shared are going to Default load context
            if (assemblyName.Name.Contains("System.Diagnostics.DiagnosticSource"))
            {
                return null;
            }

            string assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
            if (assemblyPath != null)
            {
                return LoadFromAssemblyPath(assemblyPath);
            }

            return null;
        }
    }
}
#endif
