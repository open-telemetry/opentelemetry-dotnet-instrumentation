// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using IntegrationTests.Helpers;
using static IntegrationTests.Helpers.DockerFileHelper;

namespace IntegrationTests;

[CollectionDefinition(Name)]
public class KafkaCollection : ICollectionFixture<KafkaFixture>
{
    public const string Name = nameof(KafkaCollection);
}

/// <summary>
/// Container setup based on https://github.com/confluentinc/kafka-images/blob/83f57e511aead515822334ef28da6872d127c6a2/examples/kafka-single-node/docker-compose.yml
/// </summary>
public class KafkaFixture : IAsyncLifetime
{
    private const int ZookeeperClientPort = 2181;
    private static readonly string ZooKeeperImage = ReadImageFrom("zookeeper.Dockerfile");
    private static readonly string KafkaImage = ReadImageFrom("kafka.Dockerfile");
    private readonly string _kafkaContainerName;
    private readonly string _testNetworkName;
    private readonly string _zookeeperContainerName;
    private IContainer? _kafkaContainer;
    private IContainer? _zooKeeperContainer;
    private INetwork? _containerNetwork;

    public KafkaFixture()
    {
        Port = TcpPortProvider.GetOpenPort();
        _kafkaContainerName = "integration-test-kafka" + Port;
        _testNetworkName = $"{_kafkaContainerName}-network";
        _zookeeperContainerName = $"{_kafkaContainerName}-zookeeper";
    }

    public int Port { get; }

    public async Task InitializeAsync()
    {
        _containerNetwork = new NetworkBuilder()
            .WithName(_testNetworkName)
            .Build();
        await _containerNetwork.CreateAsync();
        _zooKeeperContainer = await LaunchZookeeper(_containerNetwork);
        _kafkaContainer = await LaunchKafkaContainer(_containerNetwork, _zooKeeperContainer);
    }

    public async Task DisposeAsync()
    {
        if (_kafkaContainer != null)
        {
            await _kafkaContainer.DisposeAsync();
        }

        if (_zooKeeperContainer != null)
        {
            await _zooKeeperContainer.DisposeAsync();
        }

        if (_containerNetwork != null)
        {
            await _containerNetwork.DisposeAsync();
        }
    }

    private async Task<IContainer?> LaunchZookeeper(INetwork? containerNetwork)
    {
        var container = new ContainerBuilder()
            .WithImage(ZooKeeperImage)
            .WithName(_zookeeperContainerName)
            .WithEnvironment("ZOOKEEPER_CLIENT_PORT", ZookeeperClientPort.ToString())
            .WithEnvironment("ZOOKEEPER_TICK_TIME", "2000")
            .WithNetwork(containerNetwork)
            .Build();
        await container.StartAsync();

        return container;
    }

    private async Task<IContainer?> LaunchKafkaContainer(
        INetwork? containerNetwork,
        IContainer? zooKeeperContainer)
    {
        // returned container name starts with '/'
        var zookeeperContainerName = zooKeeperContainer?.Name.Substring(1);
        var container = new ContainerBuilder()
            .WithImage(KafkaImage)
            .WithName(_kafkaContainerName)
            .WithPortBinding(Port)
            .WithEnvironment("KAFKA_BROKER_ID", "1")
            .WithEnvironment("KAFKA_AUTO_CREATE_TOPICS_ENABLE", "false")
            .WithEnvironment("KAFKA_ZOOKEEPER_CONNECT", $"{zookeeperContainerName}:{ZookeeperClientPort}")
            .WithEnvironment("KAFKA_ADVERTISED_LISTENERS", $"PLAINTEXT://{_kafkaContainerName}:29092,PLAINTEXT_HOST://localhost:{Port}")
            .WithEnvironment("KAFKA_LISTENER_SECURITY_PROTOCOL_MAP", "PLAINTEXT:PLAINTEXT,PLAINTEXT_HOST:PLAINTEXT")
            .WithEnvironment("KAFKA_INTER_BROKER_LISTENER_NAME", "PLAINTEXT")
            .WithEnvironment("KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR", "1")
            .WithNetwork(containerNetwork)
            .Build();

        await container.StartAsync();

        return container;
    }
}
