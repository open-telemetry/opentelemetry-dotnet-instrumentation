// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.Util;

internal static partial class ManagedProfilerLocationHelper
{
    private static readonly string TracerHomeDirectory =
        ReadEnvironmentVariable(Constants.EnvironmentVariables.OtelDotnetAutoHome) ?? string.Empty;

    private static string? ReadEnvironmentVariable(string key)
    {
        try
        {
            return Environment.GetEnvironmentVariable(key);
        }
        catch
        {
            return null;
        }
    }
}
