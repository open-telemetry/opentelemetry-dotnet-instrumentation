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

#if !NETFRAMEWORK
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using IntegrationTests.Helpers;
using Xunit;

namespace IntegrationTests.MongoDB;

[CollectionDefinition(Name)]
public class MongoDBCollection : ICollectionFixture<MongoDBFixture>
{
    public const string Name = nameof(MongoDBCollection);
}

public class MongoDBFixture : IAsyncLifetime
{
    private const int MongoDBPort = 27017;
    private const string MongoDBImage = "mongo:5.0.6";

    private TestcontainersContainer _container;

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

    private async Task<TestcontainersContainer> LaunchMongoContainerAsync(int port)
    {
        var mongoContainersBuilder = new TestcontainersBuilder<TestcontainersContainer>()
            .WithImage(MongoDBImage)
            .WithName($"mongo-db-{port}")
            .WithPortBinding(port, MongoDBPort)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(MongoDBPort));

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
#endif
