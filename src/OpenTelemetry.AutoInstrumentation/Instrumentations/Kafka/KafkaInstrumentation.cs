// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using System.Globalization;
using System.Text;
using OpenTelemetry.AutoInstrumentation.DuckTyping;
using OpenTelemetry.AutoInstrumentation.Instrumentations.Kafka.DuckTypes;
using OpenTelemetry.AutoInstrumentation.Instrumentations.Kafka.Integrations;
using OpenTelemetry.Context.Propagation;

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.Kafka;

internal static class KafkaInstrumentation
{
    private static readonly ActivitySource Source =
        new(new ActivitySourceOptions("OpenTelemetry.AutoInstrumentation.Kafka")
        {
            Version = AutoInstrumentationVersion.Version,
            TelemetrySchemaUrl = GenericAttributes.SchemaUrl1440
        });

    public static Activity? StartConsumerActivity(object consumer)
    {
        var activity = Source.StartActivity(name: string.Empty, kind: ActivityKind.Consumer);

        if (activity is { IsAllDataRequested: true })
        {
            var client = consumer.DuckCast<INamedClient>();
            if (client is not null)
            {
                activity.SetTag(MessagingAttributes.Keys.ClientId, client.Name);
            }

            if (ConsumerCache.TryGet(consumer, out var groupId))
            {
                activity.SetTag(MessagingAttributes.Keys.ConsumerGroupName, groupId);
            }
        }

        return activity;
    }

    public static void EndConsumerActivity(Activity activity, IConsumeResult consumeResult)
    {
        var spanName = GetActivityName(consumeResult.Topic, MessagingAttributes.Values.ReceiveOperationName);
        activity.DisplayName = spanName;

        var activityLinks = GetActivityLinks(consumeResult);

        foreach (var activityLink in activityLinks)
        {
            activity.AddLink(activityLink);
        }

        SetCommonAttributes(
               activity,
               MessagingAttributes.Values.ReceiveOperationName,
               consumeResult.Topic,
               consumeResult.Partition,
               consumeResult.Message?.Key,
               null);

        activity.SetTag(MessagingAttributes.Keys.Kafka.Offset, consumeResult.Offset.Value);
    }

    public static Activity? StartProducerActivity<TTopicPartition, TMessage, TClient>(
        TTopicPartition partition,
        TMessage message,
        TClient producer)
    where TTopicPartition : ITopicPartition
    where TMessage : IKafkaMessage
    where TClient : INamedClient
    {
        var spanName = GetActivityName(partition.Topic, MessagingAttributes.Values.SendOperationName);

        var activity = Source.StartActivity(name: spanName, ActivityKind.Producer);
        if (activity is not null && activity.IsAllDataRequested)
        {
            SetCommonAttributes(
                activity,
                MessagingAttributes.Values.SendOperationName,
                partition.Topic,
                partition.Partition,
                message.Key,
                producer);

            activity.SetTag(MessagingAttributes.Keys.Kafka.IsTombstone, message.Value is null);
        }

        return activity;
    }

    public static void InjectContext<TTopicPartition, TMessage>(TMessage message, Activity activity)
    where TMessage : IKafkaMessage
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
        SetDestinationPartitionId(activity, deliveryResult.Partition);

        activity.SetTag(
            MessagingAttributes.Keys.Kafka.Offset,
            deliveryResult.Offset.Value);
    }

    internal static string? ExtractMessageKeyValue(object key)
    {
        return key switch
        {
            string s => s,
            int or uint or long or ulong or float or double or decimal => Convert.ToString(key, CultureInfo.InvariantCulture),
            _ => null
        };
    }

    private static ActivityLink[] GetActivityLinks(IConsumeResult consumeResult)
    {
        var propagatedContext = Propagators.DefaultTextMapPropagator.Extract(default, consumeResult, MessageHeaderValueGetter);

        return propagatedContext.ActivityContext.IsValid() ? [new ActivityLink(propagatedContext.ActivityContext)] : [];
    }

    private static string GetActivityName(string? destination, string operationName)
    {
        return string.IsNullOrEmpty(destination) ? operationName : $"{operationName} {destination}";
    }

    // messaging.destination.partition.id is a string, unlike the messaging.kafka.destination.partition
    // int it replaces.
    private static void SetDestinationPartitionId(Activity activity, Partition partition)
    {
        activity.SetTag(
            MessagingAttributes.Keys.DestinationPartitionId,
            partition.Value.ToString(CultureInfo.InvariantCulture));
    }

    private static void SetCommonAttributes(
        Activity activity,
        string operationName,
        string? topic,
        Partition? partition,
        object? key,
        INamedClient? client)
    {
        activity.SetTag(MessagingAttributes.Keys.MessagingOperationName, operationName);

        // Kafka has no operation whose type differs from its system-specific name.
        activity.SetTag(MessagingAttributes.Keys.MessagingOperationType, operationName);
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
            var keyValue = ExtractMessageKeyValue(key);
            if (keyValue is not null)
            {
                activity.SetTag(MessagingAttributes.Keys.Kafka.MessageKey, keyValue);
            }
        }

        if (partition is not null)
        {
            SetDestinationPartitionId(activity, partition.Value);
        }
    }

    private static IEnumerable<string> MessageHeaderValueGetter(IConsumeResult? message, string key)
    {
        if (message?.Message?.Headers is not null && message.Message.Headers.TryGetLastBytes(key, out var bytes))
        {
            return [Encoding.UTF8.GetString(bytes)];
        }

        return [];
    }

    private static void MessageHeaderValueSetter<TMessage>(TMessage msg, string key, string val)
    where TMessage : IKafkaMessage
    {
        msg.Headers?.Remove(key);
        msg.Headers?.Add(key, Encoding.UTF8.GetBytes(val));
    }
}
