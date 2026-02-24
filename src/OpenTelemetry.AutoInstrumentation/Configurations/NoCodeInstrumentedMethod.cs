// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using OpenTelemetry.AutoInstrumentation.Instrumentations.NoCode;

namespace OpenTelemetry.AutoInstrumentation.Configurations;

internal class NoCodeInstrumentedMethod
{
    public NoCodeInstrumentedMethod(
        NativeCallTargetDefinition definition,
        string[] signatureTypes,
        string spanName,
        ActivityKind activityKind,
        TagList attributes,
        List<NoCodeDynamicAttribute>? dynamicAttributes = null,
        List<NoCodeStatusRule>? statusRules = null,
        NoCodeFunctionExpression? dynamicSpanName = null)
    {
        Definition = definition;
        SignatureTypes = signatureTypes;
        SpanName = spanName;
        ActivityKind = activityKind;
        Attributes = attributes;
        DynamicAttributes = dynamicAttributes ?? [];
        StatusRules = statusRules ?? [];
        DynamicSpanName = dynamicSpanName;
    }

    public NativeCallTargetDefinition Definition { get; }

    // Not possible to reuse the array from Definition because it is marshalled as IntPtr
    public string[] SignatureTypes { get; }

    public string SpanName { get; }

    public ActivityKind ActivityKind { get; }

    /// <summary>
    /// Gets the static attributes (with fixed values from configuration).
    /// </summary>
    public TagList Attributes { get; }

    /// <summary>
    /// Gets the dynamic attributes (with values extracted from method arguments at runtime).
    /// </summary>
    public List<NoCodeDynamicAttribute> DynamicAttributes { get; }

    /// <summary>
    /// Gets the status rules for setting span status based on return value or other conditions.
    /// </summary>
    public List<NoCodeStatusRule> StatusRules { get; }

    /// <summary>
    /// Gets the dynamic span name expression (if configured), which evaluates to the span name at runtime.
    /// If null, the static SpanName property is used.
    /// </summary>
    public NoCodeFunctionExpression? DynamicSpanName { get; }
}
