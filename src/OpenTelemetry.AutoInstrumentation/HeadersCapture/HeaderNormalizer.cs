// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.HeadersCapture;

internal static class HeaderNormalizer
{
    public static string Normalize(string httpHeaderName)
    {
#pragma warning disable CA1308 // Normalize to lower-case invariant for HTTP header names
        return httpHeaderName.ToLowerInvariant();
#pragma warning restore CA1308 // Normalize to lower-case invariant for HTTP header names
    }
}
