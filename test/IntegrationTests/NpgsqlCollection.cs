// <copyright file="NpgsqlCollection.cs" company="OpenTelemetry Authors">
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
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using IntegrationTests.Helpers;
using Xunit;

namespace IntegrationTests;

[CollectionDefinition(Name)]
public class NpgsqlCollection : ICollectionFixture<NpgsqlFixture>
{
    public const string Name = nameof(NpgsqlCollection);
}

public class NpgsqlFixture : IAsyncLifetime
{
    private const int NpgsqlPort = 5432;
    private const string NpgsqlImage = "postgres:14.4";

    private TestcontainersContainer _container;

    public NpgsqlFixture()
    {
        Port = TcpPortProvider.GetOpenPort();
    }

    public int Port { get; }

    public async Task InitializeAsync()
    {
        _container = await LaunchNpgsqlContainerAsync(Port);
    }

    public async Task DisposeAsync()
    {
        if (_container != null)
        {
            await ShutdownNpgsqlContainerAsync(_container);
        }
    }

    private async Task<TestcontainersContainer> LaunchNpgsqlContainerAsync(int port)
    {
        var containersBuilder = new TestcontainersBuilder<TestcontainersContainer>()
            .WithImage(NpgsqlImage)
            .WithName($"postgres-{port}")
            .WithPortBinding(port, NpgsqlPort)
            .WithEnvironment("POSTGRES_HOST_AUTH_METHOD", "trust")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(NpgsqlPort));

        var container = containersBuilder.Build();
        await container.StartAsync();

        return container;
    }

    private async Task ShutdownNpgsqlContainerAsync(TestcontainersContainer container)
    {
        await container.CleanUpAsync();
        await container.DisposeAsync();
    }
}
