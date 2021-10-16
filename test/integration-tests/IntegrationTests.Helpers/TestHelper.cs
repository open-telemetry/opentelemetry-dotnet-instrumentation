using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using DotNet.Testcontainers.Containers.Builders;
using DotNet.Testcontainers.Containers.Modules;
using DotNet.Testcontainers.Containers.OutputConsumers;
using DotNet.Testcontainers.Containers.WaitStrategies;
using IntegrationTests.Helpers.Models;
using Xunit.Abstractions;

namespace IntegrationTests.Helpers
{
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

            string gatewayEndpoint = $"http://{DockerNetworkHelper.IntegrationTestsGateway}:{traceAgentPort}";
            string healthCheckEndpoint = $"{gatewayEndpoint}/health-check";
            string zipkinEndpoint = $"{gatewayEndpoint}/api/v2/spans";
            string networkName = DockerNetworkHelper.IntegrationTestsNetworkName;
            string networkId = DockerNetworkHelper.SetupIntegrationTestsNetwork();

            // Do gateway test
            PowershellHelper.RunCommand($"Invoke-WebRequest -Uri {healthCheckEndpoint} -UseBasicParsing | Select-Object Content", Output);

            Output.WriteLine($"Zipkin Endpoint: {zipkinEndpoint}");

            var builder = new TestcontainersBuilder<TestcontainersContainer>()
                  .WithImage(sampleName)
                  .WithCleanUp(cleanUp: true)
                  .WithOutputConsumer(Consume.RedirectStdoutAndStderrToConsole())
                  .WithName($"{sampleName}-{traceAgentPort}-{webPort}")
                  .WithNetwork(networkId, networkName)
                  .WithPortBinding(webPort, 80)
                  .WithEnvironment("OTEL_EXPORTER_ZIPKIN_ENDPOINT", zipkinEndpoint)
                  .WithWaitStrategy(waitOS.UntilPortIsAvailable(80));

            var container = builder.Build();
            var wasStarted = container.StartAsync().Wait(TimeSpan.FromMinutes(5));

            Output.WriteLine($"Container was started successfully: {wasStarted}");

            if (wasStarted)
            {
                PowershellHelper.RunCommand($"docker exec {container.Name} curl -v {healthCheckEndpoint}", Output);
            }

            // Get network info
            PowershellHelper.RunCommand($"docker network inspect {DockerNetworkHelper.IntegrationTestsNetworkName}", Output);

            return new Container(container);
        }

        public Process StartSample(int traceAgentPort, string arguments, string packageVersion, int aspNetCorePort, int? statsdPort = null, string framework = "")
        {
            // get path to sample app that the profiler will attach to
            string sampleAppPath = EnvironmentHelper.GetSampleApplicationPath(packageVersion, framework);
            if (!File.Exists(sampleAppPath))
            {
                throw new Exception($"application not found: {sampleAppPath}");
            }

            // get full paths to integration definitions
            IEnumerable<string> integrationPaths = Directory.EnumerateFiles(".", "*integrations.json").Select(Path.GetFullPath);

            Output.WriteLine($"Starting Application: {sampleAppPath}");
            var executable = EnvironmentHelper.IsCoreClr() ? EnvironmentHelper.GetSampleExecutionSource() : sampleAppPath;
            var args = EnvironmentHelper.IsCoreClr() ? $"{sampleAppPath} {arguments ?? string.Empty}" : arguments;

            return ProfilerHelper.StartProcessWithProfiler(
                executable,
                EnvironmentHelper,
                args,
                traceAgentPort: traceAgentPort,
                statsdPort: statsdPort,
                aspNetCorePort: aspNetCorePort,
                processToProfile: executable);
        }

        public ProcessResult RunSampleAndWaitForExit(int traceAgentPort, int? statsdPort = null, string arguments = null, string packageVersion = "", string framework = "", int aspNetCorePort = 5000)
        {
            var process = StartSample(traceAgentPort, arguments, packageVersion, aspNetCorePort: aspNetCorePort, statsdPort: statsdPort, framework: framework);
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

        protected void SetServiceVersion(string serviceVersion)
        {
            SetEnvironmentVariable("OTEL_VERSION", serviceVersion);
        }

        protected void SetCallTargetSettings(bool enableCallTarget)
        {
            SetEnvironmentVariable("OTEL_TRACE_CALLTARGET_ENABLED", enableCallTarget ? "true" : "false");
        }
    }
}
