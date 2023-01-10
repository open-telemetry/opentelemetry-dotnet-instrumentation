// <copyright file="GacEntry.cs" company="OpenTelemetry Authors">
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

#if NETFRAMEWORK
using System.EnterpriseServices.Internal;

namespace IntegrationTests.Helpers;

public class GacEntry : IDisposable
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
