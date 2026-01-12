// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using IntegrationTests.Helpers;
using static IntegrationTests.Helpers.DockerFileHelper;

namespace IntegrationTests;

[CollectionDefinition(Name)]
public class PostgresCollectionFixture : ICollectionFixture<PostgresFixture>
{
    public const string Name = nameof(PostgresCollectionFixture);
}

public class PostgresFixture : IAsyncLifetime
{
    private const int PostgresPort = 5432;
    private static readonly string PostgresImage = ReadImageFrom("postgres.Dockerfile");

    private IContainer? _container;

    public PostgresFixture()
    {
        Port = TcpPortProvider.GetOpenPort();
    }

    public int Port { get; }

    public async Task InitializeAsync()
    {
        _container = await LaunchPostgresContainerAsync(Port).ConfigureAwait(false);
    }

    public async Task DisposeAsync()
    {
        if (_container != null)
        {
            await ShutdownPostgresContainerAsync(_container).ConfigureAwait(false);
        }
    }

    private static async Task<IContainer> LaunchPostgresContainerAsync(int port)
    {
        var containersBuilder = new ContainerBuilder()
            .WithImage(PostgresImage)
            .WithName($"postgres-{port}")
            .WithPortBinding(port, PostgresPort)
            .WithEnvironment("POSTGRES_HOST_AUTH_METHOD", "trust")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(PostgresPort));

        var container = containersBuilder.Build();
        await container.StartAsync().ConfigureAwait(false);

        return container;
    }

    private static async Task ShutdownPostgresContainerAsync(IContainer container)
    {
        await container.DisposeAsync().ConfigureAwait(false);
    }
}
