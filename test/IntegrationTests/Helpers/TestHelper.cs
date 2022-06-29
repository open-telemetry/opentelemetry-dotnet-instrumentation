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
        : this(new EnvironmentHelper(testApplicationName, typeof(TestHelper), output), output)
    {
    }

    protected TestHelper(EnvironmentHelper environmentHelper, ITestOutputHelper output)
    {
        EnvironmentHelper = environmentHelper;
        Output = output;

        Output.WriteLine($"Platform: {EnvironmentTools.GetPlatform()}");
        Output.WriteLine($"Configuration: {EnvironmentTools.GetBuildConfiguration()}");
        Output.WriteLine($"TargetFramework: {EnvironmentHelper.GetTargetFramework()}");
        Output.WriteLine($".NET Core: {EnvironmentHelper.IsCoreClr()}");
        Output.WriteLine($"Profiler DLL: {EnvironmentHelper.GetProfilerPath()}");
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
            builder = builder.WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", agentBaseUrl);
            builder = builder.WithEnvironment("OTEL_AUTO_METRIC_EXPORT_INTERVAL", "1000");
            builder = builder.WithEnvironment("OTEL_DOTNET_AUTO_METRICS_ENABLED_INSTRUMENTATIONS", "AspNet");
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

    public Process StartTestApplication(int traceAgentPort, string arguments, string packageVersion, int aspNetCorePort, string framework = "", bool enableStartupHook = true)
    {
        var testSettings = new TestSettings
        {
            TracesSettings = new TracesSettings { Port = traceAgentPort },
            Arguments = arguments,
            PackageVersion = packageVersion,
            AspNetCorePort = aspNetCorePort,
            Framework = framework,
            EnableStartupHook = enableStartupHook
        };
        return StartTestApplication(testSettings);
    }

    public ProcessResult RunTestApplicationAndWaitForExit(int traceAgentPort, string arguments = null, string packageVersion = "", string framework = "", int aspNetCorePort = 5000, bool enableStartupHook = true)
    {
        var testSettings = new TestSettings
        {
            TracesSettings = new TracesSettings { Port = traceAgentPort },
            Arguments = arguments,
            PackageVersion = packageVersion,
            AspNetCorePort = aspNetCorePort,
            Framework = framework,
            EnableStartupHook = enableStartupHook
        };
        return RunTestApplicationAndWaitForExit(testSettings);
    }

    public ProcessResult RunTestApplicationAndWaitForExit(TestSettings testSettings)
    {
        var process = StartTestApplication(testSettings);
        var name = process.ProcessName;

        using var helper = new ProcessHelper(process);

        bool processTimeout = !process.WaitForExit((int)DefaultProcessTimeout.TotalMilliseconds);
        if (processTimeout)
        {
            process.Kill();
        }

        var exitCode = process.ExitCode;

        Output.WriteLine($"ProcessName: " + name);
        Output.WriteLine($"ProcessId: " + process.Id);
        Output.WriteLine($"Exit Code: " + exitCode);
        Output.WriteResult(helper);

        if (processTimeout)
        {
            throw new TimeoutException($"{name} ({process.Id}) did not exit within {DefaultProcessTimeout.TotalSeconds} sec");
        }

        return new ProcessResult(process, helper.StandardOutput, helper.ErrorOutput, exitCode);
    }

    protected void EnableDebugMode()
    {
        EnvironmentHelper.DebugModeEnabled = true;
    }

    protected void SetEnvironmentVariable(string key, string value)
    {
        EnvironmentHelper.CustomEnvironmentVariables.Add(key, value);
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
