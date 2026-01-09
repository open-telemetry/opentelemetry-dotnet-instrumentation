// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using IntegrationTests.Helpers;
using static IntegrationTests.Helpers.DockerFileHelper;

namespace IntegrationTests;

[CollectionDefinition(Name)]
public class AzureCollection : ICollectionFixture<AzureFixture>
{
    public const string Name = nameof(AzureCollection);
}

public class AzureFixture : IAsyncLifetime
{
    private const int BlobServicePort = 10000;
    private static readonly string AzureStorageImage = ReadImageFrom("azure.Dockerfile");

    private IContainer? _container;

    public AzureFixture()
    {
        Port = TcpPortProvider.GetOpenPort();
    }

    public int Port { get; }

    public async Task InitializeAsync()
    {
        _container = await LaunchAzureContainerAsync(Port).ConfigureAwait(false);
    }

    public async Task DisposeAsync()
    {
        if (_container != null)
        {
            await ShutdownAzureContainerAsync(_container).ConfigureAwait(false);
        }
    }

    private static async Task<IContainer> LaunchAzureContainerAsync(int port)
    {
        var containersBuilder = new ContainerBuilder()
            .WithImage(AzureStorageImage)
            .WithName($"azure-storage-{port}")
            .WithPortBinding(port, BlobServicePort)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(BlobServicePort));

        var container = containersBuilder.Build();
        await container.StartAsync().ConfigureAwait(false);

        return container;
    }

    private static async Task ShutdownAzureContainerAsync(IContainer container)
    {
        await container.DisposeAsync().ConfigureAwait(false);
    }
}
