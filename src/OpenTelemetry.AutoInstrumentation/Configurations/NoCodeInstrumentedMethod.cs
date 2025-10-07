// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;

namespace OpenTelemetry.AutoInstrumentation.Configurations;

internal class NoCodeInstrumentedMethod
{
    public NoCodeInstrumentedMethod(NativeCallTargetDefinition definition, string[] signatureTypes, string spanName, ActivityKind activityKind, TagList attributes)
    {
        Definition = definition;
        SignatureTypes = signatureTypes;
        SpanName = spanName;
        ActivityKind = activityKind;
        Attributes = attributes;
    }

    public NativeCallTargetDefinition Definition { get; }

    // Not possible to reuse the array from Definition because it is marshalled as IntPtr
    public string[] SignatureTypes { get; }

    public string SpanName { get; }

    public ActivityKind ActivityKind { get; }

    public TagList Attributes { get; }
}
