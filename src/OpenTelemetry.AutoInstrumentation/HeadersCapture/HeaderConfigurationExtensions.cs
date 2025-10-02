// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Configurations;

namespace OpenTelemetry.AutoInstrumentation.HeadersCapture;

internal static class HeaderConfigurationExtensions
{
    public static IReadOnlyList<AdditionalTag> ParseHeaders(this Configuration source, string key, Func<string, AdditionalTag> stringToHeaderCacheConverter)
    {
        var headers = source.ParseList(key, Constants.ConfigurationValues.Separator);

        if (headers.Count == 0)
        {
            return [];
        }

        return headers.Select(stringToHeaderCacheConverter).ToArray();
    }

    public static IReadOnlyList<AdditionalTag> ParseHeaders(string? headersList, Func<string, AdditionalTag> stringToHeaderCacheConverter)
    {
        if (string.IsNullOrWhiteSpace(headersList))
        {
            return [];
        }

        var headers = headersList!.Split([Constants.ConfigurationValues.Separator], StringSplitOptions.RemoveEmptyEntries);

        if (headers.Length == 0)
        {
            return [];
        }

        return headers.Select(stringToHeaderCacheConverter).ToArray();
    }
}
