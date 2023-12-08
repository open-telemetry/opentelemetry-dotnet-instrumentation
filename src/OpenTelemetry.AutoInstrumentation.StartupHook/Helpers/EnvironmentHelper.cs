// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Security;
using OpenTelemetry.AutoInstrumentation.Logging;

namespace OpenTelemetry.AutoInstrumentation.Helpers;

internal static class EnvironmentHelper
{
    private static readonly IOtelLogger Logger = OtelLogging.GetLogger("StartupHook");

    public static string? GetEnvironmentVariable(string variableName)
    {
        try
        {
            return Environment.GetEnvironmentVariable(variableName);
        }
        catch (SecurityException ex)
        {
            Logger.Error(ex, "Error getting environment variable {0}:", variableName);
            return null;
        }
    }
}
