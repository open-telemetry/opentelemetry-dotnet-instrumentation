// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using System.Reflection;
using Xunit.Abstractions;

namespace IntegrationTests.Helpers;

#pragma warning disable CA1515 //  // Consider making public types internal. Needed for xunit tests.
public abstract class TestHelper
#pragma warning restore CA1515 //  // Consider making public types internal. Needed for xunit tests.
{
    protected TestHelper(string testApplicationName, ITestOutputHelper output, string testApplicationType = "integrations")
    {
        Output = output ?? throw new ArgumentNullException(nameof(output));
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

    internal EnvironmentHelper EnvironmentHelper { get; }

    protected ITestOutputHelper Output { get; }

    public static string GetTestAssemblyPath()
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

    internal void SetExporter(MockSpansCollector collector)
    {
        SetEnvironmentVariable("OTEL_TRACES_EXPORTER", "otlp");
        SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", $"http://localhost:{collector.Port}");
    }

    internal void SetFileBasedExporter(MockSpansCollector collector)
    {
        SetEnvironmentVariable("OTEL_EXPORTER_OTLP_TRACES_ENDPOINT", $"http://localhost:{collector.Port}/v1/traces");
    }

#if !NUGET_PACKAGE_TESTS
    internal void SetExporter(MockMetricsCollector collector)
    {
        SetEnvironmentVariable("OTEL_METRICS_EXPORTER", "otlp");
        SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", $"http://localhost:{collector.Port}");
    }

    internal void SetFileBasedExporter(MockMetricsCollector collector)
    {
        SetEnvironmentVariable("OTEL_EXPORTER_OTLP_METRICS_ENDPOINT", $"http://localhost:{collector.Port}/v1/metrics");
    }

    internal void SetExporter(MockLogsCollector collector)
    {
        SetEnvironmentVariable("OTEL_LOGS_EXPORTER", "otlp");
        SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", $"http://localhost:{collector.Port}");
    }

    internal void SetFileBasedExporter(MockLogsCollector collector)
    {
        SetEnvironmentVariable("OTEL_EXPORTER_OTLP_LOGS_ENDPOINT", $"http://localhost:{collector.Port}/v1/logs");
    }

    internal void SetExporter(MockProfilesCollector collector)
    {
        SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", $"http://localhost:{collector.Port}");
    }
#endif

    internal void EnableBytecodeInstrumentation()
    {
        SetEnvironmentVariable("CORECLR_ENABLE_PROFILING", "1");
    }

    internal void EnableDefaultExporters()
    {
        RemoveEnvironmentVariable("OTEL_TRACES_EXPORTER");
        RemoveEnvironmentVariable("OTEL_METRICS_EXPORTER");
        RemoveEnvironmentVariable("OTEL_LOGS_EXPORTER");
    }

    internal void EnableFileBasedConfigWithDefaultPath()
    {
        SetEnvironmentVariable("OTEL_EXPERIMENTAL_FILE_BASED_CONFIGURATION_ENABLED", "true");
        SetEnvironmentVariable("OTEL_EXPERIMENTAL_CONFIG_FILE", Path.Combine(EnvironmentHelper.GetTestApplicationApplicationOutputDirectory(), "config.yaml"));
    }

    /// <summary>
    /// Starts the test application and waits for it to exit.
    /// Use this when you need to verify the process output or exit code, or when the test application
    /// completes its work and exits naturally.
    /// Assertion exceptions are thrown if it timed out or the exit code is non-zero.
    /// For tests that need to collect telemetry from a running process and then terminate it manually,
    /// use <see cref="StartTestApplication"/> instead.
    /// </summary>
    internal (string StandardOutput, string ErrorOutput, int ProcessId) RunTestApplication(TestSettings? testSettings = null)
    {
        testSettings ??= new();
        // Don't drain streams here because ProcessHelper will do it and capture the output.
        // Calling BeginOutputReadLine twice would throw InvalidOperationException.
        using var process = StartTestApplication(testSettings, drainStreams: false);
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

    /// <summary>
    /// Starts the test application and returns immediately.
    /// Use this when you need to collect telemetry while the process runs, will manually terminate
    /// the process (e.g., process.Kill()), or don't need to verify exit code or output.
    /// By default, output streams are drained asynchronously to prevent deadlock.
    /// For tests that need to wait for clean exit and verify output, use <see cref="RunTestApplication"/> instead.
    /// </summary>
    /// <param name="testSettings">Optional test settings.</param>
    /// <param name="drainStreams">
    /// If true (default), output streams are drained asynchronously to prevent process deadlock.
    /// Set to false only if you will create a ProcessHelper to capture output.
    /// </param>
    internal Process? StartTestApplication(TestSettings? testSettings = null, bool drainStreams = true)
    {
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
            throw new InvalidOperationException($"application not found: {testApplicationPath}");
        }

        Process? process;
        if (startupMode == TestAppStartupMode.DotnetCLI)
        {
            Output.WriteLine($"DotnetCLI Starting Application: {testApplicationPath}");
            var executable = EnvironmentHelper.GetTestApplicationExecutionSource();
            var args = $"{testApplicationPath} {testSettings.Arguments ?? string.Empty}";
            process = InstrumentedProcessHelper.Start(executable, args, EnvironmentHelper);
        }
        else if (startupMode == TestAppStartupMode.Exe)
        {
            Output.WriteLine($"Starting Application: {testApplicationPath}");
            process = InstrumentedProcessHelper.Start(testApplicationPath, testSettings.Arguments, EnvironmentHelper);
        }
        else
        {
            throw new InvalidOperationException($"StartupMode '{startupMode}' has no logic defined.");
        }

        // Start draining output streams to prevent deadlock when buffers fill up.
        // This happens asynchronously and doesn't block the caller.
        // Skip if caller will create a ProcessHelper (which also calls BeginOutputReadLine).
        if (process != null && drainStreams)
        {
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
        }

        return process;
    }
}
