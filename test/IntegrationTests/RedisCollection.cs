// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET

using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using IntegrationTests.Helpers;
using static IntegrationTests.Helpers.DockerFileHelper;

namespace IntegrationTests;

[CollectionDefinition(Name)]
public class RedisCollection : ICollectionFixture<RedisFixture>
{
    public const string Name = nameof(RedisCollection);
}

public class RedisFixture : IAsyncLifetime
{
    private const int RedisPort = 6379;
    private static readonly string RedisImage = ReadImageFrom("redis.Dockerfile");

    private IContainer? _container;

    public RedisFixture()
    {
        Port = TcpPortProvider.GetOpenPort();
    }

    public int Port { get; }

    public async Task InitializeAsync()
    {
        _container = await LaunchRedisContainerAsync(Port);
    }

    public async Task DisposeAsync()
    {
        if (_container != null)
        {
            await ShutdownRedisContainerAsync(_container);
        }
    }

    private async Task<IContainer> LaunchRedisContainerAsync(int port)
    {
        var containersBuilder = new ContainerBuilder()
            .WithImage(RedisImage)
            .WithName($"redis-{port}")
            .WithPortBinding(port, RedisPort)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(RedisPort));

        var container = containersBuilder.Build();
        await container.StartAsync();

        return container;
    }

    private async Task ShutdownRedisContainerAsync(IContainer container)
    {
        await container.DisposeAsync();
    }
}
#endif
