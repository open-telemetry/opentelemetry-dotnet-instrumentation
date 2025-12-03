// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Vendors.YamlDotNet.Serialization;
using Vendors.YamlDotNet.Serialization.Utilities;

namespace OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration.Parser;

internal sealed class OtelDefaultNamingConvention : INamingConvention
{
#pragma warning disable CS0618 // Type or member is obsolete
    public static readonly INamingConvention Instance = new OtelDefaultNamingConvention();
#pragma warning restore CS0618 // Type or member is obsolete

    [Obsolete("Use the Instance static field instead of creating new instances")]
    public OtelDefaultNamingConvention()
    {
    }

    public string Apply(string value)
    {
        // If the string ends with "Development", replace the ending with "/development"
        const string target = "Development";
        if (value != null &&
            value.EndsWith(target, StringComparison.Ordinal) &&
            value.Length > target.Length &&
            char.IsUpper(value, value.Length - target.Length))
        {
            var prefix = value.Substring(0, value.Length - target.Length);
            value = prefix + "/development";
        }

        return value.FromCamelCase("_");
    }

    public string Reverse(string value)
    {
        var result = value.ToPascalCase();
        return result;
    }
}
