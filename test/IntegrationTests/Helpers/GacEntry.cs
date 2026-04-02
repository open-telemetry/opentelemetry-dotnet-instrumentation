// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NETFRAMEWORK
using System.EnterpriseServices.Internal;

namespace IntegrationTests.Helpers;

internal sealed class GacEntry : IDisposable
{
    private readonly string _assemblyPath;
    private readonly Publish _publish = new Publish();

    public GacEntry(string assemblyPath)
    {
        _assemblyPath = assemblyPath;
        _publish.GacInstall(assemblyPath);
    }

    public void Dispose()
    {
        _publish.GacRemove(_assemblyPath);
    }
}
#endif
