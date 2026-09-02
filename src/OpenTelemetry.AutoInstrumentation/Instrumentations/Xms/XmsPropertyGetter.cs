// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.Instrumentations.Xms.DuckTypes;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.Xms;

internal static class XmsPropertyGetter
{
    public static IEnumerable<string>? GetValues<T>(T carrier, string key)
        where T : IXmsPropertyContext
    {
        try
        {
            var sanitizedKey = XmsMessagePropertyNames.Sanitize(key);

            if (!carrier.PropertyExists(sanitizedKey))
            {
                return null;
            }

            var value = carrier.GetStringProperty(sanitizedKey);

            return value is null ? null : new[] { value };
        }
        catch
        {
            return null;
        }
    }
}
