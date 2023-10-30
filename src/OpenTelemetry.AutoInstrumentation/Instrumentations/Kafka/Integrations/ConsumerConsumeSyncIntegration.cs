// <copyright file="ConsumerConsumeSyncIntegration.cs" company="OpenTelemetry Authors">
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
using OpenTelemetry.Context.Propagation;

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
        // Attempting to consume another message marks the end
        // of a previous message processing
        KafkaCommon.StopCurrentConsumerActivity();

        return CallTargetState.GetDefault();
    }

    internal static CallTargetReturn<TResponse> OnMethodEnd<TTarget, TResponse>(TTarget instance, TResponse response, Exception? exception, in CallTargetState state)
    {
        IConsumeResult? consumeResult;
        if (exception is not null && exception.TryDuckCast<IResultException>(out var resultException))
        {
            consumeResult = resultException.ConsumerRecord;
        }
        else
        {
            consumeResult = response == null ? null : response.DuckAs<IConsumeResult>();
        }

        if (consumeResult is null)
        {
            return new CallTargetReturn<TResponse>(response);
        }

        var propagatedContext = Propagators.DefaultTextMapPropagator.Extract(default, consumeResult, KafkaCommon.MessageHeaderValueGetter);
        var name = $"{consumeResult.Topic} {MessagingTags.Values.ProcessOperationName}";
        var activity = KafkaCommon.Source.StartActivity(name, ActivityKind.Consumer, propagatedContext.ActivityContext);

        if (activity is { IsAllDataRequested: true })
        {
            if (ConsumerCache.TryGet(instance!, out var groupId))
            {
                KafkaCommon.SetCommonTags(
                    activity,
                    MessagingTags.Values.ProcessOperationName,
                    consumeResult.Topic,
                    consumeResult.Partition.Value,
                    consumeResult.Message.Key,
                    instance.DuckCast<IClientName>()!);

                activity.SetTag(MessagingTags.Keys.Kafka.ConsumerGroupId, groupId);
                activity.SetTag(MessagingTags.Keys.Kafka.PartitionOffset, consumeResult.Offset.Value);
            }
        }

        return new CallTargetReturn<TResponse>(response);
    }
}
