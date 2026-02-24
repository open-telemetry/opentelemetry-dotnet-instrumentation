// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using OpenTelemetry.AutoInstrumentation.Instrumentations.NoCode;
using OpenTelemetry.AutoInstrumentation.Logging;
using Vendors.YamlDotNet.Serialization;

namespace OpenTelemetry.AutoInstrumentation.Configurations.FileBasedConfiguration;

internal class NoCodeSpan
{
    private static readonly IOtelLogger Log = OtelLogging.GetLogger();

    [YamlMember(Alias = "name")]
    public string? Name { get; set; }

    [YamlMember(Alias = "name_source")]
    public string? NameSource { get; set; }

    [YamlMember(Alias = "kind")]
    public string? Kind { get; set; }

    [YamlMember(Alias = "attributes")]
    public List<YamlAttribute>? Attributes { get; set; }

    [YamlMember(Alias = "status")]
    public NoCodeStatusConfig? Status { get; set; }

    /// <summary>
    /// Parses static attributes (those with a fixed value).
    /// </summary>
    public TagList ParseAttributes()
    {
        if (Attributes == null || Attributes.Count == 0)
        {
            return default;
        }

        // Only parse attributes with Value (static attributes)
        var staticAttributes = Attributes.Where(a => a.Value != null && string.IsNullOrEmpty(a.Source)).ToList();
        return YamlAttribute.ParseAttributes(staticAttributes);
    }

    /// <summary>
    /// Parses dynamic attributes (those with an expression source).
    /// </summary>
    public List<NoCodeDynamicAttribute> ParseDynamicAttributes()
    {
        if (Attributes == null || Attributes.Count == 0)
        {
            return [];
        }

        var dynamicAttributes = new List<NoCodeDynamicAttribute>();

        foreach (var attribute in Attributes)
        {
            if (string.IsNullOrEmpty(attribute.Source))
            {
                continue;
            }

            if (string.IsNullOrEmpty(attribute.Name))
            {
                Log.Debug("Dynamic attribute has source but no name. Skipping.");
                continue;
            }

            // Try to parse as function expression first
            var funcExpression = NoCodeFunctionExpression.Parse(attribute.Source);
            if (funcExpression != null)
            {
                dynamicAttributes.Add(new NoCodeDynamicAttribute(attribute.Name!, funcExpression, attribute.Type));
                continue;
            }

            // Fall back to simple expression
            var expression = NoCodeExpression.Parse(attribute.Source);
            if (expression == null)
            {
                Log.Debug("Failed to parse dynamic attribute expression '{0}' for attribute '{1}'. Skipping.", attribute.Source, attribute.Name);
                continue;
            }

            dynamicAttributes.Add(new NoCodeDynamicAttribute(attribute.Name!, expression, attribute.Type));
        }

        return dynamicAttributes;
    }

    /// <summary>
    /// Parses status rules from configuration.
    /// </summary>
    public List<NoCodeStatusRule> ParseStatusRules()
    {
        if (Status == null || Status.Rules == null || Status.Rules.Count == 0)
        {
            return [];
        }

        var statusRules = new List<NoCodeStatusRule>();

        foreach (var rule in Status.Rules)
        {
            if (string.IsNullOrEmpty(rule.Condition))
            {
                Log.Debug("Status rule has no condition. Skipping.");
                continue;
            }

            var statusCode = rule.Code?.ToUpperInvariant() switch
            {
                "OK" => ActivityStatusCode.Ok,
                "ERROR" => ActivityStatusCode.Error,
                _ => ActivityStatusCode.Unset
            };

            // Try to parse condition as function expression
            var funcCondition = NoCodeFunctionExpression.Parse(rule.Condition);
            if (funcCondition != null)
            {
                statusRules.Add(new NoCodeStatusRule(funcCondition, statusCode, rule.Description));
                continue;
            }

            // Try to parse as simple expression (boolean result expected)
            var simpleCondition = NoCodeExpression.Parse(rule.Condition);
            if (simpleCondition != null)
            {
                statusRules.Add(new NoCodeStatusRule(simpleCondition, statusCode, rule.Description));
                continue;
            }

            Log.Debug("Failed to parse status condition: '{0}'. Skipping.", rule.Condition);
        }

        return statusRules;
    }

    /// <summary>
    /// Parses dynamic span name expression from configuration.
    /// </summary>
    public NoCodeFunctionExpression? ParseDynamicSpanName()
    {
        if (string.IsNullOrEmpty(NameSource))
        {
            return null;
        }

        // Try to parse as function expression
        var funcExpression = NoCodeFunctionExpression.Parse(NameSource);
        if (funcExpression != null)
        {
            return funcExpression;
        }

        // For span names, we only support function expressions (not simple expressions)
        // because span names should always be strings
        Log.Debug("Failed to parse dynamic span name expression '{0}'. Only function expressions are supported for span names.", NameSource);
        return null;
    }
}
