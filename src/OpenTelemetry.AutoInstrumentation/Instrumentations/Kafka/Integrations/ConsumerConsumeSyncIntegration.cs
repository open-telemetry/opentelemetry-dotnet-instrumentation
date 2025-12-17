// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
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
    parameterTypeNames: [ClrNames.Int32],
    minimumVersion: IntegrationConstants.MinVersion,
    maximumVersion: IntegrationConstants.MaxVersion,
    integrationName: IntegrationConstants.IntegrationName,
    type: InstrumentationType.Trace)]
public static class ConsumerConsumeSyncIntegration
{
    internal static CallTargetState OnMethodBegin<TTarget>(TTarget instance, int timeout)
    {
        var activity = KafkaInstrumentation.StartConsumerActivity(instance!);
        return new CallTargetState(activity, null);
    }

    internal static CallTargetReturn<TResponse> OnMethodEnd<TTarget, TResponse>(TTarget instance, TResponse response, Exception? exception, in CallTargetState state)
    {
        var activity = state.Activity;
        if (activity is null)
        {
            return new CallTargetReturn<TResponse>(response);
        }

        IConsumeResult? consumeResult;
        if (exception is not null && exception.TryDuckCast<IConsumeException>(out var consumeException))
        {
            consumeResult = consumeException.ConsumerRecord;
        }
        else
        {
            consumeResult = response?.DuckAs<IConsumeResult>();
        }

        if (consumeResult is not null && !consumeResult.IsPartitionEOF)
        {
            KafkaInstrumentation.EndConsumerActivity(activity, consumeResult);
            if (exception is not null)
            {
                activity.SetException(exception);
            }
        }
        else
        {
            activity.ActivityTraceFlags = ActivityTraceFlags.None;
        }

        activity.Stop();

        return new CallTargetReturn<TResponse>(response);
    }
}
