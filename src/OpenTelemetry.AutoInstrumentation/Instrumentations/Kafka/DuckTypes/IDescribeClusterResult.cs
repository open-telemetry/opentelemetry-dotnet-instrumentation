// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.Kafka.DuckTypes;

// wraps https://github.com/confluentinc/confluent-kafka-dotnet/blob/master/src/Confluent.Kafka/Admin/DescribeClusterResult.cs
internal interface IDescribeClusterResult
{
    public string ClusterId { get; }
}
