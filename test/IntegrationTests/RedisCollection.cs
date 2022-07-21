// <copyright file="RedisCollection.cs" company="OpenTelemetry Authors">
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

#if NETCOREAPP3_1_OR_GREATER

using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using IntegrationTests.Helpers;
using Xunit;

namespace IntegrationTests;

[CollectionDefinition(Name)]
public class RedisCollection : ICollectionFixture<RedisFixture>
{
    public const string Name = nameof(RedisCollection);
}

public class RedisFixture : IAsyncLifetime
{
    private const int RedisPort = 6379;
    private const string RedisImage = "redis:7.0.4";

    private TestcontainersContainer _container;

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

    private async Task<TestcontainersContainer> LaunchRedisContainerAsync(int port)
    {
        var containersBuilder = new TestcontainersBuilder<TestcontainersContainer>()
            .WithImage(RedisImage)
            .WithName($"postgres-{port}")
            .WithPortBinding(port, RedisPort)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(RedisPort));

        var container = containersBuilder.Build();
        await container.StartAsync();

        return container;
    }

    private async Task ShutdownRedisContainerAsync(TestcontainersContainer container)
    {
        await container.CleanUpAsync();
        await container.DisposeAsync();
    }
}
#endif
