// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections;
#if ASPNET_NETFRAMEWORK
using System;
using System.Collections.Generic;
using System.Linq;
#endif

namespace TestApplication.Shared;

internal static class ProfilerHelper
{
    public static IEnumerable<KeyValuePair<string, string>> GetEnvironmentConfiguration()
    {
        var prefixes = new[] { "COR_", "CORECLR_", "DOTNET_", "OTEL_" };

        var envVars = from envVar in Environment.GetEnvironmentVariables().Cast<DictionaryEntry>()
                      from prefix in prefixes
                      let key = (envVar.Key as string)?.ToUpperInvariant()
                      let value = envVar.Value as string
                      where key.StartsWith(prefix, StringComparison.Ordinal)
                      orderby key
                      select new KeyValuePair<string, string>(key, value);

        return envVars;
    }
}
