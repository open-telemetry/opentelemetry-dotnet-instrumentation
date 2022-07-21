// <copyright file="MySqlDataCollection.cs" company="OpenTelemetry Authors">
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
public class MySqlDataCollection : ICollectionFixture<MySqlDataFixture>
{
    public const string Name = nameof(MySqlDataCollection);
}

public class MySqlDataFixture : IAsyncLifetime
{
    private const int MySqlPort = 3306;
    private const string MySqlImage = "mysql:8.0.29";

    private TestcontainersContainer _container;

    public MySqlDataFixture()
    {
        Port = TcpPortProvider.GetOpenPort();
    }

    public int Port { get; }

    public async Task InitializeAsync()
    {
        _container = await LaunchMySqlContainerAsync(Port);
    }

    public async Task DisposeAsync()
    {
        if (_container != null)
        {
            await ShutdownMySqlContainerAsync(_container);
        }
    }

    private async Task<TestcontainersContainer> LaunchMySqlContainerAsync(int port)
    {
        var containersBuilder = new TestcontainersBuilder<TestcontainersContainer>()
            .WithImage(MySqlImage)
            .WithName($"mysql-{port}")
            .WithPortBinding(port, MySqlPort)
            .WithEnvironment("MYSQL_ALLOW_EMPTY_PASSWORD", "true")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(MySqlPort));

        var container = containersBuilder.Build();
        await container.StartAsync();

        return container;
    }

    private async Task ShutdownMySqlContainerAsync(TestcontainersContainer container)
    {
        await container.CleanUpAsync();
        await container.DisposeAsync();
    }
}
#endif
