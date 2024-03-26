// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using IntegrationTests.Helpers;

namespace IntegrationTests;

/// <summary>
/// Logic partial of LibraryVersion
/// </summary>
public static partial class LibraryVersion
{
    public static IEnumerable<object[]> GetPlatformVersions(string integrationName)
    {
        var anyPlatformVersions = LookupMap[integrationName];
        var platformKey = $"{integrationName}_{EnvironmentTools.GetPlatform()}";

        if (LookupMap.TryGetValue(platformKey, out var platformVersions))
        {
            return anyPlatformVersions.Concat(platformVersions);
        }

        return anyPlatformVersions;
    }
}
