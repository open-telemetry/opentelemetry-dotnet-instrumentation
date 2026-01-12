// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using IntegrationTests.Helpers;
using static IntegrationTests.Helpers.DockerFileHelper;

namespace IntegrationTests;

[CollectionDefinition(Name)]
public class MySqlCollection : ICollectionFixture<MySqlFixture>
{
    public const string Name = nameof(MySqlCollection);
}

public class MySqlFixture : IAsyncLifetime
{
    private const int MySqlPort = 3306;
    private static readonly string MySqlImage = ReadImageFrom("mysql.Dockerfile");

    private IContainer? _container;

    public MySqlFixture()
    {
        Port = TcpPortProvider.GetOpenPort();
    }

    public int Port { get; }

    public async Task InitializeAsync()
    {
        _container = await LaunchMySqlContainerAsync(Port).ConfigureAwait(false);
    }

    public async Task DisposeAsync()
    {
        if (_container != null)
        {
            await ShutdownMySqlContainerAsync(_container).ConfigureAwait(false);
        }
    }

    private static async Task<IContainer> LaunchMySqlContainerAsync(int port)
    {
        var containersBuilder = new ContainerBuilder()
            .WithImage(MySqlImage)
            .WithName($"mysql-{port}")
            .WithPortBinding(port, MySqlPort)
            .WithEnvironment("MYSQL_ALLOW_EMPTY_PASSWORD", "true")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(MySqlPort));

        var container = containersBuilder.Build();
        await container.StartAsync().ConfigureAwait(false);

        return container;
    }

    private static async Task ShutdownMySqlContainerAsync(IContainer container)
    {
        await container.DisposeAsync().ConfigureAwait(false);
    }
}
