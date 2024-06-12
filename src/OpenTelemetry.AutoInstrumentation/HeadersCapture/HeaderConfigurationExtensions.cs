// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Configurations;

namespace OpenTelemetry.AutoInstrumentation.HeadersCapture;

internal static class HeaderConfigurationExtensions
{
    public static IReadOnlyList<AdditionalTag> ParseHeaders(this Configuration source, string key, Func<string, AdditionalTag> stringToHeaderCacheConverter)
    {
        var headers = source.ParseList(key, ',');

        if (headers.Count == 0)
        {
            return Array.Empty<AdditionalTag>();
        }

        return headers.Select(stringToHeaderCacheConverter).ToArray();
    }
}
