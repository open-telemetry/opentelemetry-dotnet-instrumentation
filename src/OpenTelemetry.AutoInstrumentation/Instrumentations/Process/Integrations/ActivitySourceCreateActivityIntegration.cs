// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using OpenTelemetry.AutoInstrumentation.CallTarget;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.Process.Integrations;

/// <summary>
/// Applies process environment trace context before activity sampling and ID generation.
/// </summary>
[InstrumentMethod(
    assemblyName: "System.Diagnostics.DiagnosticSource",
    typeName: "System.Diagnostics.ActivitySource",
    methodName: "CreateActivity",
    returnTypeName: "System.Diagnostics.Activity",
    parameterTypeNames: [ClrNames.String, "System.Diagnostics.ActivityKind", "System.Diagnostics.ActivityContext", ClrNames.String, "System.Collections.Generic.IEnumerable`1[System.Collections.Generic.KeyValuePair`2[System.String,System.Object]]", "System.Collections.Generic.IEnumerable`1[System.Diagnostics.ActivityLink]", "System.DateTimeOffset", ClrNames.Bool, "System.Diagnostics.ActivityIdFormat"],
    minimumVersion: "10.0.0",
    maximumVersion: "10.65535.65535",
    integrationName: "Process",
    type: InstrumentationType.Trace)]
public static class ActivitySourceCreateActivityIntegration
{
    internal static CallTargetState OnMethodBegin<TTarget>(
        TTarget instance,
        string name,
        ActivityKind kind,
        ref ActivityContext context,
        string? parentId,
        IEnumerable<KeyValuePair<string, object?>>? tags,
        ref IEnumerable<ActivityLink>? links,
        DateTimeOffset startTime,
        bool startIt,
        ActivityIdFormat idFormat)
    {
        // Match the factory's no-listener fast path before preparing any links.
        if (instance is ActivitySource source && source.HasListeners())
        {
            ProcessInstrumentation.ApplyToRoot(ref context, parentId, ref links);
        }

        return CallTargetState.GetDefault();
    }
}
