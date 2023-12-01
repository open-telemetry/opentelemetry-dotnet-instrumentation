// <copyright file="KafkaInstrumentation.cs" company="OpenTelemetry Authors">
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
using System.Text;
using OpenTelemetry.AutoInstrumentation.DuckTyping;
using OpenTelemetry.AutoInstrumentation.Instrumentations.Kafka.DuckTypes;
using OpenTelemetry.AutoInstrumentation.Instrumentations.Kafka.Integrations;
using OpenTelemetry.Context.Propagation;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.Kafka;

internal static class KafkaInstrumentation
{
    private static ActivitySource Source { get; } = new("OpenTelemetry.AutoInstrumentation.Kafka");

    public static Activity? StartConsumerActivity(IConsumeResult? consumeResult, DateTimeOffset startTime, object consumer)
    {
        PropagationContext? propagatedContext = null;
        if (consumeResult is not null)
        {
            propagatedContext = Propagators.DefaultTextMapPropagator.Extract(default, consumeResult, MessageHeaderValueGetter);
        }

        string? spanName = null;
        if (!string.IsNullOrEmpty(consumeResult?.Topic))
        {
            spanName = $"{consumeResult?.Topic} {MessagingAttributes.Values.ReceiveOperationName}";
        }

        spanName ??= MessagingAttributes.Values.ReceiveOperationName;

        var activityLinks = propagatedContext is not null && propagatedContext.Value.ActivityContext.IsValid()
            ? new[] { new ActivityLink(propagatedContext.Value.ActivityContext) }
            : Array.Empty<ActivityLink>();
        var activity = Source.StartActivity(name: spanName, kind: ActivityKind.Consumer, links: activityLinks, startTime: startTime);

        if (activity is { IsAllDataRequested: true })
        {
            SetCommonAttributes(
                activity,
                MessagingAttributes.Values.ReceiveOperationName,
                consumeResult?.Topic,
                consumeResult?.Partition,
                consumeResult?.Message?.Key,
                consumer.DuckCast<INamedClient>());

            if (consumeResult is not null)
            {
                activity.SetTag(MessagingAttributes.Keys.Kafka.PartitionOffset, consumeResult.Offset.Value);
            }

            if (ConsumerCache.TryGet(consumer, out var groupId))
            {
                activity.SetTag(MessagingAttributes.Keys.Kafka.ConsumerGroupId, groupId);
            }
        }

        return activity;
    }

    public static Activity? StartProducerActivity(
        ITopicPartition partition,
        IKafkaMessage message,
        INamedClient producer)
    {
        string? spanName = null;
        if (!string.IsNullOrEmpty(partition.Topic))
        {
            spanName = $"{partition.Topic} {MessagingAttributes.Values.PublishOperationName}";
        }

        spanName ??= MessagingAttributes.Values.PublishOperationName;
        var activity = Source.StartActivity(name: spanName, ActivityKind.Producer);
        if (activity is not null && activity.IsAllDataRequested)
        {
            SetCommonAttributes(
                activity,
                MessagingAttributes.Values.PublishOperationName,
                partition.Topic,
                partition.Partition,
                message.Key,
                producer);

            activity.SetTag(MessagingAttributes.Keys.Kafka.IsTombstone, message.Value is null);
        }

        return activity;
    }

    public static void InjectContext<TTopicPartition>(IKafkaMessage message, Activity activity)
    {
        message.Headers ??= MessageHeadersHelper<TTopicPartition>.Create();
        Propagators.DefaultTextMapPropagator.Inject(
            new PropagationContext(activity.Context, Baggage.Current),
            message,
            MessageHeaderValueSetter);
    }

    public static void SetDeliveryResults(Activity activity, IDeliveryResult deliveryResult)
    {
        // Set the final partition message was delivered to.
        activity.SetTag(MessagingAttributes.Keys.Kafka.Partition, deliveryResult.Partition.Value);

        activity.SetTag(
            MessagingAttributes.Keys.Kafka.PartitionOffset,
            deliveryResult.Offset.Value);
    }

    private static void SetCommonAttributes(
        Activity activity,
        string operationName,
        string? topic,
        Partition? partition,
        object? key,
        INamedClient? client)
    {
        activity.SetTag(MessagingAttributes.Keys.MessagingOperation, operationName);
        activity.SetTag(MessagingAttributes.Keys.MessagingSystem, MessagingAttributes.Values.KafkaMessagingSystemName);
        if (!string.IsNullOrEmpty(topic))
        {
            activity.SetTag(MessagingAttributes.Keys.DestinationName, topic);
        }

        if (client is not null)
        {
            activity.SetTag(MessagingAttributes.Keys.ClientId, client.Name);
        }

        if (key is not null)
        {
            activity.SetTag(MessagingAttributes.Keys.Kafka.MessageKey, key);
        }

        if (partition is not null)
        {
            activity.SetTag(MessagingAttributes.Keys.Kafka.Partition, partition.Value.Value);
        }
    }

    private static IEnumerable<string> MessageHeaderValueGetter(IConsumeResult? message, string key)
    {
        if (message?.Message?.Headers is not null && message.Message.Headers.TryGetLastBytes(key, out var bytes))
        {
            return new[] { Encoding.UTF8.GetString(bytes) };
        }

        return Enumerable.Empty<string>();
    }

    private static void MessageHeaderValueSetter(IKafkaMessage msg, string key, string val)
    {
        msg.Headers?.Remove(key);
        msg.Headers?.Add(key, Encoding.UTF8.GetBytes(val));
    }
}
