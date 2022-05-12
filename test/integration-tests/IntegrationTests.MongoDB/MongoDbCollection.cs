// <copyright file="MongoDbCollection.cs" company="OpenTelemetry Authors">
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

using System.Threading.Tasks;
using DotNet.Testcontainers.Containers.Builders;
using DotNet.Testcontainers.Containers.Modules;
using DotNet.Testcontainers.Containers.WaitStrategies;
using IntegrationTests.Helpers;
using Xunit;

namespace IntegrationTests.MongoDB;

[CollectionDefinition(Name)]
public class MongoDbCollection : ICollectionFixture<MongoDbFixture>
{
    public const string Name = nameof(MongoDbCollection);
}

public class MongoDbFixture : IAsyncLifetime
{
    private const int MongoDbPort = 27017;
    private const string MongoDbImage = "mongo:5.0.6";

    private TestcontainersContainer _container;

    public MongoDbFixture()
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

    private async Task<TestcontainersContainer> LaunchMongoContainerAsync(int port)
    {
        var waitOS = EnvironmentTools.IsWindows()
            ? Wait.ForWindowsContainer()
            : Wait.ForUnixContainer();

        var mongoContainersBuilder = new TestcontainersBuilder<TestcontainersContainer>()
            .WithImage(MongoDbImage)
            .WithName($"mongo-db-{port}")
            .WithPortBinding(port, MongoDbPort)
            .WithWaitStrategy(waitOS.UntilPortIsAvailable(MongoDbPort));

        var container = mongoContainersBuilder.Build();
        await container.StartAsync();

        return container;
    }

    private async Task ShutdownMongoContainerAsync(TestcontainersContainer container)
    {
        await container.CleanUpAsync();
        await container.DisposeAsync();
    }
}
