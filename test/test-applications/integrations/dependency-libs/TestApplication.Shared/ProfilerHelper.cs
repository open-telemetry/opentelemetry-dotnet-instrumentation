// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections;
using System.Text.RegularExpressions;
#if ASPNET_NETFRAMEWORK
using System;
using System.Collections.Generic;
using System.Linq;
#endif

namespace TestApplication.Shared;

internal static class ProfilerHelper
{
    // Keep this pattern and replacement aligned with MatchesSecretsPattern in native regex_utils.cpp.
    // The native profiler matches the complete KEY=value entry.
    private static readonly Regex SecretsPattern = new(@"(?:^|_)(API|TOKEN|SECRET|KEY|PASSWORD|PASS|PWD|HEADERS?|CREDENTIALS)(?:_|=|$)", RegexOptions.ECMAScript | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public static IEnumerable<KeyValuePair<string, string>> GetEnvironmentConfiguration()
    {
        var prefixes = new[] { "COR_", "CORECLR_", "DOTNET_", "OTEL_" };

        var envVars = from envVar in Environment.GetEnvironmentVariables().Cast<DictionaryEntry>()
                      from prefix in prefixes
                      let key = (envVar.Key as string)?.ToUpperInvariant()
                      let value = envVar.Value as string
                      where key.StartsWith(prefix, StringComparison.Ordinal)
                      orderby key
                      select new KeyValuePair<string, string>(key, SecretsPattern.IsMatch(key + "=" + value) ? "<hidden>" : value);

        return envVars;
    }
}
