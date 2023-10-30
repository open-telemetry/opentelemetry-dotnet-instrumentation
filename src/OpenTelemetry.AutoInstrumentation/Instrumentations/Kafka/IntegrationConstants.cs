// <copyright file="IntegrationConstants.cs" company="OpenTelemetry Authors">
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

internal static class IntegrationConstants
{
    public const string IntegrationName = "Kafka";
    public const string MinVersion = "1.4.0";
    public const string MaxVersion = "2.*.*";
    public const string ConfluentKafkaAssemblyName = "Confluent.Kafka";
    public const string ProducerTypeName = "Confluent.Kafka.Producer`2";
    public const string ProducerDeliveryHandlerShimTypeName = "Confluent.Kafka.Producer`2+TypedDeliveryHandlerShim_Action";
    public const string ConsumerTypeName = "Confluent.Kafka.Consumer`2";
    public const string ConsumerBuilderTypeName = "Confluent.Kafka.ConsumerBuilder`2[!0,!1]";
    public const string ProduceSyncMethodName = "Produce";
    public const string ConsumeSyncMethodName = "Consume";
    public const string DisposeMethodName = "Dispose";
    public const string CloseMethodName = "Close";
    public const string TopicPartitionTypeName = "Confluent.Kafka.TopicPartition";
    public const string MessageTypeName = "Confluent.Kafka.Message`2[!0,!1]";
    public const string ActionOfDeliveryReportTypeName = "System.Action`1[Confluent.Kafka.DeliveryReport`2[!0,!1]]";
    public const string ConsumeResultTypeName = "Confluent.Kafka.ConsumeResult`2[!0,!1]";
    public const string UnsubscribeMethodName = "Unsubscribe";
}
