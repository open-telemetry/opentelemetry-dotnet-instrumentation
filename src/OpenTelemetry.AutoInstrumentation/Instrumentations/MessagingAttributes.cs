// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.Instrumentations;

internal static class MessagingAttributes
{
    internal static class Keys
    {
        // Names unchanged between v1.23.0 and v1.44.0.
        public const string MessagingSystem = "messaging.system";
        public const string DestinationName = "messaging.destination.name";

        // https://github.com/open-telemetry/semantic-conventions/blob/v1.23.0/docs/messaging/messaging-spans.md#messaging-attributes
        // Used by the RabbitMQ instrumentation, which has not been migrated yet.
        public const string MessagingOperation = "messaging.operation";
        public const string MessageBodySize = "messaging.message.body.size";
        public const string MessageId = "messaging.message.id";
        public const string ConversationId = "messaging.message.conversation_id";

        // https://github.com/open-telemetry/semantic-conventions/blob/v1.44.0/docs/messaging/messaging-spans.md
        public const string MessagingOperationName = "messaging.operation.name";
        public const string MessagingOperationType = "messaging.operation.type";
        public const string ClientId = "messaging.client.id";
        public const string ConsumerGroupName = "messaging.consumer.group.name";
        public const string DestinationPartitionId = "messaging.destination.partition.id";

        // https://github.com/open-telemetry/semantic-conventions/blob/v1.44.0/docs/messaging/kafka.md
        internal static class Kafka
        {
            public const string MessageKey = "messaging.kafka.message.key";
            public const string Offset = "messaging.kafka.offset";
            public const string IsTombstone = "messaging.kafka.message.tombstone";
        }

        internal static class RabbitMq
        {
            public const string RoutingKey = "messaging.rabbitmq.destination.routing_key";
            public const string DeliveryTag = "messaging.rabbitmq.delivery_tag";
        }
    }

    internal static class Values
    {
        public const string KafkaMessagingSystemName = "kafka";

        // Same value in v1.23.0 and v1.44.0.
        public const string ReceiveOperationName = "receive";

        // v1.23.0 operation names, used by the RabbitMQ instrumentation.
        public const string PublishOperationName = "publish";
        public const string DeliverOperationName = "deliver";

        // v1.44.0 replaces "publish" with "send" for producer operations.
        public const string SendOperationName = "send";

        internal static class RabbitMq
        {
            public const string MessagingSystemName = "rabbitmq";
            public const string DefaultExchangeName = "amq.default";
        }
    }
}
