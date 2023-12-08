// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.CallTarget;
using OpenTelemetry.AutoInstrumentation.DuckTyping;
using OpenTelemetry.AutoInstrumentation.Instrumentations.Kafka.DuckTypes;
using OpenTelemetry.AutoInstrumentation.Util;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.Kafka.Integrations;

/// <summary>
/// Kafka consumer sync consume instrumentation
/// </summary>
[InstrumentMethod(
    assemblyName: IntegrationConstants.ConfluentKafkaAssemblyName,
    typeName: IntegrationConstants.ConsumerTypeName,
    methodName: IntegrationConstants.ConsumeSyncMethodName,
    returnTypeName: IntegrationConstants.ConsumeResultTypeName,
    parameterTypeNames: new[] { ClrNames.Int32 },
    minimumVersion: IntegrationConstants.MinVersion,
    maximumVersion: IntegrationConstants.MaxVersion,
    integrationName: IntegrationConstants.IntegrationName,
    type: InstrumentationType.Trace)]
public static class ConsumerConsumeSyncIntegration
{
    internal static CallTargetState OnMethodBegin<TTarget>(TTarget instance, int timeout)
    {
        // Preferably, activity would be started here,
        // and link to propagated context later;
        // this is currently not possible, so capture start time
        // to use it when starting activity to get approximate
        // activity duration.
        // TODO: accurate on .NET, but not .NET Fx
        return new CallTargetState(null, null, DateTime.UtcNow);
    }

    internal static CallTargetReturn<TResponse> OnMethodEnd<TTarget, TResponse>(TTarget instance, TResponse response, Exception? exception, in CallTargetState state)
    {
        IConsumeResult? consumeResult;
        if (exception is not null && exception.TryDuckCast<IConsumeException>(out var consumeException))
        {
            consumeResult = consumeException.ConsumerRecord;
        }
        else
        {
            consumeResult = response == null ? null : response.DuckAs<IConsumeResult>();
        }

        var activity = KafkaInstrumentation.StartConsumerActivity(consumeResult, (DateTimeOffset)state.StartTime!, instance!);
        if (exception is not null)
        {
            activity.SetException(exception);
        }

        activity?.Stop();

        return new CallTargetReturn<TResponse>(response);
    }
}
