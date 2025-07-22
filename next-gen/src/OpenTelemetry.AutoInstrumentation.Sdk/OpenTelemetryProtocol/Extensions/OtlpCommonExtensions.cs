// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Globalization;

using Google.Protobuf.Collections;

namespace OpenTelemetry.Proto.Common.V1;

internal static class OtlpCommonExtensions
{
    public static void AddRange(
        this RepeatedField<KeyValue> otlpAttributes,
        ReadOnlySpan<KeyValuePair<string, object?>> attributes)
    {
        foreach (KeyValuePair<string, object?> attribute in attributes)
        {
            otlpAttributes.Add(
                new KeyValue()
                {
                    Key = attribute.Key,
                    Value = new AnyValue()
                    {
                        StringValue = Convert.ToString(attribute.Value, CultureInfo.InvariantCulture) // todo: support other types
                    }
                });
        }
    }

    public static void AddRange(
        this RepeatedField<KeyValue> otlpAttributes,
        in TagList tagList)
    {
        for (int i = 0; i < tagList.Count; i++)
        {
            KeyValuePair<string, object?> attribute = tagList[i];

            otlpAttributes.Add(
                new KeyValue()
                {
                    Key = attribute.Key,
                    Value = new AnyValue()
                    {
                        StringValue = Convert.ToString(attribute.Value, CultureInfo.InvariantCulture) // todo: support other types
                    }
                });
        }
    }
}
