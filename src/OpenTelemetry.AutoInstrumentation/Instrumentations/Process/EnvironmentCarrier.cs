// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections;
using System.Runtime.InteropServices;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.Process;

internal static class EnvironmentCarrier
{
    internal static Dictionary<string, string?> Capture()
    {
        var comparer = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        var snapshot = new Dictionary<string, string?>(comparer);
        foreach (DictionaryEntry entry in Environment.GetEnvironmentVariables())
        {
            // Preserve actual names: on Unix a non-normalized name is not a carrier key.
            snapshot[(string)entry.Key] = entry.Value as string;
        }

        return snapshot;
    }

    internal static IEnumerable<string> Get(IDictionary<string, string?> carrier, string key)
    {
        return carrier.TryGetValue(NormalizeKey(key), out var value) && value is not null ? [value] : [];
    }

    internal static void Set(IDictionary<string, string?> carrier, string key, string value)
    {
        carrier[NormalizeKey(key)] = value;
    }

    internal static string NormalizeKey(string key)
    {
        if (key.Length == 0)
        {
            return "_";
        }

        var prefixLength = key[0] is >= '0' and <= '9' ? 1 : 0;
        var normalized = new char[key.Length + prefixLength];
        if (prefixLength != 0)
        {
            normalized[0] = '_';
        }

        for (var i = 0; i < key.Length; i++)
        {
            var character = key[i];
            normalized[i + prefixLength] = character switch
            {
                >= 'a' and <= 'z' => (char)(character - ('a' - 'A')),
                >= 'A' and <= 'Z' or >= '0' and <= '9' or '_' => character,
                _ => '_'
            };
        }

        return new string(normalized);
    }
}
