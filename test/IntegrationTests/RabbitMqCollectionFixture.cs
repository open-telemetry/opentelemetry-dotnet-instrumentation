// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using IntegrationTests.Helpers;
using static IntegrationTests.Helpers.DockerFileHelper;

namespace IntegrationTests;

[CollectionDefinition(Name)]
public class RabbitMqCollectionFixture : ICollectionFixture<RabbitMqFixture>
{
    public const string Name = nameof(RabbitMqCollectionFixture);
}

public class RabbitMqFixture : IAsyncLifetime
{
    private const int RabbitMqPort = 5672;
    private static readonly string RabbitMqImage = ReadImageFrom("rabbitmq.Dockerfile");

    private IContainer? _container;

    public RabbitMqFixture()
    {
        if (IsCurrentArchitectureSupported)
        {
            Port = TcpPortProvider.GetOpenPort();
        }
    }

    public bool IsCurrentArchitectureSupported { get; } = EnvironmentTools.IsX64();

    public int Port { get; }

    public async Task InitializeAsync()
    {
        if (!IsCurrentArchitectureSupported)
        {
            return;
        }

        _container = await LaunchRabbitMqContainerAsync(Port).ConfigureAwait(false);
    }

    public async Task DisposeAsync()
    {
        if (_container != null)
        {
            await ShutdownRabbitMqContainerAsync(_container).ConfigureAwait(false);
        }
    }

    public void SkipIfUnsupportedPlatform()
    {
        if (!IsCurrentArchitectureSupported)
        {
            throw new SkipException("RabbitMQ is supported only on AMD64.");
        }
    }

    private static async Task<IContainer> LaunchRabbitMqContainerAsync(int port)
    {
        var containersBuilder = new ContainerBuilder(RabbitMqImage)
            .WithName($"rabbitmq-{port}")
            .WithPortBinding(port, RabbitMqPort)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(RabbitMqPort));

        var container = containersBuilder.Build();
        await container.StartAsync().ConfigureAwait(false);

        return container;
    }

    private static async Task ShutdownRabbitMqContainerAsync(IContainer container)
    {
        await container.DisposeAsync().ConfigureAwait(false);
    }
}
