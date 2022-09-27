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
using System.Reflection;
using FluentAssertions;
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

    /// <summary>
    /// Gets the path for the test assembly, not the shadow copy created by xunit.
    /// </summary>
    public string GetTestAssemblyPath()
    {
#if NETFRAMEWORK
        // CodeBase is deprecated outside .NET Framework, instead of suppressing the error
        // build the code as per recommendation for each runtime.
        var codeBaseUrl = new Uri(Assembly.GetExecutingAssembly().CodeBase);
        var codeBasePath = Uri.UnescapeDataString(codeBaseUrl.AbsolutePath);
        var directory = Path.GetDirectoryName(codeBasePath);
        return Path.GetFullPath(directory);
#else
        return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
#endif
    }

    /// <summary>
    /// StartTestApplication starts the test application
    /// and returns the Process instance for further interaction.
    /// </summary>
    /// <returns>Test application process</returns>
    public Process StartTestApplication(int traceAgentPort = 0, int metricsAgentPort = 0, int logsAgentPort = 0, string arguments = null, string packageVersion = "", int aspNetCorePort = 0, string framework = "", bool enableStartupHook = true, bool enableClrProfiler = true)
    {
        var testSettings = new TestSettings
        {
            Arguments = arguments,
            PackageVersion = packageVersion,
            AspNetCorePort = aspNetCorePort,
            Framework = framework,
            EnableStartupHook = enableStartupHook,
            EnableClrProfiler = enableClrProfiler
        };

        if (traceAgentPort != 0)
        {
            testSettings.TracesSettings = new() { Port = traceAgentPort };
        }

        if (metricsAgentPort != 0)
        {
            testSettings.MetricsSettings = new() { Port = metricsAgentPort };
        }

        if (logsAgentPort != 0)
        {
            testSettings.LogSettings = new() { Port = logsAgentPort };
        }

        return StartTestApplication(testSettings);
    }

    /// <summary>
    /// RunTestApplication starts the test application, wait up to DefaultProcessTimeout.
    /// Assertion exceptions are thrown if it timed out or the exit code is non-zero.
    /// </summary>
    public void RunTestApplication(int traceAgentPort = 0, int metricsAgentPort = 0, string arguments = null, string packageVersion = "", string framework = "", int aspNetCorePort = 5000, bool enableStartupHook = true, bool enableClrProfiler = true)
    {
        var testSettings = new TestSettings
        {
            Arguments = arguments,
            PackageVersion = packageVersion,
            AspNetCorePort = aspNetCorePort,
            Framework = framework,
            EnableStartupHook = enableStartupHook,
            EnableClrProfiler = enableClrProfiler
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

    protected bool IsCoreClr()
    {
        return EnvironmentHelper.IsCoreClr();
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

        Output.WriteLine("ProcessId: " + process.Id);
        Output.WriteLine("Exit Code: " + process.ExitCode);
        Output.WriteResult(helper);

        processTimeout.Should().BeFalse("Test application timed out");
        process.ExitCode.Should().Be(0, "Test application exited with non-zero exit code");
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
