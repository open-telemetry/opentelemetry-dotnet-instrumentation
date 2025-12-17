// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NETFRAMEWORK
using System.Globalization;

namespace System.Text;

internal static class StringBuilderExtensions
{
    public static StringBuilder AppendLine(this StringBuilder stringBuilder, CultureInfo cultureInfo, string value)
    {
        return stringBuilder.AppendLine(value);
    }

    public static StringBuilder Append(this StringBuilder stringBuilder, CultureInfo cultureInfo, string value)
    {
        return stringBuilder.AppendLine(value);
    }
}
#endif
