// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Globalization;
using OpenTelemetry.AutoInstrumentation.Logging;
using Vendors.YamlDotNet.Serialization;

namespace OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;

internal class ResourceConfiguration
{
    private static readonly IOtelLogger Log = OtelLogging.GetLogger();

    /// <summary>
    /// Gets or sets the list of resource attributes.
    /// </summary>
    [YamlMember(Alias = "attributes")]
    public List<ResourceAttribute>? Attributes { get; set; }

    /// <summary>
    /// Gets or sets the attributes list for the resource.
    /// </summary>
    [YamlMember(Alias = "attributes_list")]
    public string? AttributesList { get; set; }

    /// <summary>
    /// Gets or sets the detection development configuration.
    /// </summary>
    [YamlMember(Alias = "detection/development")]
    public DetectionDevelopment? DetectionDevelopment { get; set; }

    public List<KeyValuePair<string, object>> ParseAttributes()
    {
        var resourceAttributesWithPriority = new Dictionary<string, object>();

        if (Attributes != null)
        {
            foreach (var attr in Attributes)
            {
                if (string.IsNullOrEmpty(attr.Name))
                {
                    Log.Debug("NoCode - attribute name is null or empty. Skipping.");
                    continue;
                }

                var attributeName = attr.Name!;
                var attributeValue = attr.Value;

                switch (attr.Type)
                {
                    case "string":
                        if (attributeValue is string strValue)
                        {
                            resourceAttributesWithPriority.Add(attributeName, strValue);
                        }
                        else
                        {
                            Log.Debug("NoCode - attribute is marked as string but value is not a string '{0}'. Skipping.", attributeValue);
                        }

                        break;

                    case "bool":
                        if (attributeValue is bool boolValue)
                        {
                            resourceAttributesWithPriority.Add(attributeName, boolValue);
                        }
                        else if (attributeValue is string boolStr && bool.TryParse(boolStr, out var parsedBool))
                        {
                            resourceAttributesWithPriority.Add(attributeName, parsedBool);
                        }
                        else
                        {
                            Log.Debug("NoCode - attribute is marked as bool but value is not a bool '{0}'. Skipping.", attributeValue);
                        }

                        break;

                    case "int":
                        if (attributeValue is long longValue)
                        {
                            resourceAttributesWithPriority.Add(attributeName, longValue);
                        }
                        else if (attributeValue is string longStr && long.TryParse(longStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedLong))
                        {
                            resourceAttributesWithPriority.Add(attributeName, parsedLong);
                        }
                        else
                        {
                            Log.Debug("NoCode - attribute is marked as int but value is not an integer '{0}'. Skipping.", attributeValue);
                        }

                        break;

                    case "double":
                        if (attributeValue is double dblValue)
                        {
                            resourceAttributesWithPriority.Add(attributeName, dblValue);
                        }
                        else if (attributeValue is string dblStr && double.TryParse(dblStr, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var parsedDouble))
                        {
                            resourceAttributesWithPriority.Add(attributeName, parsedDouble);
                        }
                        else
                        {
                            Log.Debug("NoCode - attribute is marked as double but value is not a double '{0}'. Skipping.", attributeValue);
                        }

                        break;

                    case "string_array":
                        if (attributeValue is List<object> stringList)
                        {
                            var allStrings = stringList.All(v => v is string);
                            if (allStrings)
                            {
                                resourceAttributesWithPriority.Add(attributeName, stringList.Cast<string>().ToArray());
                            }
                            else
                            {
                                Log.Debug("NoCode - attribute is marked as string_array but one or more values are not strings. Skipping.");
                            }
                        }
                        else
                        {
                            Log.Debug("NoCode - attribute is marked as string_array but value is not a list '{0}'. Skipping.", attributeValue);
                        }

                        break;

                    case "bool_array":
                        if (attributeValue is List<object> boolList)
                        {
                            var result = new List<bool>();
                            var valid = true;
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
                                    valid = false;
                                    Log.Debug("NoCode - attribute is marked as bool_array but element '{0}' is invalid. Skipping the whole array.", val);
                                    break;
                                }
                            }

                            if (valid)
                            {
                                resourceAttributesWithPriority.Add(attributeName, result.ToArray());
                            }
                        }
                        else
                        {
                            Log.Debug("NoCode - attribute is marked as bool_array but value is not a list '{0}'. Skipping.", attributeValue);
                        }

                        break;

                    case "int_array":
                        if (attributeValue is List<object> intList)
                        {
                            var result = new List<long>();
                            var valid = true;
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
                                    valid = false;
                                    Log.Debug("NoCode - attribute is marked as int_array but element '{0}' is invalid. Skipping the whole array.", val);
                                    break;
                                }
                            }

                            if (valid)
                            {
                            resourceAttributesWithPriority.Add(attributeName, result.ToArray());
                            }
                        }
                        else
                        {
                            Log.Debug("NoCode - attribute is marked as int_array but value is not a list '{0}'. Skipping.", attributeValue);
                        }

                        break;

                    case "double_array":
                        if (attributeValue is List<object> dblList)
                        {
                            var result = new List<double>();
                            var valid = true;
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
                                    valid = false;
                                    Log.Debug("NoCode - attribute is marked as double_array but element '{0}' is invalid. Skipping the whole array.", val);
                                    break;
                                }
                            }

                            if (valid)
                            {
                                resourceAttributesWithPriority.Add(attributeName, result.ToArray());
                            }
                        }
                        else
                        {
                            Log.Debug("NoCode - attribute is marked as double_array but value is not a list '{0}'. Skipping.", attributeValue);
                        }

                        break;

                    default:
                        Log.Debug("NoCode - attribute type is not recognized '{0}'. Skipping.", attr.Type);
                        break;
                }
            }
        }

        if (AttributesList != null)
        {
            const char attributeListSplitter = ',';
            char[] attributeKeyValueSplitter = ['='];

            var rawAttributes = AttributesList.Split(attributeListSplitter);
            foreach (var rawKeyValuePair in rawAttributes)
            {
                var keyValuePair = rawKeyValuePair.Split(attributeKeyValueSplitter, 2);
                if (keyValuePair.Length != 2)
                {
                    continue;
                }

                var key = keyValuePair[0].Trim();

                if (!resourceAttributesWithPriority.ContainsKey(key))
                {
                    resourceAttributesWithPriority.Add(key, keyValuePair[1].Trim());
                }
            }
        }

        return resourceAttributesWithPriority.ToList();
    }
}
