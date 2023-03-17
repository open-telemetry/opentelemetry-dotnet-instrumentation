// <copyright file="SqlServerCollection.cs" company="OpenTelemetry Authors">
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
using IntegrationTests.Helpers.TestContainers;
using Microsoft.Data.SqlClient;
using static IntegrationTests.Helpers.DockerFileHelper;

namespace IntegrationTests;

[CollectionDefinition(Name)]
public class SqlServerCollection : ICollectionFixture<SqlServerFixture>
{
    public const string Name = nameof(SqlServerCollection);
}

public class SqlServerFixture : IAsyncLifetime
{
    private const int DatabasePort = 1433;
    private static readonly string DatabaseImage = ReadImageFrom("sql-server.Dockerfile");

    private IContainer? _container;

    public SqlServerFixture()
    {
        Port = TcpPortProvider.GetOpenPort();
    }

    public string Password { get; } = $"@{Guid.NewGuid().ToString("N")}";

    public int Port { get; }

    public async Task InitializeAsync()
    {
        _container = await LaunchSqlServerContainerAsync();
    }

    public async Task DisposeAsync()
    {
        if (_container != null)
        {
            await ShutdownSqlServerContainerAsync(_container);
        }
    }

    private static async Task ShutdownSqlServerContainerAsync(IContainer container)
    {
        await container.DisposeAsync();
    }

    private async Task<IContainer> LaunchSqlServerContainerAsync()
    {
        var databaseContainersBuilder = new ContainerBuilder()
            .WithImage(DatabaseImage)
            .WithName($"sql-server-{Port}")
            .WithPortBinding(Port, DatabasePort)
            .WithEnvironment("SA_PASSWORD", Password)
            .WithEnvironment("ACCEPT_EULA", "Y")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(DatabasePort))
            .WithWaitStrategy(Wait.ForUnixContainer().AddCustomWaitStrategy(new UntilAsyncOperationIsSucceeded(DatabaseLoginOperation, 15)));

        var container = databaseContainersBuilder.Build();
        await container.StartAsync();

        return container;
    }

    private async Task<bool> DatabaseLoginOperation()
    {
        try
        {
            string connectionString = $"Server=127.0.0.1,{Port};User=sa;Password={Password};TrustServerCertificate=True;";
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                return true;
            }
        }
        catch
        {
            // Slow down next connection attempt
            await Task.Delay(TimeSpan.FromSeconds(2));

            return false;
        }
    }
}
