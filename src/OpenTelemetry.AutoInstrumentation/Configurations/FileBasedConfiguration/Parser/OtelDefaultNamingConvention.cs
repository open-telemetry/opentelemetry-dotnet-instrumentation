// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using Vendors.YamlDotNet.Serialization;
using Vendors.YamlDotNet.Serialization.Utilities;

namespace OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration.Parser;

internal sealed class OtelDefaultNamingConvention : INamingConvention
{
    public static readonly INamingConvention Instance = new OtelDefaultNamingConvention();

    private OtelDefaultNamingConvention()
    {
    }

    public string Apply(string value)
    {
        // If the string ends with "Development", replace the ending with "/development"
        const string target = "Development";
        if (value.EndsWith(target, StringComparison.Ordinal) && value.Length > target.Length)
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
