// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.HeadersCapture;

internal static class HeaderNormalizer
{
    public static string NormalizeHeader(string httpHeaderName)
    {
        return httpHeaderName.ToLowerInvariant();
    }
}
