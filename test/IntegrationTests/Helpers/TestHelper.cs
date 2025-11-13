// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using System.Reflection;
using Xunit.Abstractions;

namespace IntegrationTests.Helpers;

public abstract class TestHelper
{
    protected TestHelper(string testApplicationName, ITestOutputHelper output, string testApplicationType = "integrations")
    {
        Output = output;
        EnvironmentHelper = new EnvironmentHelper(testApplicationName, typeof(TestHelper), output, testApplicationType: testApplicationType);

        output.WriteLine($"Platform: {EnvironmentTools.GetPlatform()}");
        output.WriteLine($"Configuration: {EnvironmentTools.GetBuildConfiguration()}");
        output.WriteLine($"TargetFramework: {EnvironmentHelper.GetTargetFramework()}");
        output.WriteLine($".NET Core: {EnvironmentHelper.IsCoreClr()}");
        if (testApplicationType == "integrations")
        {
            output.WriteLine($"Profiler DLL: {EnvironmentHelper.GetProfilerPath()}");
        }
    }

    protected EnvironmentHelper EnvironmentHelper { get; }

    protected ITestOutputHelper Output { get; }

    public string GetTestAssemblyPath()
    {
        // Gets the path for the test assembly, not the shadow copy created by xunit.
#if NETFRAMEWORK
        // CodeBase is deprecated outside .NET Framework, instead of suppressing the error
        // build the code as per recommendation for each runtime.
        var codeBaseUrl = new Uri(Assembly.GetExecutingAssembly().CodeBase);
        var codeBasePath = Uri.UnescapeDataString(codeBaseUrl.AbsolutePath);
        var directory = Path.GetDirectoryName(codeBasePath);
        return Path.GetFullPath(directory);
#else
        return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
#endif
    }

    public void SetEnvironmentVariable(string key, string? value)
    {
        EnvironmentHelper.CustomEnvironmentVariables[key] = value;
    }

    public void RemoveEnvironmentVariable(string key)
    {
        EnvironmentHelper.CustomEnvironmentVariables.Remove(key);
    }

    public void SetExporter(MockSpansCollector collector)
    {
        SetEnvironmentVariable("OTEL_TRACES_EXPORTER", "otlp");
        SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", $"http://localhost:{collector.Port}");
    }

    public void SetFileBasedExporter(MockSpansCollector collector)
    {
        SetEnvironmentVariable("OTEL_EXPORTER_OTLP_TRACES_ENDPOINT", $"http://localhost:{collector.Port}/v1/traces");
    }

    public void SetExporter(MockMetricsCollector collector)
    {
        SetEnvironmentVariable("OTEL_METRICS_EXPORTER", "otlp");
        SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", $"http://localhost:{collector.Port}");
    }

    public void SetFileBasedExporter(MockMetricsCollector collector)
    {
        SetEnvironmentVariable("OTEL_EXPORTER_OTLP_METRICS_ENDPOINT", $"http://localhost:{collector.Port}/v1/metrics");
    }

    public void SetExporter(MockLogsCollector collector)
    {
        SetEnvironmentVariable("OTEL_LOGS_EXPORTER", "otlp");
        SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", $"http://localhost:{collector.Port}");
    }

    public void SetFileBasedExporter(MockLogsCollector collector)
    {
        SetEnvironmentVariable("OTEL_EXPORTER_OTLP_LOGS_ENDPOINT", $"http://localhost:{collector.Port}/v1/logs");
    }

#if NET
    public void SetExporter(MockProfilesCollector collector)
    {
        SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", $"http://localhost:{collector.Port}");
    }
#endif

    public void EnableBytecodeInstrumentation()
    {
        SetEnvironmentVariable("CORECLR_ENABLE_PROFILING", "1");
    }

    public void EnableDefaultExporters()
    {
        RemoveEnvironmentVariable("OTEL_TRACES_EXPORTER");
        RemoveEnvironmentVariable("OTEL_METRICS_EXPORTER");
        RemoveEnvironmentVariable("OTEL_LOGS_EXPORTER");
    }

    public void EnableFileBasedConfigWithDefaultPath()
    {
        SetEnvironmentVariable("OTEL_EXPERIMENTAL_FILE_BASED_CONFIGURATION_ENABLED", "true");
        SetEnvironmentVariable("OTEL_EXPERIMENTAL_CONFIG_FILE", Path.Combine(EnvironmentHelper.GetTestApplicationApplicationOutputDirectory(), "config.yaml"));
    }

    public (string StandardOutput, string ErrorOutput, int ProcessId) RunTestApplication(TestSettings? testSettings = null)
    {
        // RunTestApplication starts the test application, wait up to DefaultProcessTimeout.
        // Assertion exceptions are thrown if it timed out or the exit code is non-zero.
        testSettings ??= new();
        using var process = StartTestApplication(testSettings);
        Output.WriteLine($"ProcessName: " + process?.ProcessName);
        using var helper = new ProcessHelper(process);

        Assert.NotNull(process);

        var processId = process!.Id;

        bool processTimeout = !process!.WaitForExit((int)TestTimeout.ProcessExit.TotalMilliseconds);
        if (processTimeout)
        {
            process.Kill();
        }

        Output.WriteLine("ProcessId: " + process.Id);
        Output.WriteLine("Exit Code: " + process.ExitCode);
        Output.WriteResult(helper);

        Assert.False(processTimeout, "Test application timed out");
        Assert.Equal(0, process.ExitCode);

        return (helper.StandardOutput, helper.ErrorOutput, processId);
    }

    public Process? StartTestApplication(TestSettings? testSettings = null)
    {
        // StartTestApplication starts the test application
        // and returns the Process instance for further interaction.
        testSettings ??= new();

        var startupMode = testSettings.StartupMode;
        if (startupMode == TestAppStartupMode.Auto)
        {
            startupMode = EnvironmentHelper.IsCoreClr() ? TestAppStartupMode.DotnetCLI : TestAppStartupMode.Exe;
        }

        // get path to test application that the profiler will attach to
        var testApplicationPath = EnvironmentHelper.GetTestApplicationPath(testSettings.PackageVersion, testSettings.Framework, startupMode);
        if (!File.Exists(testApplicationPath))
        {
            throw new Exception($"application not found: {testApplicationPath}");
        }

        if (startupMode == TestAppStartupMode.DotnetCLI)
        {
            Output.WriteLine($"DotnetCLI Starting Application: {testApplicationPath}");
            var executable = EnvironmentHelper.GetTestApplicationExecutionSource();
            var args = $"{testApplicationPath} {testSettings.Arguments ?? string.Empty}";
            return InstrumentedProcessHelper.Start(executable, args, EnvironmentHelper);
        }
        else if (startupMode == TestAppStartupMode.Exe)
        {
            Output.WriteLine($"Starting Application: {testApplicationPath}");
            return InstrumentedProcessHelper.Start(testApplicationPath, testSettings.Arguments, EnvironmentHelper);
        }
        else
        {
            throw new InvalidOperationException($"StartupMode '{startupMode}' has no logic defined.");
        }
    }
}
