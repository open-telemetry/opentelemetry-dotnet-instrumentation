// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NETFRAMEWORK
using System.Text;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using IntegrationTests.Helpers;
using Xunit.Abstractions;

namespace IntegrationTests;

internal static class IISContainerTestHelper
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

        var builder = new ContainerBuilder(imageName)
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

    public static async Task RunContainerAsync(
        string imageName,
        IReadOnlyDictionary<string, string> environmentVariables,
        ITestOutputHelper testOutputHelper)
    {
        var networkName = await DockerNetworkHelper.SetupIntegrationTestsNetworkAsync().ConfigureAwait(false);
        using var stdout = new MemoryStream();
        using var stderr = new MemoryStream();

        var builder = new ContainerBuilder(imageName)
            .WithCleanUp(cleanUp: true)
            .WithName($"{imageName}-{Guid.NewGuid():N}")
            .WithNetwork(networkName)
            .WithOutputConsumer(Consume.RedirectStdoutAndStderrToStream(stdout, stderr));

        foreach (var env in environmentVariables)
        {
            builder = builder.WithEnvironment(env.Key, env.Value);
        }

        var container = builder.Build();
        using var cts = new CancellationTokenSource(TestTimeout.ProcessExit);

        try
        {
            await container.StartAsync(cts.Token).ConfigureAwait(false);
            testOutputHelper.WriteLine("Container was started successfully.");
            var exitCode = await container.GetExitCodeAsync(cts.Token).ConfigureAwait(false);

            WriteContainerOutput(testOutputHelper, stdout, stderr);
            Assert.Equal(0L, exitCode);
        }
        catch (OperationCanceledException)
        {
            WriteContainerOutput(testOutputHelper, stdout, stderr);
            throw new TimeoutException($"Container based on {imageName} did not exit within {TestTimeout.ProcessExit}.");
        }
        finally
        {
            await container.DisposeAsync().ConfigureAwait(false);
        }
    }

    private static void WriteContainerOutput(ITestOutputHelper testOutputHelper, MemoryStream stdout, MemoryStream stderr)
    {
        var standardOutput = ReadStream(stdout);
        var errorOutput = ReadStream(stderr);

        if (!string.IsNullOrWhiteSpace(standardOutput))
        {
            testOutputHelper.WriteLine("Container stdout:");
            testOutputHelper.WriteLine(standardOutput);
        }

        if (!string.IsNullOrWhiteSpace(errorOutput))
        {
            testOutputHelper.WriteLine("Container stderr:");
            testOutputHelper.WriteLine(errorOutput);
        }
    }

    private static string ReadStream(MemoryStream stream)
    {
        stream.Position = 0;
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        return reader.ReadToEnd();
    }
}

#endif
