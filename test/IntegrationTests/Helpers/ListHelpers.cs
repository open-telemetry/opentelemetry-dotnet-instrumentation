// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace IntegrationTests.Helpers;

internal static class ListHelpers
{
    public static ICollection<KeyValuePair<string, string>> ToEnvironmentVariablesList(this IEnumerable<string> list)
    {
        return list.Select(x =>
            {
                var keyValuePair = x.Split(['='], 2);

                return new KeyValuePair<string, string>(keyValuePair[0], keyValuePair[1]);
            })
            .ToList();
    }
}
