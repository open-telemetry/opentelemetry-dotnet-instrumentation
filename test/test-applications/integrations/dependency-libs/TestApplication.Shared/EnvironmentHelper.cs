// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace TestApplication.Shared;

internal static class EnvironmentHelper
{
    public static string GetRequiredEnvironmentVariable(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrEmpty(value))
        {
            throw new NotSupportedException($"Lack of required environment variable '{name}' for the test application.");
        }

        return value;
    }
}
