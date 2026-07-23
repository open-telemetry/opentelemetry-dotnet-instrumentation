// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.AutoInstrumentation.Instrumentations.Kafka.Integrations;

internal sealed class KafkaConstructorFetchState
{
    internal KafkaConstructorFetchState(string bootstrapServers, List<KeyValuePair<string, string>>? config)
    {
        BootstrapServers = bootstrapServers;
        Config = config;
    }

    internal string BootstrapServers { get; }

    internal List<KeyValuePair<string, string>>? Config { get; }
}
