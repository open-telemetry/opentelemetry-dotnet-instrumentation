// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NETFRAMEWORK
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using IntegrationTests.Helpers;
using Xunit.Abstractions;

namespace IntegrationTests;

internal class IISContainerTestHelper
{
    public static async Task<IContainer> StartContainerAsync(
        string imageName,
        int webPort,
        Dictionary<string, string> environmentVariables,
        ITestOutputHelper testOutputHelper)
    {
        var networkName = await DockerNetworkHelper.SetupIntegrationTestsNetworkAsync().ConfigureAwait(false);

        var logPath = EnvironmentHelper.IsRunningOnCI()
            ? Path.Combine(Environment.GetEnvironmentVariable("GITHUB_WORKSPACE"), "test-artifacts", "profiler-logs")
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"OpenTelemetry .NET AutoInstrumentation", "logs");
        Directory.CreateDirectory(logPath);
        testOutputHelper.WriteLine("Collecting docker logs to: " + logPath);

        var builder = new ContainerBuilder()
            .WithImage(imageName)
            .WithCleanUp(cleanUp: true)
            .WithName($"{imageName}-{webPort}")
            .WithNetwork(networkName)
            .WithPortBinding(webPort, 80)
            .WithBindMount(logPath, "c:/inetpub/wwwroot/logs");

        foreach (var env in environmentVariables)
        {
            builder = builder.WithEnvironment(env.Key, env.Value);
        }

        var container = builder.Build();
        try
        {
            var wasStarted = container.StartAsync().Wait(TimeSpan.FromMinutes(5));
            Assert.True(wasStarted, $"Container based on {imageName} has to be operational for the test.");
            testOutputHelper.WriteLine("Container was started successfully.");

            await HealthzHelper.TestAsync($"http://localhost:{webPort}/healthz", testOutputHelper).ConfigureAwait(false);
            testOutputHelper.WriteLine("IIS WebApp was started successfully.");
        }
        catch
        {
            await container.DisposeAsync().ConfigureAwait(false);
            throw;
        }

        return container;
    }
}

#endif
