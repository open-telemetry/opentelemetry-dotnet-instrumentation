using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using DotNet.Testcontainers.Containers.Builders;
using DotNet.Testcontainers.Containers.Modules;
using DotNet.Testcontainers.Containers.OutputConsumers;
using DotNet.Testcontainers.Containers.WaitStrategies;
using FluentAssertions;
using IntegrationTests.Helpers.Models;
using Xunit.Abstractions;

namespace IntegrationTests.Helpers;

public abstract class TestHelper
{
    // Warning: Long timeouts can cause integer overflow!
    private static readonly TimeSpan DefaultProcessTimeout = TimeSpan.FromMinutes(5);

    protected TestHelper(string sampleAppName, ITestOutputHelper output)
        : this(new EnvironmentHelper(sampleAppName, typeof(TestHelper), output), output)
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

    public Container StartContainer(int traceAgentPort, int webPort)
    {
        // get path to sample app that the profiler will attach to
        string sampleName = $"samples-{EnvironmentHelper.SampleName.ToLowerInvariant()}";

        var waitOS = EnvironmentTools.IsWindows()
            ? Wait.ForWindowsContainer()
            : Wait.ForUnixContainer();

        string agentBaseUrl = $"http://{DockerNetworkHelper.IntegrationTestsGateway}:{traceAgentPort}";
        string healthCheckEndpoint = $"{agentBaseUrl}/health-check";
        string zipkinEndpoint = $"{agentBaseUrl}/api/v2/spans";
        string networkName = DockerNetworkHelper.IntegrationTestsNetworkName;
        string networkId = DockerNetworkHelper.SetupIntegrationTestsNetwork();

        Output.WriteLine($"Zipkin Endpoint: {zipkinEndpoint}");

        string logPath = EnvironmentHelper.IsRunningOnCI()
            ? Path.Combine(Environment.GetEnvironmentVariable("GITHUB_WORKSPACE"), "build_data", "profiler-logs")
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"OpenTelemetry .NET AutoInstrumentation", "logs");

        Directory.CreateDirectory(logPath);

        Output.WriteLine("Collecting docker logs to: " + logPath);

        var builder = new TestcontainersBuilder<TestcontainersContainer>()
            .WithImage(sampleName)
            .WithCleanUp(cleanUp: true)
            .WithOutputConsumer(Consume.RedirectStdoutAndStderrToConsole())
            .WithName($"{sampleName}-{traceAgentPort}-{webPort}")
            .WithNetwork(networkId, networkName)
            .WithPortBinding(webPort, 80)
            .WithEnvironment("OTEL_EXPORTER_ZIPKIN_ENDPOINT", zipkinEndpoint)
            .WithMount(logPath, "c:/inetpub/wwwroot/logs")
            .WithMount(EnvironmentHelper.GetNukeBuildOutput(), "c:/opentelemetry");

        var container = builder.Build();
        var wasStarted = container.StartAsync().Wait(TimeSpan.FromMinutes(5));

        wasStarted.Should().BeTrue($"Container based on {sampleName} has to be operational for the test.");

        Output.WriteLine($"Container was started successfully.");

        PowershellHelper.RunCommand($"docker exec {container.Name} curl -v {healthCheckEndpoint}", Output);

        var healthChecks = 0;
        var healthChecksUrl = $"http://localhost:{webPort}/health-check";
        var healthChecksPassed = false;

        do
        {
            var (standardOutput, _) = PowershellHelper.RunCommand($"Write-Host (Invoke-WebRequest -Uri {healthChecksUrl}).StatusCode", Output);

            healthChecksPassed = standardOutput.Trim() == "200";

            if (!healthChecksPassed)
            {
                healthChecks++;

                // wait for next health check
                Thread.Sleep(2000 * healthChecks);
            }
        }
        while (healthChecks < 5 && !healthChecksPassed);

        healthChecksPassed.Should().BeTrue($"Health checks to the url '{healthChecksUrl}' failed. Application didn't respond with given time.");

        return new Container(container);
    }

    public Process StartSample(int traceAgentPort, string arguments, string packageVersion, int aspNetCorePort, string framework = "", bool enableStartupHook = true)
    {
        // get path to sample app that the profiler will attach to
        string sampleAppPath = EnvironmentHelper.GetSampleApplicationPath(packageVersion, framework);
        if (!File.Exists(sampleAppPath))
        {
            throw new Exception($"application not found: {sampleAppPath}");
        }

        Output.WriteLine($"Starting Application: {sampleAppPath}");
        var executable = EnvironmentHelper.IsCoreClr() ? EnvironmentHelper.GetSampleExecutionSource() : sampleAppPath;
        var args = EnvironmentHelper.IsCoreClr() ? $"{sampleAppPath} {arguments ?? string.Empty}" : arguments;

        return InstrumentedProcessHelper.StartInstrumentedProcess(
            executable,
            EnvironmentHelper,
            args,
            traceAgentPort: traceAgentPort,
            aspNetCorePort: aspNetCorePort,
            processToProfile: executable,
            enableStartupHook: enableStartupHook);
    }

    public ProcessResult RunSampleAndWaitForExit(int traceAgentPort, string arguments = null, string packageVersion = "", string framework = "", int aspNetCorePort = 5000, bool enableStartupHook = true)
    {
        var process = StartSample(traceAgentPort, arguments, packageVersion, aspNetCorePort: aspNetCorePort, framework: framework, enableStartupHook: enableStartupHook);
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
}
