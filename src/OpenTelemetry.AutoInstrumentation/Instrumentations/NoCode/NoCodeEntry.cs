// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.NoCode;

internal class NoCodeEntry
{
    public NoCodeEntry(string targetAssembly, string targetType, string targetMethod, string[] targetSignatureTypes, string spanName)
    {
        TargetAssembly = targetAssembly;
        TargetType = targetType;
        TargetMethod = targetMethod;
        TargetSignatureTypes = targetSignatureTypes;
        SpanName = spanName;
    }

    public string TargetAssembly { get; }

    public string TargetType { get; }

    public string TargetMethod { get; }

    public string[] TargetSignatureTypes { get; }

    public string SpanName { get; }
}
