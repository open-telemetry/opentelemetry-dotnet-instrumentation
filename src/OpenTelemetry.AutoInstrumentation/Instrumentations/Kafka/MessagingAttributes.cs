// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.Kafka;

// https://github.com/open-telemetry/semantic-conventions/blob/v1.23.0/docs/messaging/messaging-spans.md#messaging-attributes
internal static class MessagingAttributes
{
    internal static class Keys
    {
        public const string MessagingSystem = "messaging.system";
        public const string MessagingOperation = "messaging.operation";
        public const string DestinationName = "messaging.destination.name";
        public const string ClientId = "messaging.client_id";

        // https://github.com/open-telemetry/semantic-conventions/blob/v1.23.0/docs/messaging/kafka.md#span-attributes
        internal static class Kafka
        {
            public const string ConsumerGroupId = "messaging.kafka.consumer.group";
            public const string Partition = "messaging.kafka.destination.partition";
            public const string MessageKey = "messaging.kafka.message.key";
            public const string PartitionOffset = "messaging.kafka.message.offset";
            public const string IsTombstone = "messaging.kafka.message.tombstone";
        }
    }

    internal static class Values
    {
        public const string KafkaMessagingSystemName = "kafka";
        public const string PublishOperationName = "publish";
        public const string ReceiveOperationName = "receive";
    }
}
