// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.InteropServices;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using IntegrationTests.Helpers;
using static IntegrationTests.Helpers.DockerFileHelper;

namespace IntegrationTests;

[CollectionDefinition(Name)]
public class XmsCollectionFixture : ICollectionFixture<XmsFixture>
{
    public const string Name = nameof(XmsCollectionFixture);
}

public class XmsFixture : IAsyncLifetime
{
    private const int MqPort = 1414;
    private static readonly string IbmMqImage = ReadImageFrom("ibm-mq.Dockerfile");

    private IContainer? _container;

    public XmsFixture()
    {
        Port = TcpPortProvider.GetOpenPort();
    }

    public int Port { get; }

    // Not every platform the CI matrix runs on necessarily has a matching image manifest.
    // Rather than pre-guessing via an architecture allow-list, we try to start the real
    // container and only skip the dependent tests if that concrete attempt fails.
    public Exception? StartupFailure { get; private set; }

    public async Task InitializeAsync()
    {
        var container = BuildContainer(Port);

        try
        {
            await container.StartAsync().ConfigureAwait(false);
            _container = container;
        }
#pragma warning disable CA1031 // Do not catch general exception types. Any exception means the operation failed.
        catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types. Any exception means the operation failed.
        {
            // Container startup is this fixture's entire job, so any failure here (missing
            // platform manifest, daemon hiccup, etc.) means "can't run this test on this
            // runner" and should surface as a named skip, not a hard fixture failure.
            await container.DisposeAsync().ConfigureAwait(false);
            StartupFailure = ex;
        }
    }

    public async Task DisposeAsync()
    {
        if (_container != null)
        {
            await _container.DisposeAsync().ConfigureAwait(false);
        }
    }

    public void SkipIfUnsupportedPlatform()
    {
        if (StartupFailure is not null)
        {
            throw new SkipException(
                $"IBM MQ container failed to start on {RuntimeInformation.OSArchitecture}/{RuntimeInformation.OSDescription}: {StartupFailure.Message}");
        }
    }

    private static IContainer BuildContainer(int port)
    {
        // Queues DEV.QUEUE.1-3, channel DEV.APP.SVRCONN and user "app" are pre-provisioned by the
        // developer default configuration whenever LICENSE=accept is supplied and no custom MQSC is mounted.
        return new ContainerBuilder(IbmMqImage)
            .WithName($"ibm-mq-{port}")
            .WithPortBinding(port, MqPort)
            .WithEnvironment("LICENSE", "accept")
            .WithEnvironment("MQ_QMGR_NAME", "QM1")
            .WithEnvironment("MQ_APP_PASSWORD", "passw0rd")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(MqPort))
            .Build();
    }
}
