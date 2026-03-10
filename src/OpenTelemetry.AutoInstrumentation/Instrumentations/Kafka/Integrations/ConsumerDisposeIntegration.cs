// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.CallTarget;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.Kafka.Integrations;

/// <summary>
/// Kafka consumer dispose instrumentation
/// </summary>
[InstrumentMethod(
    assemblyName: IntegrationConstants.ConfluentKafkaAssemblyName,
    typeName: IntegrationConstants.ConsumerTypeName,
    methodName: IntegrationConstants.DisposeMethodName,
    returnTypeName: ClrNames.Void,
    parameterTypeNames: [],
    minimumVersion: IntegrationConstants.MinVersion,
    maximumVersion: IntegrationConstants.MaxVersion,
    integrationName: IntegrationConstants.IntegrationName,
    type: InstrumentationType.Trace)]
public static class ConsumerDisposeIntegration
{
    internal static CallTargetReturn OnMethodEnd<TTarget>(TTarget instance, Exception? exception, in CallTargetState state)
    {
        ConsumerCache.Remove(instance!);
        return CallTargetReturn.GetDefault();
    }
}
