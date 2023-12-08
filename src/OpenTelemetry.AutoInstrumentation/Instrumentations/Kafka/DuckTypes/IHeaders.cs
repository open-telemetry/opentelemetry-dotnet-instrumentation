// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.Kafka.DuckTypes;

// wraps https://github.com/confluentinc/confluent-kafka-dotnet/blob/07de95ed647af80a0db39ce6a8891a630423b952/src/Confluent.Kafka/Headers.cs
internal interface IHeaders
{
    public void Add(string name, byte[] value);

    public void Remove(string name);

    public bool TryGetLastBytes(string key, out byte[] lastHeader);
}
