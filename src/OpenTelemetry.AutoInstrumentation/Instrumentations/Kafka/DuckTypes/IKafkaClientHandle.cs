// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.Kafka.DuckTypes;

// wraps https://github.com/confluentinc/confluent-kafka-dotnet/blob/master/src/Confluent.Kafka/IClient.cs
internal interface IKafkaClientHandle
{
    public object Handle { get; }
}
