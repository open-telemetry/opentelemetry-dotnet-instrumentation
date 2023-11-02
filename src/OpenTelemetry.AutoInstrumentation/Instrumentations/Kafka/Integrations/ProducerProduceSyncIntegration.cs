// <copyright file="ProducerProduceSyncIntegration.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

using System.Diagnostics;
using OpenTelemetry.AutoInstrumentation.CallTarget;
using OpenTelemetry.AutoInstrumentation.DuckTyping;
using OpenTelemetry.AutoInstrumentation.Instrumentations.Kafka.DuckTypes;
using OpenTelemetry.AutoInstrumentation.Util;
using OpenTelemetry.Context.Propagation;
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
        where TTopicPartition : ITopicPartition, IDuckType
        where TMessage : IKafkaMessage, IDuckType
    {
        // duck types created for message and topicPartition are structs
        if (message.Instance is null || topicPartition.Instance is null)
        {
            // invalid parameters, exit early
            return CallTargetState.GetDefault();
        }

        string? spanName = null;
        if (!string.IsNullOrEmpty(topicPartition.Topic))
        {
            spanName = $"{topicPartition.Topic} {MessagingAttributes.Values.PublishOperationName}";
        }

        spanName ??= MessagingAttributes.Values.PublishOperationName;
        var activity = KafkaCommon.Source.StartActivity(name: spanName, ActivityKind.Producer);
        if (activity is not null)
        {
            Propagators.DefaultTextMapPropagator.Inject<IKafkaMessage>(
                new PropagationContext(activity.Context, Baggage.Current),
                message,
                KafkaCommon.MessageHeaderValueSetter);

            if (activity.IsAllDataRequested)
            {
                KafkaCommon.SetCommonAttributes(
                    activity,
                    MessagingAttributes.Values.PublishOperationName,
                    topicPartition.Topic,
                    topicPartition.Partition,
                    message.Key,
                    instance.DuckCast<IClientName>()!);

                activity.SetTag(MessagingAttributes.Keys.Kafka.IsTombstone, message.Value is null);
            }

            // Store as state information if delivery handler was set
            return new CallTargetState(activity, deliveryHandler is null);
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

        // If delivery handler was not set, stop the activity
        if (state.State is true)
        {
            activity.Stop();
        }
        else
        {
            // If delivery handler was set,
            // only set parent as a current activity.
            // Activity will be stopped inside updated
            // delivery handler
            var current = Activity.Current;
            Activity.Current = current?.Parent;
        }

        return CallTargetReturn.GetDefault();
    }
}
