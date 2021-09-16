using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using IntegrationTests.Helpers.Models;
using Xunit.Abstractions;

namespace IntegrationTests.Helpers
{
    public abstract class TestHelper
    {
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

            using var helper = new ProcessHelper(process);

            process.WaitForExit();
            helper.Drain();
            var exitCode = process.ExitCode;

            Output.WriteLine($"ProcessId: " + process.Id);
            Output.WriteLine($"Exit Code: " + exitCode);

            var standardOutput = helper.StandardOutput;

            if (!string.IsNullOrWhiteSpace(standardOutput))
            {
                Output.WriteLine($"StandardOutput:{Environment.NewLine}{standardOutput}");
            }

            var standardError = helper.ErrorOutput;

            if (!string.IsNullOrWhiteSpace(standardError))
            {
                Output.WriteLine($"StandardError:{Environment.NewLine}{standardError}");
            }

            return new ProcessResult(process, standardOutput, standardError, exitCode);
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
