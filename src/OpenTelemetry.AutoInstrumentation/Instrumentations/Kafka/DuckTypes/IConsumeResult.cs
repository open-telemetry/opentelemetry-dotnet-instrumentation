// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.Kafka.DuckTypes;

// wraps https://github.com/confluentinc/confluent-kafka-dotnet/blob/07de95ed647af80a0db39ce6a8891a630423b952/src/Confluent.Kafka/ConsumeResult.cs
internal interface IConsumeResult
{
    public IKafkaMessage? Message { get; }

    public string? Topic { get; set; }

    public Offset Offset { get; set; }

    public Partition Partition { get; set; }
}
