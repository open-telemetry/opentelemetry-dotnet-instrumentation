// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.CallTarget;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.Process.Integrations;

/// <summary>
/// Propagates context through the .NET 11 process start implementation with explicit handles.
/// </summary>
[InstrumentMethod(
    assemblyName: "System.Diagnostics.Process",
    typeName: "System.Diagnostics.Process",
    methodName: "StartCore",
    returnTypeName: "System.Boolean",
    parameterTypeNames: ["System.Diagnostics.ProcessStartInfo", "Microsoft.Win32.SafeHandles.SafeFileHandle", "Microsoft.Win32.SafeHandles.SafeFileHandle", "Microsoft.Win32.SafeHandles.SafeFileHandle", "System.Runtime.InteropServices.SafeHandle[]"],
    minimumVersion: "11.0.0",
    maximumVersion: "11.*.*",
    integrationName: "Process",
    type: InstrumentationType.Trace)]
public static class ProcessStartWithHandlesIntegration
{
    internal static CallTargetState OnMethodBegin<TTarget, TStartInfo>(TTarget instance, ref TStartInfo startInfo, object? stdinHandle, object? stdoutHandle, object? stderrHandle, object? inheritedHandles)
    {
        return ProcessStartIntegration.OnMethodBegin(instance, ref startInfo);
    }
}
