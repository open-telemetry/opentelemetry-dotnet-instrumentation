// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.CallTarget;
using OpenTelemetry.AutoInstrumentation.Logging;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.Process.Integrations;

/// <summary>
/// Propagates the current context to directly started child processes.
/// </summary>
[InstrumentMethod(
    assemblyName: "System.Diagnostics.Process",
    typeName: "System.Diagnostics.Process",
    methodName: "StartCore",
    returnTypeName: "System.Boolean",
    parameterTypeNames: ["System.Diagnostics.ProcessStartInfo"],
    minimumVersion: "8.0.0",
    maximumVersion: "10.*.*",
    integrationName: "Process",
    type: InstrumentationType.Trace)]
[InstrumentMethod(
    assemblyName: "System",
    typeName: "System.Diagnostics.Process",
    methodName: "StartWithCreateProcess",
    returnTypeName: "System.Boolean",
    parameterTypeNames: ["System.Diagnostics.ProcessStartInfo"],
    minimumVersion: "4.0.0",
    maximumVersion: "4.*.*",
    integrationName: "Process",
    type: InstrumentationType.Trace)]
public static class ProcessStartIntegration
{
    private static readonly IOtelLogger Log = OtelLogging.GetLogger();

    internal static CallTargetState OnMethodBegin<TTarget, TStartInfo>(TTarget instance, ref TStartInfo startInfo)
    {
        if (ProcessInstrumentation.InjectionEnabled && startInfo is not null)
        {
            try
            {
                // Publish the new argument only after preparation has completed. The
                // application keeps its original start info, including on launch failure.
                startInfo = ProcessStartInfoCloner.Prepare(startInfo, ProcessInstrumentation.InjectEnvironment);
            }
            catch (Exception exception)
            {
                Log.Error(exception, "Could not propagate context to the child process environment.");
            }
        }

        return CallTargetState.GetDefault();
    }
}
