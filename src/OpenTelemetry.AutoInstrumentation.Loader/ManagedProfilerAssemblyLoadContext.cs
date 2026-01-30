// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET
using System.Reflection;
using System.Runtime.Loader;

namespace OpenTelemetry.AutoInstrumentation.Loader;

internal class ManagedProfilerAssemblyLoadContext(string name, bool isCollectible = false) : AssemblyLoadContext(name, isCollectible)
{
    protected override Assembly? Load(AssemblyName assemblyName)
    {
        return null;
    }
}
#endif
