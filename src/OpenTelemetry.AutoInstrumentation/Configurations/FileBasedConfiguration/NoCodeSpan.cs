// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using System.Globalization;
using OpenTelemetry.AutoInstrumentation.Logging;
using Vendors.YamlDotNet.Serialization;

namespace OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;

internal class NoCodeSpan
{
    private static readonly IOtelLogger Log = OtelLogging.GetLogger();

    [YamlMember(Alias = "name")]
    public string? Name { get; set; }

    [YamlMember(Alias = "kind")]
    public string? Kind { get; set; }

    [YamlMember(Alias = "attributes")]
    public List<NoCodeAttribute>? Attributes { get; set; }

    public TagList ParseAttributes()
    {
        var attributes = Attributes;

        TagList tagList = default;
        if (attributes == null || attributes.Count == 0)
        {
            return tagList;
        }

        for (var i = 0; i < attributes.Count; i++)
        {
            var attribute = attributes[i];

            if (string.IsNullOrEmpty(attribute.Name))
            {
                Log.Debug($"NoCode - attribute name is null or empty. Skipping.");
                continue;
            }

            var attributeName = attribute.Name!;
            var attributeValue = attribute.Value;

            switch (attribute.Type)
            {
                case "string":
                    if (attributeValue is string)
                    {
                        tagList.Add(attributeName, attributeValue);
                    }
                    else
                    {
                        Log.Debug("NoCode - attribute is marked as string but the value does not looks like a string '{0}'. Skipping.", attributeValue);
                    }

                    continue;
                case "bool":
                    if (attributeValue is bool boolValue)
                    {
                        tagList.Add(attributeName, boolValue);
                    }
                    else if (attributeValue is string boolStringValue && bool.TryParse(boolStringValue, out var parsedBool))
                    {
                        tagList.Add(attributeName, parsedBool);
                    }
                    else
                    {
                        Log.Debug("NoCode - attribute is marked as bool but the value does not looks like a bool '{0}'. Skipping.", attributeValue);
                    }

                    continue;
                case "int":
                    if (attributeValue is long intValue)
                    {
                        tagList.Add(attributeName, intValue);
                    }
                    else if (attributeValue is string intStringValue && long.TryParse(intStringValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedInt))
                    {
                        tagList.Add(attributeName, parsedInt);
                    }
                    else
                    {
                        Log.Debug("NoCode - attribute is marked as int but the value does not looks like an int '{0}'. Skipping.", attributeValue);
                    }

                    continue;
                case "double":
                    if (attributeValue is double doubleValue)
                    {
                        tagList.Add(attributeName, doubleValue);
                    }
                    else if (attributeValue is string doubleStringValue && double.TryParse(doubleStringValue, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var parsedDouble))
                    {
                        tagList.Add(attributeName, parsedDouble);
                    }
                    else
                    {
                        Log.Debug("NoCode - attribute is marked as double but the value does not looks like a double '{0}'. Skipping.", attributeValue);
                    }

                    continue;

                case "string_array":
                    if (attributeValue is List<object> stringValues)
                    {
                        var strings = new string[stringValues.Count];
                        var nonStringValue = false;
                        for (var j = 0; j < stringValues.Count; j++)
                        {
                            if (stringValues[j] is string str)
                            {
                                strings[j] = str;
                            }
                            else
                            {
                                nonStringValue = true;
                                Log.Debug("NoCode - attribute is marked as string_array but one of the values does not looks like a string '{0}'. Skipping the whole array.", stringValues[j]);
                                break;
                            }
                        }

                        if (!nonStringValue)
                        {
                            tagList.Add(attributeName, strings);
                        }
                    }
                    else
                    {
                        Log.Debug("NoCode - attribute is marked as string_array but the values does not looks like strings '{0}'. Skipping.", attributeValue);
                    }

                    continue;
                case "bool_array":
                    if (attributeValue is List<object> objectBoolValues)
                    {
                        var booleans = new bool[objectBoolValues.Count];
                        var nonBooleanValue = false;
                        for (var j = 0; j < objectBoolValues.Count; j++)
                        {
                            if (objectBoolValues[j] is bool boolValueFromArray)
                            {
                                booleans[j] = boolValueFromArray;
                            }
                            else if (objectBoolValues[j] is string boolStringValue && bool.TryParse(boolStringValue, out var parsedBool))
                            {
                                booleans[j] = parsedBool;
                            }
                            else
                            {
                                nonBooleanValue = true;
                                Log.Debug("NoCode - attribute is marked as bool_array but one of the values does not looks like a bool '{0}'. Skipping the whole array.", objectBoolValues[j]);
                                break;
                            }
                        }

                        if (!nonBooleanValue)
                        {
                            tagList.Add(attributeName, booleans);
                        }
                    }
                    else
                    {
                        Log.Debug("NoCode - attribute is marked as bool_array but the value does not looks like a booleans '{0}'. Skipping.", attributeValue);
                    }

                    continue;
                case "int_array":
                    Log.Debug("NoCode - attribute is marked as int_array. It is not supported yet. Skipping.", attributeValue);
                    continue;
                case "double_array":
                    Log.Debug("NoCode - attribute is marked as double_array. It is not supported yet. Skipping.", attributeValue);
                    continue;

                default:
                    Log.Debug("NoCode - attribute type is not recognized '{0}'. Skipping.", attribute.Type);
                    continue;
            }
        }

        return tagList;
    }
}
