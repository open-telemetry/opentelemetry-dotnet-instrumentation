// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Globalization;
using OpenTelemetry.AutoInstrumentation.Logging;
using Vendors.YamlDotNet.Serialization;

namespace OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;

internal class ResourceAttribute
{
    private static readonly IOtelLogger Log = OtelLogging.GetLogger();

    /// <summary>
    /// Gets or sets the name of the resource attribute.
    /// </summary>
    [YamlMember(Alias = "name")]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the value of the resource attribute.
    /// </summary>
    [YamlMember(Alias = "value")]
    public object? Value { get; set; }

    /// <summary>
    /// Gets or sets the type of the resource attribute.
    /// </summary>
    [YamlMember(Alias = "type")]
    public string Type { get; set; } = "string";

    public static bool TryParseAttribute(ResourceAttribute attribute, out string name, out object? value)
    {
        name = attribute.Name ?? string.Empty;
        value = null;

        if (string.IsNullOrEmpty(name))
        {
            Log.Debug("NoCode - attribute name is null or empty. Skipping.");
            return false;
        }

        var attributeValue = attribute.Value;

        switch (attribute.Type)
        {
            case "string":
                if (attributeValue is string strValue)
                {
                    value = strValue;
                    return true;
                }

                Log.Debug("NoCode - attribute is marked as string but value is not a string '{0}'. Skipping.", attributeValue);
                return false;
            case "bool":
                if (attributeValue is bool boolValue)
                {
                    value = boolValue;
                    return true;
                }

                if (attributeValue is string boolStr && bool.TryParse(boolStr, out var parsedBool))
                {
                    value = parsedBool;
                    return true;
                }

                Log.Debug("NoCode - attribute is marked as bool but value is not a bool '{0}'. Skipping.", attributeValue);
                return false;
            case "int":
                if (attributeValue is long longValue)
                {
                    value = longValue;
                    return true;
                }

                if (attributeValue is string longStr && long.TryParse(longStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedLong))
                {
                    value = parsedLong;
                    return true;
                }

                Log.Debug("NoCode - attribute is marked as int but value is not an integer '{0}'. Skipping.", attributeValue);
                return false;
            case "double":
                if (attributeValue is double dblValue)
                {
                    value = dblValue;
                    return true;
                }

                if (attributeValue is string dblStr && double.TryParse(dblStr, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var parsedDouble))
                {
                    value = parsedDouble;
                    return true;
                }

                Log.Debug("NoCode - attribute is marked as double but value is not a double '{0}'. Skipping.", attributeValue);
                return false;
            case "string_array":
                if (attributeValue is List<object> stringList && stringList.All(v => v is string))
                {
                    value = stringList.Cast<string>().ToArray();
                    return true;
                }

                Log.Debug("NoCode - attribute is marked as string_array but contains invalid values '{0}'. Skipping.", attributeValue);
                return false;
            case "bool_array":
                if (attributeValue is List<object> boolList)
                {
                    var result = new List<bool>();
                    foreach (var val in boolList)
                    {
                        if (val is bool b)
                        {
                            result.Add(b);
                        }
                        else if (val is string bs && bool.TryParse(bs, out var parsed))
                        {
                            result.Add(parsed);
                        }
                        else
                        {
                            Log.Debug("NoCode - attribute is marked as bool_array but element '{0}' is invalid. Skipping.", val);
                            return false;
                        }
                    }

                    value = result.ToArray();
                    return true;
                }

                return false;
            case "int_array":
                if (attributeValue is List<object> intList)
                {
                    var result = new List<long>();
                    foreach (var val in intList)
                    {
                        if (val is long l)
                        {
                            result.Add(l);
                        }
                        else if (val is string ls && long.TryParse(ls, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
                        {
                            result.Add(parsed);
                        }
                        else
                        {
                            Log.Debug("NoCode - attribute is marked as int_array but element '{0}' is invalid. Skipping.", val);
                            return false;
                        }
                    }

                    value = result.ToArray();
                    return true;
                }

                return false;
            case "double_array":
                if (attributeValue is List<object> dblList)
                {
                    var result = new List<double>();
                    foreach (var val in dblList)
                    {
                        if (val is double d)
                        {
                            result.Add(d);
                        }
                        else if (val is string ds && double.TryParse(ds, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var parsed))
                        {
                            result.Add(parsed);
                        }
                        else
                        {
                            Log.Debug("NoCode - attribute is marked as double_array but element '{0}' is invalid. Skipping.", val);
                            return false;
                        }
                    }

                    value = result.ToArray();
                    return true;
                }

                return false;
            default:
                Log.Debug("NoCode - attribute type is not recognized '{0}'. Skipping.", attribute.Type);
                return false;
        }
    }
}
