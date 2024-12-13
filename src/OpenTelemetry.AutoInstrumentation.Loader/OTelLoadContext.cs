// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET

using System.Reflection;
using System.Runtime.Loader;

namespace OpenTelemetry.AutoInstrumentation.Loader;

internal class OTelLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver resolver;

    public OTelLoadContext(string pluginModule)
    {
        resolver = new AssemblyDependencyResolver(pluginModule);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        if (assemblyName.Name == "System.Diagnostics.DiagnosticSource")
        {
            return Default.LoadFromAssemblyName(assemblyName);
        }

        string? assemblyPath = resolver.ResolveAssemblyToPath(assemblyName);
        if (assemblyPath != null)
        {
            return LoadFromAssemblyPath(assemblyPath);
        }

        return null;
    }
}

#endif
