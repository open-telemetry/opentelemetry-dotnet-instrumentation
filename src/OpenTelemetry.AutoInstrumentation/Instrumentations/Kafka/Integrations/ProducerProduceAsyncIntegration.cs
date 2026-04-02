// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.CallTarget;
using OpenTelemetry.AutoInstrumentation.DuckTyping;
using OpenTelemetry.AutoInstrumentation.Instrumentations.Kafka.DuckTypes;
using OpenTelemetry.AutoInstrumentation.Util;

// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.Kafka.Integrations;

/// <summary>
/// Producer ProduceAsync integration
/// </summary>
[InstrumentMethod(
assemblyName: IntegrationConstants.ConfluentKafkaAssemblyName,
typeName: IntegrationConstants.ProducerTypeName,
methodName: IntegrationConstants.ProduceAsyncMethodName,
returnTypeName: IntegrationConstants.TaskOfDeliveryReportTypeName,
parameterTypeNames: [IntegrationConstants.TopicPartitionTypeName, IntegrationConstants.MessageTypeName, ClrNames.CancellationToken],
minimumVersion: IntegrationConstants.MinVersion,
maximumVersion: IntegrationConstants.MaxVersion,
integrationName: IntegrationConstants.IntegrationName,
type: InstrumentationType.Trace)]
public static class ProducerProduceAsyncIntegration
{
    internal static CallTargetState OnMethodBegin<TTarget, TTopicPartition, TMessage>(
        TTarget instance, TTopicPartition topicPartition, TMessage message, CancellationToken cancellationToken)
        where TMessage : IKafkaMessage, IDuckType
    {
        // Duck type created for message is a struct.
        if (message.Instance is null || topicPartition is null)
        {
            // Exit early if provided parameters are invalid.
            return CallTargetState.GetDefault();
        }

        var activity = KafkaInstrumentation.StartProducerActivity(topicPartition.DuckCast<ITopicPartition>(), message, instance.DuckCast<INamedClient>()!);
        if (activity is not null)
        {
            KafkaInstrumentation.InjectContext<TTopicPartition, TMessage>(message, activity);
            return new CallTargetState(activity);
        }

        return CallTargetState.GetDefault();
    }

    internal static TReturn OnAsyncMethodEnd<TTarget, TReturn>(
        TTarget instance, TReturn returnValue, Exception? exception, in CallTargetState state)
    where TReturn : IDeliveryResult
    {
        var activity = state.Activity;
        if (activity is null)
        {
            return returnValue;
        }

        IDeliveryResult? deliveryResult;
        if (exception is not null && exception.TryDuckCast<IProduceException>(out var produceException))
        {
            deliveryResult = produceException.DeliveryResult;
        }
        else
        {
            deliveryResult = returnValue;
        }

        if (deliveryResult is not null)
        {
            KafkaInstrumentation.SetDeliveryResults(activity, deliveryResult);
        }

        if (exception is not null)
        {
            activity.SetException(exception);
        }

        activity.Stop();

        return returnValue;
    }
}
