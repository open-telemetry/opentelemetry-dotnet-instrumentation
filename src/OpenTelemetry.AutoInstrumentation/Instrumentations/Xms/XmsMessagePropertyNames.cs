// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Text;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.Xms;

internal static class XmsMessagePropertyNames
{
    public static string Sanitize(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            return key;
        }

        var builder = new StringBuilder(key.Length);

        for (var i = 0; i < key.Length; i++)
        {
            var c = key[i];
            var isValidChar = (c >= 'A' && c <= 'Z') ||
                               (c >= 'a' && c <= 'z') ||
                               (c >= '0' && c <= '9') ||
                               c == '_';

            builder.Append(isValidChar ? c : '_');
        }

        if (builder.Length > 0 && builder[0] >= '0' && builder[0] <= '9')
        {
            builder.Insert(0, '_');
        }

        return builder.ToString();
    }
}
