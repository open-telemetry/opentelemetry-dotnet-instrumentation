// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Specialized;
using System.Diagnostics;
using System.Net.Http.Headers;
#if NET
using Microsoft.AspNetCore.Http;
#endif

namespace OpenTelemetry.AutoInstrumentation.HeadersCapture;

internal static class ActivityExtensions
{
    public static void AddHeadersAsTags(this Activity activity, IReadOnlyList<AdditionalTag> additionalTags, NameValueCollection headers)
    {
        if (!activity.IsAllDataRequested)
        {
            return;
        }

        for (var i = 0; i < additionalTags.Count; i++)
        {
            var additionalTag = additionalTags[i];
            var headerValues = headers.GetValues(additionalTag.Key);

            if (headerValues == null)
            {
                continue;
            }

            if (headerValues.Length == 1)
            {
                activity.SetTag(additionalTag.TagName, headerValues[0]);
            }
            else
            {
                activity.SetTag(additionalTag.TagName, headerValues);
            }
        }
    }

#if NET
    public static void AddHeadersAsTags(this Activity activity, IReadOnlyList<AdditionalTag> additionalTags, IHeaderDictionary headers)
    {
        if (!activity.IsAllDataRequested)
        {
            return;
        }

        for (var i = 0; i < additionalTags.Count; i++)
        {
            var additionalTag = additionalTags[i];

            if (!headers.TryGetValue(additionalTag.Key, out var headerValues))
            {
                continue;
            }

            if (headerValues.Count == 1)
            {
                activity.SetTag(additionalTag.TagName, headerValues[0]);
            }
            else
            {
                activity.SetTag(additionalTag.TagName, headerValues);
            }
        }
    }
#endif

    public static void AddHeadersAsTags(this Activity activity, IReadOnlyList<AdditionalTag> additionalTags, HttpHeaders headers)
    {
        if (!activity.IsAllDataRequested)
        {
            return;
        }

        for (var i = 0; i < additionalTags.Count; i++)
        {
            var additionalTag = additionalTags[i];

            if (!headers.TryGetValues(additionalTag.Key, out var headerValues))
            {
                continue;
            }

            var headerValuesAsArray = headerValues.ToArray();

            if (headerValuesAsArray.Length == 1)
            {
                activity.SetTag(additionalTag.TagName, headerValuesAsArray[0]);
            }
            else
            {
                activity.SetTag(additionalTag.TagName, headerValuesAsArray);
            }
        }
    }
}
