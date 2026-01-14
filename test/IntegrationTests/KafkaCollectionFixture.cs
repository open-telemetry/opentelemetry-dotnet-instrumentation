// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using IntegrationTests.Helpers;
using static IntegrationTests.Helpers.DockerFileHelper;

namespace IntegrationTests;

[CollectionDefinition(Name)]
public class KafkaCollectionFixture : ICollectionFixture<KafkaFixture>
{
    public const string Name = nameof(KafkaCollectionFixture);
}

/// <summary>
/// Container setup based on https://github.com/confluentinc/kafka-images/blob/83f57e511aead515822334ef28da6872d127c6a2/examples/kafka-single-node/docker-compose.yml
/// </summary>
public class KafkaFixture : IAsyncLifetime
{
    private static readonly string KafkaImage = ReadImageFrom("kafka.Dockerfile");
    private readonly string _kafkaContainerName;
    private readonly string _testNetworkName;
    private IContainer? _kafkaContainer;
    private INetwork? _containerNetwork;

    public KafkaFixture()
    {
        Port = TcpPortProvider.GetOpenPort();
        _kafkaContainerName = "integration-test-kafka" + Port;
        _testNetworkName = $"{_kafkaContainerName}-network";
    }

    public int Port { get; }

    public async Task InitializeAsync()
    {
        _containerNetwork = new NetworkBuilder()
            .WithName(_testNetworkName)
            .Build();
        await _containerNetwork.CreateAsync().ConfigureAwait(false);
        _kafkaContainer = await LaunchKafkaContainer(_containerNetwork).ConfigureAwait(false);
    }

    public async Task DisposeAsync()
    {
        if (_kafkaContainer != null)
        {
            await _kafkaContainer.DisposeAsync().ConfigureAwait(false);
        }

        if (_containerNetwork != null)
        {
            await _containerNetwork.DisposeAsync().ConfigureAwait(false);
        }
    }

    private async Task<IContainer?> LaunchKafkaContainer(INetwork? containerNetwork)
    {
        var container = new ContainerBuilder(KafkaImage)
            .WithName(_kafkaContainerName)
            .WithPortBinding(Port)
            .WithEnvironment("KAFKA_BROKER_ID", "1")
            .WithEnvironment("KAFKA_AUTO_CREATE_TOPICS_ENABLE", "false")
            .WithEnvironment("KAFKA_LISTENER_SECURITY_PROTOCOL_MAP", "PLAINTEXT:PLAINTEXT,PLAINTEXT_HOST:PLAINTEXT,CONTROLLER:PLAINTEXT")
            .WithEnvironment("KAFKA_ADVERTISED_LISTENERS", $"PLAINTEXT://{_kafkaContainerName}:29092,PLAINTEXT_HOST://localhost:{Port}")
            .WithEnvironment("KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR", "1")
            .WithEnvironment("KAFKA_GROUP_INITIAL_REBALANCE_DELAY_MS", "0")
            .WithEnvironment("KAFKA_TRANSACTION_STATE_LOG_MIN_ISR", "1")
            .WithEnvironment("KAFKA_TRANSACTION_STATE_LOG_REPLICATION_FACTOR", "1")
            .WithEnvironment("KAFKA_PROCESS_ROLES", "broker,controller")
            .WithEnvironment("KAFKA_NODE_ID", "1")
            .WithEnvironment("KAFKA_CONTROLLER_QUORUM_VOTERS", $"1@{_kafkaContainerName}:29093")
            .WithEnvironment("KAFKA_LISTENERS", $"PLAINTEXT://{_kafkaContainerName}:29092,CONTROLLER://{_kafkaContainerName}:29093,PLAINTEXT_HOST://0.0.0.0:{Port}")
            .WithEnvironment("KAFKA_INTER_BROKER_LISTENER_NAME", "PLAINTEXT")
            .WithEnvironment("KAFKA_CONTROLLER_LISTENER_NAMES", "CONTROLLER")
            .WithEnvironment("CLUSTER_ID", Guid.NewGuid().ToString())
            .WithNetwork(containerNetwork)
            .Build();

        await container.StartAsync().ConfigureAwait(false);

        return container;
    }
}
