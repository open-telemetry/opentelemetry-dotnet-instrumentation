// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using OpenTelemetry.AutoInstrumentation.CallTarget;
using OpenTelemetry.AutoInstrumentation.DuckTyping;
using OpenTelemetry.AutoInstrumentation.Instrumentations.Kafka.DuckTypes;
using OpenTelemetry.AutoInstrumentation.Util;

// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.Kafka.Integrations;

/// <summary>
/// Kafka sync produce instrumentation
/// </summary>
[InstrumentMethod(
    assemblyName: IntegrationConstants.ConfluentKafkaAssemblyName,
    typeName: IntegrationConstants.ProducerTypeName,
    methodName: IntegrationConstants.ProduceSyncMethodName,
    returnTypeName: ClrNames.Void,
    parameterTypeNames: new[] { IntegrationConstants.TopicPartitionTypeName, IntegrationConstants.MessageTypeName, IntegrationConstants.ActionOfDeliveryReportTypeName },
    minimumVersion: IntegrationConstants.MinVersion,
    maximumVersion: IntegrationConstants.MaxVersion,
    integrationName: IntegrationConstants.IntegrationName,
    type: InstrumentationType.Trace)]
public static class ProducerProduceSyncIntegration
{
    internal static CallTargetState OnMethodBegin<TTarget, TTopicPartition, TMessage, TDeliveryHandler>(
        TTarget instance, TTopicPartition topicPartition, TMessage message, TDeliveryHandler deliveryHandler)
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
            // Store delivery handler as state.
            return new CallTargetState(activity, deliveryHandler);
        }

        return CallTargetState.GetDefault();
    }

    internal static CallTargetReturn OnMethodEnd<TTarget>(TTarget instance, Exception? exception, in CallTargetState state)
    {
        var activity = state.Activity;
        if (activity is null)
        {
            return CallTargetReturn.GetDefault();
        }

        if (exception is not null)
        {
            activity.SetException(exception);
        }

        // If delivery handler was not set, stop the activity.
        if (state.State is null || exception is not null)
        {
            activity.Stop();
        }
        else
        {
            // If delivery handler was set,
            // only set parent as a current activity.
            // Activity will be stopped inside updated
            // delivery handler.
            var current = Activity.Current;
            Activity.Current = current?.Parent;
        }

        return CallTargetReturn.GetDefault();
    }
}
