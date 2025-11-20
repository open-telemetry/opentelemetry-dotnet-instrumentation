// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using IntegrationTests.Helpers;
using static IntegrationTests.Helpers.DockerFileHelper;

namespace IntegrationTests;

[CollectionDefinition(Name)]
public class OracleCollection : ICollectionFixture<OracleFixture>
{
    public const string Name = nameof(OracleCollection);
}

public class OracleFixture : IAsyncLifetime
{
    private const int OraclePort = 1521;
    private static readonly string OracleImage = ReadImageFrom("oracle.Dockerfile");

    private IContainer? _container;

    public OracleFixture()
    {
        if (IsCurrentArchitectureSupported)
        {
            Port = TcpPortProvider.GetOpenPort();
        }
    }

    public bool IsCurrentArchitectureSupported { get; } = EnvironmentTools.IsX64();

    public int Port { get; }

    public string Password { get; } = $"@{Guid.NewGuid():N}";

    public async Task InitializeAsync()
    {
        if (!IsCurrentArchitectureSupported)
        {
            return;
        }

        _container = await LaunchOracleContainerAsync(Port);
    }

    public async Task DisposeAsync()
    {
        if (_container != null)
        {
            await ShutdownOracleContainerAsync(_container);
        }
    }

    public void SkipIfUnsupportedPlatform()
    {
        if (!IsCurrentArchitectureSupported)
        {
            throw new SkipException("Oracle is supported only on AMD64.");
        }
    }

    private async Task<IContainer> LaunchOracleContainerAsync(int port)
    {
        var containersBuilder = new ContainerBuilder()
            .WithImage(OracleImage)
            .WithEnvironment("ORACLE_RANDOM_PASSWORD", "yes")
            .WithName($"oracle-{port}")
            .WithPortBinding(port, OraclePort)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(OraclePort))
            .WithWaitStrategy(Wait.ForUnixContainer().UntilCommandIsCompleted("./healthcheck.sh"));

        var container = containersBuilder.Build();
        await container.StartAsync();

        // Create the application user after container is ready
        // First ensure the pluggable database is open, then create the user
        var createUserScript = $"ALTER PLUGGABLE DATABASE ALL OPEN; " +
                               $"ALTER SESSION SET CONTAINER=FREEPDB1; " +
                               $"CREATE USER appuser IDENTIFIED BY \"{Password}\" QUOTA UNLIMITED ON USERS; " +
                               $"GRANT CONNECT, RESOURCE TO appuser;";

        await container.ExecAsync(new[] { "bash", "-c", $"echo \"{createUserScript}\" | sqlplus -s / as sysdba" });

        return container;
    }

    private async Task ShutdownOracleContainerAsync(IContainer container)
    {
        await container.DisposeAsync();
    }
}
