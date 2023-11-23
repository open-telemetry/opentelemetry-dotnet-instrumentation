// <copyright file="AzureCollection.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

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
        _container = await LaunchAzureContainerAsync(Port);
    }

    public async Task DisposeAsync()
    {
        if (_container != null)
        {
            await ShutdownAzureContainerAsync(_container);
        }
    }

    private static async Task<IContainer> LaunchAzureContainerAsync(int port)
    {
        var containersBuilder = new ContainerBuilder()
            .WithImage(AzureStorageImage)
            .WithName($"azure-storage-{port}")
            .WithPortBinding(port, BlobServicePort)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(BlobServicePort));

        var container = containersBuilder.Build();
        await container.StartAsync();

        return container;
    }

    private static async Task ShutdownAzureContainerAsync(IContainer container)
    {
        await container.DisposeAsync();
    }
}
