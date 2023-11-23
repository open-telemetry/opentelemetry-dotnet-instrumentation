// <copyright file="MongoDBCollection.cs" company="OpenTelemetry Authors">
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
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using IntegrationTests.Helpers;
using static IntegrationTests.Helpers.DockerFileHelper;

namespace IntegrationTests;

[CollectionDefinition(Name)]
public class MongoDBCollection : ICollectionFixture<MongoDBFixture>
{
    public const string Name = nameof(MongoDBCollection);
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
        _container = await LaunchMongoContainerAsync(Port);
    }

    public async Task DisposeAsync()
    {
        if (_container != null)
        {
            await ShutdownMongoContainerAsync(_container);
        }
    }

    private async Task<IContainer> LaunchMongoContainerAsync(int port)
    {
        var waitForOs = await GetWaitForOSTypeAsync();
        var mongoContainersBuilder = new ContainerBuilder()
            .WithImage(MongoDBImage)
            .WithName($"mongo-db-{port}")
            .WithPortBinding(port, MongoDBPort)
            .WithWaitStrategy(waitForOs.UntilPortIsAvailable(MongoDBPort));

        var container = mongoContainersBuilder.Build();
        await container.StartAsync();

        return container;
    }

    private async Task ShutdownMongoContainerAsync(IContainer container)
    {
        await container.DisposeAsync();
    }

    private async Task<IWaitForContainerOS> GetWaitForOSTypeAsync()
    {
#if _WINDOWS
        var isWindowsEngine = await DockerSystemHelper.GetIsWindowsEngineEnabled();

        return isWindowsEngine
            ? Wait.ForWindowsContainer()
            : Wait.ForUnixContainer();
#else
        return await Task.Run(Wait.ForUnixContainer);
#endif
    }
}
