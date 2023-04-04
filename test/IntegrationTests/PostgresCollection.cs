// <copyright file="PostgresCollection.cs" company="OpenTelemetry Authors">
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
public class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = nameof(PostgresCollection);
}

public class PostgresFixture : IAsyncLifetime
{
    private const int PostgresPort = 5432;
    private static readonly string PostgresImage = ReadImageFrom("postgres.Dockerfile");

    private IContainer? _container;

    public PostgresFixture()
    {
        Port = TcpPortProvider.GetOpenPort();
    }

    public int Port { get; }

    public async Task InitializeAsync()
    {
        _container = await LaunchPostgresContainerAsync(Port);
    }

    public async Task DisposeAsync()
    {
        if (_container != null)
        {
            await ShutdownPostgresContainerAsync(_container);
        }
    }

    private async Task<IContainer> LaunchPostgresContainerAsync(int port)
    {
        var containersBuilder = new ContainerBuilder()
            .WithImage(PostgresImage)
            .WithName($"postgres-{port}")
            .WithPortBinding(port, PostgresPort)
            .WithEnvironment("POSTGRES_HOST_AUTH_METHOD", "trust")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(PostgresPort));

        var container = containersBuilder.Build();
        await container.StartAsync();

        return container;
    }

    private async Task ShutdownPostgresContainerAsync(IContainer container)
    {
        await container.DisposeAsync();
    }
}
