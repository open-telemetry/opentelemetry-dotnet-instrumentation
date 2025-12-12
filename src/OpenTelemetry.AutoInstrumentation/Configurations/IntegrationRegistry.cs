// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.Configurations;

internal static class IntegrationRegistry
{
    internal static readonly string[] Names;

    internal static readonly IReadOnlyDictionary<string, int> Ids;

    static IntegrationRegistry()
    {
#if NET
        var values = Enum.GetValues<TracerInstrumentation>();
#else
        var values = Enum.GetValues(typeof(TracerInstrumentation));
#endif
        var ids = new Dictionary<string, int>(values.Length);

        Names = new string[values.Cast<int>().Max() + 1];

        foreach (var value in values)
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
