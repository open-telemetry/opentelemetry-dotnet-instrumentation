// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using OpenTelemetry.AutoInstrumentation.Instrumentations.NoCode;
using OpenTelemetry.AutoInstrumentation.Instrumentations.NoCode.Cel;

namespace OpenTelemetry.AutoInstrumentation.Configurations;

internal class NoCodeInstrumentedMethod
{
    public NoCodeInstrumentedMethod(
        NativeCallTargetDefinition2 definition,
        string targetAssembly,
        string targetType,
        string targetMethod,
        string[] signatureTypes,
        string spanName,
        ActivityKind activityKind,
        TagList attributes,
        List<NoCodeDynamicAttribute>? dynamicAttributes = null,
        List<NoCodeStatusRule>? statusRules = null,
        CelExpression? dynamicSpanName = null)
    {
        Definition = definition;
        TargetAssembly = targetAssembly;
        TargetType = targetType;
        TargetMethod = targetMethod;
        SignatureTypes = signatureTypes;
        SpanName = spanName;
        ActivityKind = activityKind;
        Attributes = attributes;
        DynamicAttributes = dynamicAttributes ?? [];
        StatusRules = statusRules ?? [];
        DynamicSpanName = dynamicSpanName;
    }

    public NativeCallTargetDefinition2 Definition { get; }

    public string TargetAssembly { get; }

    public string TargetType { get; }

    public string TargetMethod { get; }

    // The definition stores unmanaged pointers, while no-code instrumentation needs managed values at runtime.
    public string[] SignatureTypes { get; }

    public string SpanName { get; }

    public ActivityKind ActivityKind { get; }

    /// <summary>
    /// Gets the static attributes (with fixed values from configuration).
    /// </summary>
    public TagList Attributes { get; }

    /// <summary>
    /// Gets the dynamic attributes (with values extracted from method arguments at runtime using CEL expressions).
    /// </summary>
    public List<NoCodeDynamicAttribute> DynamicAttributes { get; }

    /// <summary>
    /// Gets the status rules for setting span status based on return value or other conditions.
    /// </summary>
    public List<NoCodeStatusRule> StatusRules { get; }

    /// <summary>
    /// Gets the dynamic span name CEL expression (if configured), which evaluates to the span name at runtime.
    /// If null, the static SpanName property is used.
    /// </summary>
    public CelExpression? DynamicSpanName { get; }
}
