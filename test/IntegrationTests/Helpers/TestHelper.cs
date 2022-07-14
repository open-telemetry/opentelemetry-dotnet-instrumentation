// <copyright file="TestHelper.cs" company="OpenTelemetry Authors">
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

using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using FluentAssertions;
using IntegrationTests.Helpers.Models;
using Xunit.Abstractions;

namespace IntegrationTests.Helpers;

public abstract class TestHelper
{
    // Warning: Long timeouts can cause integer overflow!
    private static readonly TimeSpan DefaultProcessTimeout = TimeSpan.FromMinutes(5);

    protected TestHelper(string testApplicationName, ITestOutputHelper output)
    {
        Output = output;
        EnvironmentHelper = new EnvironmentHelper(testApplicationName, typeof(TestHelper), output);

        output.WriteLine($"Platform: {EnvironmentTools.GetPlatform()}");
        output.WriteLine($"Configuration: {EnvironmentTools.GetBuildConfiguration()}");
        output.WriteLine($"TargetFramework: {EnvironmentHelper.GetTargetFramework()}");
        output.WriteLine($".NET Core: {EnvironmentHelper.IsCoreClr()}");
        output.WriteLine($"Profiler DLL: {EnvironmentHelper.GetProfilerPath()}");
    }

    protected EnvironmentHelper EnvironmentHelper { get; }

    protected ITestOutputHelper Output { get; }

    public async Task<Container> StartContainerAsync(TestSettings testSettings, int webPort)
    {
        // get path to test application that the profiler will attach to
        string testApplicationName = $"testapplication-{EnvironmentHelper.TestApplicationName.ToLowerInvariant()}";

        string networkName = DockerNetworkHelper.IntegrationTestsNetworkName;
        string networkId = await DockerNetworkHelper.SetupIntegrationTestsNetworkAsync();

        string logPath = EnvironmentHelper.IsRunningOnCI()
            ? Path.Combine(Environment.GetEnvironmentVariable("GITHUB_WORKSPACE"), "build_data", "profiler-logs")
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"OpenTelemetry .NET AutoInstrumentation", "logs");

        Directory.CreateDirectory(logPath);

        Output.WriteLine("Collecting docker logs to: " + logPath);

        var agentPort = testSettings.TracesSettings?.Port ?? testSettings.MetricsSettings?.Port;
        var builder = new TestcontainersBuilder<TestcontainersContainer>()
            .WithImage(testApplicationName)
            .WithCleanUp(cleanUp: true)
            .WithOutputConsumer(Consume.RedirectStdoutAndStderrToConsole())
            .WithName($"{testApplicationName}-{agentPort}-{webPort}")
            .WithNetwork(networkId, networkName)
            .WithPortBinding(webPort, 80)
            .WithBindMount(logPath, "c:/inetpub/wwwroot/logs")
            .WithBindMount(EnvironmentHelper.GetNukeBuildOutput(), "c:/opentelemetry");

        string agentBaseUrl = $"http://{DockerNetworkHelper.IntegrationTestsGateway}:{agentPort}";
        string agentHealthzUrl = $"{agentBaseUrl}/healthz";

        if (testSettings.TracesSettings != null)
        {
            string zipkinEndpoint = $"{agentBaseUrl}/api/v2/spans";
            Output.WriteLine($"Zipkin Endpoint: {zipkinEndpoint}");

            builder = builder.WithEnvironment("OTEL_EXPORTER_ZIPKIN_ENDPOINT", zipkinEndpoint);
        }

        if (testSettings.MetricsSettings != null)
        {
            Output.WriteLine($"Otlp Endpoint: {agentBaseUrl}");
            builder = builder.WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", agentBaseUrl);
            builder = builder.WithEnvironment("OTEL_METRIC_EXPORT_INTERVAL", "1000");
        }

        var container = builder.Build();
        var wasStarted = container.StartAsync().Wait(TimeSpan.FromMinutes(5));

        wasStarted.Should().BeTrue($"Container based on {testApplicationName} has to be operational for the test.");

        Output.WriteLine($"Container was started successfully.");

        PowershellHelper.RunCommand($"docker exec {container.Name} curl -v {agentHealthzUrl}", Output);

        var webAppHealthzUrl = $"http://localhost:{webPort}/healthz";
        var webAppHealthzResult = await HealthzHelper.TestHealtzAsync(webAppHealthzUrl, "IIS WebApp", Output);

        webAppHealthzResult.Should().BeTrue("IIS WebApp health check never returned OK.");

        Output.WriteLine($"IIS WebApp was started successfully.");

        return new Container(container);
    }

    /// <summary>
    /// StartTestApplication starts the test application
    // and returns the Process instance for further interaction.
    /// </summary>
    public Process StartTestApplication(int traceAgentPort = 0, int metricsAgentPort = 0, string arguments = null, string packageVersion = "", int aspNetCorePort = 0, string framework = "", bool enableStartupHook = true)
    {
        var testSettings = new TestSettings
        {
            Arguments = arguments,
            PackageVersion = packageVersion,
            AspNetCorePort = aspNetCorePort,
            Framework = framework,
            EnableStartupHook = enableStartupHook
        };

        if (traceAgentPort != 0)
        {
            testSettings.TracesSettings = new() { Port = traceAgentPort };
        }

        if (metricsAgentPort != 0)
        {
            testSettings.MetricsSettings = new() { Port = metricsAgentPort };
        }

        return StartTestApplication(testSettings);
    }

    /// <summary>
    /// RunTestApplication starts the test application, wait up to DefaultProcessTimeout.
    /// Assertion exceptions are thrown if it timed out or the exit code is non-zero.
    /// </summary>
    public void RunTestApplication(int traceAgentPort = 0, int metricsAgentPort = 0, string arguments = null, string packageVersion = "", string framework = "", int aspNetCorePort = 5000, bool enableStartupHook = true)
    {
        var testSettings = new TestSettings
        {
            Arguments = arguments,
            PackageVersion = packageVersion,
            AspNetCorePort = aspNetCorePort,
            Framework = framework,
            EnableStartupHook = enableStartupHook
        };

        if (traceAgentPort != 0)
        {
            testSettings.TracesSettings = new() { Port = traceAgentPort };
        }

        if (metricsAgentPort != 0)
        {
            testSettings.MetricsSettings = new() { Port = metricsAgentPort };
        }

        RunTestApplication(testSettings);
    }

    protected void EnableDebugMode()
    {
        EnvironmentHelper.DebugModeEnabled = true;
    }

    protected void SetEnvironmentVariable(string key, string value)
    {
        EnvironmentHelper.CustomEnvironmentVariables.Add(key, value);
    }

    private void RunTestApplication(TestSettings testSettings)
    {
        using var process = StartTestApplication(testSettings);
        Output.WriteLine($"ProcessName: " + process.ProcessName);
        using var helper = new ProcessHelper(process);

        bool processTimeout = !process.WaitForExit((int)DefaultProcessTimeout.TotalMilliseconds);
        if (processTimeout)
        {
            process.Kill();
        }

        Output.WriteLine($"ProcessId: " + process.Id);
        Output.WriteLine($"Exit Code: " + process.ExitCode);
        Output.WriteResult(helper);

        processTimeout.Should().BeFalse("Test application timed out");
        process.ExitCode.Should().Be(0, $"Test application exited with non-zero exit code");
    }

    private Process StartTestApplication(TestSettings testSettings)
    {
        // get path to test application that the profiler will attach to
        string testApplicationPath = EnvironmentHelper.GetTestApplicationPath(testSettings.PackageVersion, testSettings.Framework);
        if (!File.Exists(testApplicationPath))
        {
            throw new Exception($"application not found: {testApplicationPath}");
        }

        Output.WriteLine($"Starting Application: {testApplicationPath}");
        var executable = EnvironmentHelper.IsCoreClr() ? EnvironmentHelper.GetTestApplicationExecutionSource() : testApplicationPath;
        var args = EnvironmentHelper.IsCoreClr() ? $"{testApplicationPath} {testSettings.Arguments ?? string.Empty}" : testSettings.Arguments;

        return InstrumentedProcessHelper.StartInstrumentedProcess(
            executable,
            EnvironmentHelper,
            args,
            testSettings);
    }
}
