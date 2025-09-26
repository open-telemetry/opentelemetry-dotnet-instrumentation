// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.Configurations;

internal class NoCodeInstrumentedMethod
{
    public NoCodeInstrumentedMethod(NativeCallTargetDefinition definition, string[] signatureTypes, string spanName)
    {
        Definition = definition;
        SignatureTypes = signatureTypes;
        SpanName = spanName;
    }

    public NativeCallTargetDefinition Definition { get; }

    // Not possible to reuse the array from Definition because it is marshalled as IntPtr
    public string[] SignatureTypes { get; }

    public string SpanName { get; }
}
