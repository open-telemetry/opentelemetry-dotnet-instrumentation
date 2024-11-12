// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.Instrumentations;

// https://github.com/open-telemetry/semantic-conventions/blob/v1.28.0/docs/messaging/messaging-spans.md#messaging-attributes
internal static class MessagingAttributes
{
    internal static class Keys
    {
        public const string MessagingSystem = "messaging.system";
        public const string MessagingOperationName = "messaging.operation.name";
        public const string MessagingOperationType = "messaging.operation.type";
        public const string MessagingConsumerGroupName = "messaging.consumer.group.name";
        public const string DestinationName = "messaging.destination.name";
        public const string ClientId = "messaging.client_id";
        public const string MessageBodySize = "messaging.message.body.size";
        public const string MessageId = "messaging.message.id";
        public const string ConversationId = "messaging.message.conversation_id";
        public const string Partition = "messaging.destination.partition.id";

        // https://github.com/open-telemetry/semantic-conventions/blob/v1.28.0/docs/messaging/kafka.md#span-attributes
        internal static class Kafka
        {
            public const string MessageKey = "messaging.kafka.message.key";
            public const string PartitionOffset = "messaging.kafka.message.offset";
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
        public const string SendOperation = "send";
        public const string ReceiveOperation = "receive";
        public const string ProcessOperation = "process";

        internal static class Kafka
        {
            public const string MessagingSystemName = "kafka";

            public const string CommitOperationName = "commit";
            public const string PublishOperationName = "commit";
            public const string PollOperationName = "poll";
            public const string ConsumeOperationName = "consume";
        }

        internal static class RabbitMq
        {
            public const string MessagingSystemName = "rabbitmq";
            public const string DefaultExchangeName = "amq.default";

            public const string CommitOperationName = "ack";
            public const string PollOperationName = "nack";
            public const string ConsumeOperationName = "reject";
        }
    }
}
