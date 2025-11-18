// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using System.Globalization;
using OpenTelemetry.AutoInstrumentation.Logging;
using Vendors.YamlDotNet.Serialization;

namespace OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;

internal class YamlAttribute
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

    public static TagList ParseAttributes(List<YamlAttribute>? attributes)
    {
        TagList container = default;

        if (attributes == null)
        {
            return container;
        }

        foreach (var attribute in attributes)
        {
            var name = attribute.Name ?? string.Empty;

            if (string.IsNullOrEmpty(name))
            {
                Log.Debug("Attribute name is null or empty. Skipping.");
                continue;
            }

            var attributeValue = attribute.Value;

            switch (attribute.Type)
            {
                case "string":
                    if (attributeValue is string strValue)
                    {
                        container.Add(name, strValue);
                        continue;
                    }

                    Log.Debug("Attribute is marked as string but value is not a string '{0}'. Skipping.", attributeValue);
                    continue;

                case "bool":
                    if (attributeValue is bool boolValue)
                    {
                        container.Add(name, boolValue);
                        continue;
                    }

                    if (attributeValue is string boolStr && bool.TryParse(boolStr, out var parsedBool))
                    {
                        container.Add(name, parsedBool);
                        continue;
                    }

                    Log.Debug("Attribute is marked as bool but value is not a bool '{0}'. Skipping.", attributeValue);
                    continue;

                case "int":
                    if (attributeValue is long longValue)
                    {
                        container.Add(name, longValue);
                        continue;
                    }

                    if (attributeValue is string longStr && long.TryParse(longStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedLong))
                    {
                        container.Add(name, parsedLong);
                        continue;
                    }

                    Log.Debug("Attribute is marked as int but value is not an integer '{0}'. Skipping.", attributeValue);
                    continue;

                case "double":
                    if (attributeValue is double dblValue)
                    {
                        container.Add(name, dblValue);
                        continue;
                    }

                    if (attributeValue is string dblStr && double.TryParse(dblStr, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var parsedDouble))
                    {
                        container.Add(name, parsedDouble);
                        continue;
                    }

                    Log.Debug("Attribute is marked as double but value is not a double '{0}'. Skipping.", attributeValue);
                    continue;

                case "string_array":
                    if (attributeValue is List<object> stringList && stringList.All(v => v is string))
                    {
                        container.Add(name, stringList.Cast<string>().ToArray());
                        continue;
                    }

                    Log.Debug("Attribute is marked as string_array but contains invalid values '{0}'. Skipping.", attributeValue);
                    continue;

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
                                Log.Debug("Attribute is marked as bool_array but element '{0}' is invalid. Skipping.", val);
                                result.Clear();
                                break;
                            }
                        }

                        if (result.Count == boolList.Count)
                        {
                            container.Add(name, result.ToArray());
                        }

                        continue;
                    }

                    continue;

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
                                Log.Debug("Attribute is marked as int_array but element '{0}' is invalid. Skipping.", val);
                                result.Clear();
                                break;
                            }
                        }

                        if (result.Count == intList.Count)
                        {
                            container.Add(name, result.ToArray());
                        }

                        continue;
                    }

                    continue;

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
                                Log.Debug("Attribute is marked as double_array but element '{0}' is invalid. Skipping.", val);
                                result.Clear();
                                break;
                            }
                        }

                        if (result.Count == dblList.Count)
                        {
                            container.Add(name, result.ToArray());
                        }

                        continue;
                    }

                    continue;

                default:
                    Log.Debug("Attribute type is not recognized '{0}'. Skipping.", attribute.Type);
                    continue;
            }
        }

        return container;
    }
}
