// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace IntegrationTests.Helpers;

internal static class DockerFileHelper
{
    public static string ReadImageFrom(string dockerfile)
    {
        var raw = File.ReadAllText($"./docker/{dockerfile}");

        // Exclude FROM
        return raw.Substring(4).Trim();
    }
}
