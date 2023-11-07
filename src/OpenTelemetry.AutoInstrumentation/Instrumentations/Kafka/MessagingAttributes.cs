// <copyright file="MessagingAttributes.cs" company="OpenTelemetry Authors">
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

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.Kafka;

// https://github.com/open-telemetry/semantic-conventions/blob/v1.22.0/docs/messaging/messaging-spans.md#messaging-attributes
internal static class MessagingAttributes
{
    internal static class Keys
    {
        public const string MessagingSystem = "messaging.system";
        public const string MessagingOperation = "messaging.operation";
        public const string DestinationName = "messaging.destination.name";
        public const string ClientId = "messaging.client_id";

        // https://github.com/open-telemetry/semantic-conventions/blob/v1.22.0/docs/messaging/kafka.md#span-attributes
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
        public const string ProcessOperationName = "process";
    }
}
