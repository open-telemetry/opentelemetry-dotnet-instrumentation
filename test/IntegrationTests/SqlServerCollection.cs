// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

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
        if (IsCurrentArchitectureSupported)
        {
            Port = TcpPortProvider.GetOpenPort();
        }
    }

    public string Password { get; } = $"@{Guid.NewGuid().ToString("N")}";

    public int Port { get; }

    public bool IsCurrentArchitectureSupported { get; } = EnvironmentTools.IsX64();

    public async Task InitializeAsync()
    {
        if (!IsCurrentArchitectureSupported)
        {
            return;
        }

        _container = await LaunchSqlServerContainerAsync();
    }

    public async Task DisposeAsync()
    {
        if (_container != null)
        {
            await ShutdownSqlServerContainerAsync(_container);
        }
    }

    public void SkipIfUnsupportedPlatform()
    {
        if (!IsCurrentArchitectureSupported)
        {
            throw new SkipException("SQL Server is supported only on AMD64.");
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
