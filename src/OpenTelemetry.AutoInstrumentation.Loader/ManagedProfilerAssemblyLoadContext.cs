// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET
using System.Reflection;
using System.Runtime.Loader;

namespace OpenTelemetry.AutoInstrumentation.Loader;

internal class ManagedProfilerAssemblyLoadContext : AssemblyLoadContext
{
    protected override Assembly? Load(AssemblyName assemblyName)
    {
        return null;
    }
}
#endif
