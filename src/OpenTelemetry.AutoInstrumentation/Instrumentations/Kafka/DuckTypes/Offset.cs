// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using OpenTelemetry.AutoInstrumentation.DuckTyping;
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.Kafka.DuckTypes;

// wraps https://github.com/confluentinc/confluent-kafka-dotnet/blob/07de95ed647af80a0db39ce6a8891a630423b952/src/Confluent.Kafka/Offset.cs
[DuckCopy]
internal struct Offset
{
    public long Value;
}
