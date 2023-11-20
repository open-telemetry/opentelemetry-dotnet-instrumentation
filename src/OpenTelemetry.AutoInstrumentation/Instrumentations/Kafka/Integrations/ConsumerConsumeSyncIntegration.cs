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
        string? spanName = null;
        if (!string.IsNullOrEmpty(consumeResult.Topic))
        {
            spanName = $"{consumeResult.Topic} {MessagingAttributes.Values.ReceiveOperationName}";
        }

        spanName ??= MessagingAttributes.Values.ReceiveOperationName;

        var activityLinks = propagatedContext.ActivityContext.IsValid()
                ? new[] { new ActivityLink(propagatedContext.ActivityContext) }
                : Array.Empty<ActivityLink>();
        var startTime = (DateTimeOffset)state.StartTime!;
        var activity = KafkaCommon.Source.StartActivity(name: spanName, kind: ActivityKind.Consumer, links: activityLinks, startTime: startTime);

        if (activity is { IsAllDataRequested: true })
        {
            KafkaCommon.SetCommonAttributes(
                activity,
                MessagingAttributes.Values.ReceiveOperationName,
                consumeResult.Topic,
                consumeResult.Partition,
                consumeResult.Message?.Key,
                instance.DuckCast<INamedClient>());

            activity.SetTag(MessagingAttributes.Keys.Kafka.PartitionOffset, consumeResult.Offset.Value);

            if (ConsumerCache.TryGet(instance!, out var groupId))
            {
                activity.SetTag(MessagingAttributes.Keys.Kafka.ConsumerGroupId, groupId);
            }
        }

        activity?.Stop();

        return new CallTargetReturn<TResponse>(response);
    }
}
