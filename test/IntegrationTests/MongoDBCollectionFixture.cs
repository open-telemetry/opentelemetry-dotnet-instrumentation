// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using IntegrationTests.Helpers;
using static IntegrationTests.Helpers.DockerFileHelper;

namespace IntegrationTests;

[CollectionDefinition(Name)]
public class MongoDBCollectionFixture : ICollectionFixture<MongoDBFixture>
{
    public const string Name = nameof(MongoDBCollectionFixture);
}

public class MongoDBFixture : IAsyncLifetime
{
    private const int MongoDBPort = 27017;
    private static readonly string MongoDBImage = ReadImageFrom("mongodb.Dockerfile");

    private IContainer? _container;

    public MongoDBFixture()
    {
        Port = TcpPortProvider.GetOpenPort();
    }

    public int Port { get; }

    public async Task InitializeAsync()
    {
        _container = await LaunchMongoContainerAsync(Port).ConfigureAwait(false);
    }

    public async Task DisposeAsync()
    {
        if (_container != null)
        {
            await ShutdownMongoContainerAsync(_container).ConfigureAwait(false);
        }
    }

    private static async Task<IContainer> LaunchMongoContainerAsync(int port)
    {
        var waitForOs = await GetWaitForOSTypeAsync().ConfigureAwait(false);
        var mongoContainersBuilder = new ContainerBuilder()
            .WithImage(MongoDBImage)
            .WithName($"mongo-db-{port}")
            .WithPortBinding(port, MongoDBPort)
            .WithWaitStrategy(waitForOs.UntilInternalTcpPortIsAvailable(MongoDBPort));

        var container = mongoContainersBuilder.Build();
        await container.StartAsync().ConfigureAwait(false);

        return container;
    }

    private static async Task ShutdownMongoContainerAsync(IContainer container)
    {
        await container.DisposeAsync().ConfigureAwait(false);
    }

    private static async Task<IWaitForContainerOS> GetWaitForOSTypeAsync()
    {
#if _WINDOWS
        var isWindowsEngine = await DockerSystemHelper.GetIsWindowsEngineEnabled().ConfigureAwait(false);

        return isWindowsEngine
            ? Wait.ForWindowsContainer()
            : Wait.ForUnixContainer();
#else
        return await Task.Run(Wait.ForUnixContainer);
#endif
    }
}
