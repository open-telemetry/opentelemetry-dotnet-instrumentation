// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

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
    public const string ProduceAsyncMethodName = "ProduceAsync";
    public const string ConsumeSyncMethodName = "Consume";
    public const string DisposeMethodName = "Dispose";
    public const string CloseMethodName = "Close";
    public const string TopicPartitionTypeName = "Confluent.Kafka.TopicPartition";
    public const string MessageTypeName = "Confluent.Kafka.Message`2[!0,!1]";
    public const string ActionOfDeliveryReportTypeName = "System.Action`1[Confluent.Kafka.DeliveryReport`2[!0,!1]]";
    public const string ConsumeResultTypeName = "Confluent.Kafka.ConsumeResult`2[!0,!1]";
    public const string TaskOfDeliveryReportTypeName = "System.Threading.Tasks.Task`1[Confluent.Kafka.DeliveryReport`2[!0,!1]]";
}
