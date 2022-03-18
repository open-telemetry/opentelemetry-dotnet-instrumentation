// <copyright file="IntegrationRegistry.cs" company="OpenTelemetry Authors">
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
using System.Collections.Generic;
using System.Linq;

namespace OpenTelemetry.AutoInstrumentation.Configuration;

internal static class IntegrationRegistry
{
    internal static readonly string[] Names;

    internal static readonly IReadOnlyDictionary<string, int> Ids;

    static IntegrationRegistry()
    {
        var values = Enum.GetValues(typeof(Instrumentation));
        var ids = new Dictionary<string, int>(values.Length);

        Names = new string[values.Cast<int>().Max() + 1];

        foreach (Instrumentation value in values)
        {
            var name = value.ToString();

            Names[(int)value] = name;
            ids.Add(name, (int)value);
        }

        Ids = ids;
    }

    internal static string GetName(IntegrationInfo integration)
    {
        if (integration.Name == null)
        {
            return Names[integration.Id];
        }

        return integration.Name;
    }

    internal static IntegrationInfo GetIntegrationInfo(string integrationName)
    {
        if (Ids.TryGetValue(integrationName, out var id))
        {
            return new IntegrationInfo(id);
        }

        return new IntegrationInfo(integrationName);
    }
}
