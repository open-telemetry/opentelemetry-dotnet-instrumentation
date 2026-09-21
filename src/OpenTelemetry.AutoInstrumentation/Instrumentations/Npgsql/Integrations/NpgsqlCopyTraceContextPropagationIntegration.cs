// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0
#if NET

using OpenTelemetry.AutoInstrumentation.CallTarget;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.Npgsql.Integrations;

/// <summary>
/// Npgsql COPY trace context propagation instrumentation.
/// </summary>
[InstrumentMethod(
    assemblyName: NpgsqlConstants.AssemblyName,
    typeName: NpgsqlConstants.ConnectorTypeName,
    methodName: NpgsqlConstants.TraceCopyStartMethodName,
    returnTypeName: NpgsqlConstants.ActivityTypeName,
    parameterTypeNames: [ClrNames.String, ClrNames.String],
    minimumVersion: NpgsqlConstants.TraceCopyStartMinVersion,
    maximumVersion: NpgsqlConstants.MaxVersion,
    integrationName: NpgsqlConstants.IntegrationName,
    type: InstrumentationType.Trace)]
public static class NpgsqlCopyTraceContextPropagationIntegration
{
    internal static CallTargetReturn<TReturn> OnMethodEnd<TTarget, TReturn>(TTarget instance, TReturn returnValue, Exception? exception, in CallTargetState state)
    {
        if (exception is null)
        {
            NpgsqlTraceContextPropagator.PropagateCopyActivity(returnValue, instance);
        }

        return new CallTargetReturn<TReturn>(returnValue);
    }
}
#endif
