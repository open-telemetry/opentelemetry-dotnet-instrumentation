// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.StartupHook.Util;

internal static class BooleanExtensions
{
    extension(bool)
    {
        public static bool ParseOrDefault(string? value, bool defaultValue = false)
        {
            return bool.TryParse(value, out var result) ? result : defaultValue;
        }
    }
}
