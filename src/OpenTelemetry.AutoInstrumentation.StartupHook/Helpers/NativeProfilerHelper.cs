// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.Helpers;

/// <summary>
/// Identifies the native profiler without initializing logging before assembly isolation.
/// </summary>
internal static class NativeProfilerHelper
{
    public static bool IsOpenTelemetryProfiler(string? profilerId)
    {
        return string.Equals(profilerId, Constants.EnvironmentVariables.ProfilerId, StringComparison.OrdinalIgnoreCase);
    }
}
