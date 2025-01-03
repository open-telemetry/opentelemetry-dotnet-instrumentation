// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using FluentAssertions;
using IntegrationTests.Helpers;
using Xunit.Abstractions;

#if NETFRAMEWORK
using System.Net;
using System.Net.Http;
using FluentAssertions.Extensions;
using IntegrationTests.Helpers.Compatibility;
#endif

namespace IntegrationTests;

public class SmokeTests : TestHelper
{
    private const string ServiceName = "TestApplication.Smoke";

    public SmokeTests(ITestOutputHelper output)
        : base("Smoke", output)
    {
        SetEnvironmentVariable("OTEL_SERVICE_NAME", ServiceName);
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void SubmitsTraces()
    {
        VerifyTestApplicationInstrumented();
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void WhenStartupHookIsNotEnabled()
    {
        SetEnvironmentVariable("DOTNET_STARTUP_HOOKS", null);
#if NETFRAMEWORK
        VerifyTestApplicationInstrumented();
#else
        // on .NET it is required to set DOTNET_STARTUP_HOOKS
        VerifyTestApplicationNotInstrumented();
#endif
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void WhenClrProfilerIsNotEnabled()
    {
        SetEnvironmentVariable("COR_ENABLE_PROFILING", "0");
        SetEnvironmentVariable("CORECLR_ENABLE_PROFILING", "0");
#if NETFRAMEWORK
        // on .NET Framework it is required to set the CLR .NET Profiler
        VerifyTestApplicationNotInstrumented();
#else
        VerifyTestApplicationInstrumented();
#endif
    }

#if !NETFRAMEWORK
    [Theory]
    [Trait("Category", "EndToEnd")]
    [InlineData(TestAppStartupMode.DotnetCLI)]
    [InlineData(TestAppStartupMode.Exe)]
    public void WhenClrProfilerIsNotEnabledStartupHookIsEnabledApplicationIsExcluded(TestAppStartupMode testAppStartupMode)
    {
        switch (testAppStartupMode)
        {
            case TestAppStartupMode.DotnetCLI:
                SetEnvironmentVariable("OTEL_DOTNET_AUTO_EXCLUDE_PROCESSES", "dotnet,dotnet.exe");
                break;
            case TestAppStartupMode.Exe:
                SetEnvironmentVariable("OTEL_DOTNET_AUTO_EXCLUDE_PROCESSES", $"{EnvironmentHelper.FullTestApplicationName},{EnvironmentHelper.FullTestApplicationName}.exe");
                break;
            default:
                throw new ArgumentException($"{testAppStartupMode} is not supported by this test", nameof(testAppStartupMode));
        }

        SetEnvironmentVariable("CORECLR_ENABLE_PROFILING", "0");

        VerifyTestApplicationNotInstrumented(testAppStartupMode);
    }
#endif

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void ApplicationIsNotExcluded()
    {
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_EXCLUDE_PROCESSES", string.Empty);

        VerifyTestApplicationInstrumented(TestAppStartupMode.Exe);
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void ApplicationIsExcluded()
    {
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_EXCLUDE_PROCESSES", $"{EnvironmentHelper.FullTestApplicationName},{EnvironmentHelper.FullTestApplicationName}.exe");

        VerifyTestApplicationNotInstrumented(TestAppStartupMode.Exe);
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void SubmitMetrics()
    {
        using var collector = new MockMetricsCollector(Output);
        SetExporter(collector);
        collector.Expect("MyCompany.MyProduct.MyLibrary", metric => metric.Name == "MyFruitCounter");

        SetEnvironmentVariable("OTEL_DOTNET_AUTO_METRICS_ADDITIONAL_SOURCES", "MyCompany.MyProduct.MyLibrary");
        RunTestApplication();

        collector.AssertExpectations();
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void TracesResource()
    {
        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);

        EnableOnlyHttpClientTraceInstrumentation();
        SetEnvironmentVariable("OTEL_TRACES_EXPORTER", "otlp,console");
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_SOURCES", "MyCompany.MyProduct.MyLibrary");
        var (_, _, processId) = RunTestApplication();

        ExpectResources(collector.ResourceExpector, processId);

        collector.ResourceExpector.AssertExpectations();
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void MetricsResource()
    {
        using var collector = new MockMetricsCollector(Output);
        SetExporter(collector);

        EnableOnlyHttpClientTraceInstrumentation();
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_METRICS_ADDITIONAL_SOURCES", "MyCompany.MyProduct.MyLibrary");
        var (_, _, processId) = RunTestApplication();

        ExpectResources(collector.ResourceExpector, processId);

        collector.ResourceExpector.AssertExpectations();
    }

#if NET // The feature is not supported on .NET Framework
    [Fact]
    [Trait("Category", "EndToEnd")]
    public void LogsResource()
    {
        using var collector = new MockLogsCollector(Output);
        SetExporter(collector);

        EnableOnlyHttpClientTraceInstrumentation();
        EnableBytecodeInstrumentation();
        var (_, _, processId) = RunTestApplication();

        ExpectResources(collector.ResourceExpector, processId);

        collector.ResourceExpector.AssertExpectations();
    }
#endif

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void OtlpTracesExporter()
    {
        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);
        collector.Expect("MyCompany.MyProduct.MyLibrary", span => span.Name == "SayHello");

        EnableOnlyHttpClientTraceInstrumentation();
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_SOURCES", "MyCompany.MyProduct.MyLibrary");
        RunTestApplication();

        collector.AssertExpectations();
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void ZipkinExporter()
    {
        using var collector = new MockZipkinCollector(Output);
        collector.Expect(span => span.Name == "SayHello" && span.Tags?.GetValueOrDefault("otel.library.name") == "MyCompany.MyProduct.MyLibrary");

        EnableOnlyHttpClientTraceInstrumentation();
        SetEnvironmentVariable("OTEL_TRACES_EXPORTER", "zipkin");
        SetEnvironmentVariable("OTEL_EXPORTER_ZIPKIN_ENDPOINT", $"http://localhost:{collector.Port}/api/v2/spans");
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_SOURCES", "MyCompany.MyProduct.MyLibrary");
        SetEnvironmentVariable("LONG_RUNNING", "true");

        using var process = StartTestApplication();
        using var helper = new ProcessHelper(process);

        try
        {
            collector.AssertExpectations();
        }
        finally
        {
            if (helper?.Process != null && !helper.Process.HasExited)
            {
                helper.Process.Kill();
                helper.Process.WaitForExit();

                Output.WriteLine("ProcessId: " + helper.Process.Id);
                Output.WriteLine("Exit Code: " + helper.Process.ExitCode);
                Output.WriteResult(helper);
            }
        }
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void ZipkinAndOtlpTracesExporter()
    {
        using var otlpCollector = new MockSpansCollector(Output);
        SetExporter(otlpCollector);
        otlpCollector.Expect("MyCompany.MyProduct.MyLibrary", span => span.Name == "SayHello");

        using var zipkinCollector = new MockZipkinCollector(Output);
        zipkinCollector.Expect(span => span.Name == "SayHello" && span.Tags?.GetValueOrDefault("otel.library.name") == "MyCompany.MyProduct.MyLibrary");

        EnableOnlyHttpClientTraceInstrumentation();

        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_SOURCES", "MyCompany.MyProduct.MyLibrary");
        SetEnvironmentVariable("OTEL_TRACES_EXPORTER", "otlp,zipkin");
        SetEnvironmentVariable("OTEL_EXPORTER_ZIPKIN_ENDPOINT", $"http://localhost:{zipkinCollector.Port}/api/v2/spans");
        SetEnvironmentVariable("LONG_RUNNING", "true");

        using var process = StartTestApplication();
        using var helper = new ProcessHelper(process);

        try
        {
            otlpCollector.AssertExpectations();

            zipkinCollector.AssertExpectations();
        }
        finally
        {
            if (helper?.Process != null && !helper.Process.HasExited)
            {
                helper.Process.Kill();
                helper.Process.WaitForExit();

                Output.WriteLine("ProcessId: " + helper.Process.Id);
                Output.WriteLine("Exit Code: " + helper.Process.ExitCode);
                Output.WriteResult(helper);
            }
        }
    }

#if NETFRAMEWORK // The test is flaky on Linux and macOS, because of https://github.com/dotnet/runtime/issues/28658#issuecomment-462062760
    [Fact]
    [Trait("Category", "EndToEnd")]
    public void PrometheusExporter()
    {
        EnableOnlyHttpClientTraceInstrumentation();
        SetEnvironmentVariable("LONG_RUNNING", "true");
        SetEnvironmentVariable("OTEL_METRICS_EXPORTER", "prometheus");
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_METRICS_ADDITIONAL_SOURCES", "MyCompany.MyProduct.MyLibrary");
        const string defaultPrometheusMetricsEndpoint = "http://localhost:9464/metrics";

        using var process = StartTestApplication();
        using var helper = new ProcessHelper(process);

        try
        {
            var assert = async () =>
            {
                var httpClient = new HttpClient
                {
                    Timeout = 5.Seconds()
                };
                var response = await httpClient.GetAsync(defaultPrometheusMetricsEndpoint);
                response.StatusCode.Should().Be(HttpStatusCode.OK);

                var content = await response.Content.ReadAsStringAsync();
                Output.WriteLine("Raw metrics from Prometheus:");
                Output.WriteLine(content);
                content.Should().Contain("TYPE ", "should export any metric");
            };
            assert.Should().NotThrowAfterAsync(
                waitTime: 1.Minutes(),
                pollInterval: 1.Seconds());
        }
        finally
        {
            if (helper?.Process != null && !helper.Process.HasExited)
            {
                helper.Process.Kill();
                helper.Process.WaitForExit();

                Output.WriteLine("ProcessId: " + helper.Process.Id);
                Output.WriteLine("Exit Code: " + helper.Process.ExitCode);
                Output.WriteResult(helper);
            }
        }
    }
#endif

#if NETFRAMEWORK
    [Fact]
    [Trait("Category", "EndToEnd")]
    public void PrometheusAndOtlpMetricsExporter()
    {
        using var otlpCollector = new MockMetricsCollector(Output);
        SetExporter(otlpCollector);

        EnableOnlyHttpClientTraceInstrumentation();
        SetEnvironmentVariable("LONG_RUNNING", "true");
        SetEnvironmentVariable("OTEL_METRICS_EXPORTER", "otlp,prometheus");
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_METRICS_ADDITIONAL_SOURCES", "MyCompany.MyProduct.MyLibrary");
        const string defaultPrometheusMetricsEndpoint = "http://localhost:9464/metrics";

        using var process = StartTestApplication();
        using var helper = new ProcessHelper(process);

        try
        {
            var assert = async () =>
            {
                var httpClient = new HttpClient
                {
                    Timeout = 5.Seconds()
                };
                var response = await httpClient.GetAsync(defaultPrometheusMetricsEndpoint);
                response.StatusCode.Should().Be(HttpStatusCode.OK);

                var content = await response.Content.ReadAsStringAsync();
                Output.WriteLine("Raw metrics from Prometheus:");
                Output.WriteLine(content);
                content.Should().Contain("TYPE ", "should export any metric");
            };
            assert.Should().NotThrowAfterAsync(
                waitTime: 1.Minutes(),
                pollInterval: 1.Seconds());
        }
        finally
        {
            if (helper?.Process != null && !helper.Process.HasExited)
            {
                helper.Process.Kill();
                helper.Process.WaitForExit();

                Output.WriteLine("ProcessId: " + helper.Process.Id);
                Output.WriteLine("Exit Code: " + helper.Process.ExitCode);
                Output.WriteResult(helper);
            }
        }
    }
#endif

#if NET // The feature is not supported on .NET Framework
    [Fact]
    [Trait("Category", "EndToEnd")]
    public void SubmitLogs()
    {
        using var collector = new MockLogsCollector(Output);
        SetExporter(collector);
        collector.Expect(logRecord => Convert.ToString(logRecord.Body) == "{ \"stringValue\": \"Example log message\" }");

        EnableOnlyHttpClientTraceInstrumentation();
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOGS_INCLUDE_FORMATTED_MESSAGE", "true");
        EnableBytecodeInstrumentation();
        RunTestApplication();

        collector.AssertExpectations();
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void LogTraceCorrelation()
    {
        using var collector = new MockCorrelationCollector(Output);

        SetEnvironmentVariable("OTEL_TRACES_EXPORTER", "otlp");
        SetEnvironmentVariable("OTEL_LOGS_EXPORTER", "otlp");
        SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", $"http://localhost:{collector.Port}");

        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_SOURCES", "MyCompany.MyProduct.MyLibrary");

        SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOGS_INCLUDE_FORMATTED_MESSAGE", "true");
        EnableBytecodeInstrumentation();
        RunTestApplication();

        collector.ExpectSpan(collectedSpan => collectedSpan.InstrumentationScopeName == "MyCompany.MyProduct.MyLibrary");
        collector.ExpectLogRecord(record => Convert.ToString(record.Body) == "{ \"stringValue\": \"Example log message\" }");

        collector.AssertCorrelation();
    }

    [Theory]
    [InlineData("OTEL_DOTNET_AUTO_INSTRUMENTATION_ENABLED", "false")]
    [InlineData("OTEL_DOTNET_AUTO_LOGS_INSTRUMENTATION_ENABLED", "false")]
    [InlineData("OTEL_DOTNET_AUTO_LOGS_ENABLED", "false")]
    [InlineData("OTEL_LOGS_EXPORTER", "none")]
    [Trait("Category", "EndToEnd")]
    public void LogsNoneInstrumentations(string envVarName, string envVarVal)
    {
        using var collector = new MockLogsCollector(Output);
        SetExporter(collector);

        EnableOnlyHttpClientTraceInstrumentation();
        SetEnvironmentVariable(envVarName, envVarVal);
        EnableBytecodeInstrumentation();
        RunTestApplication();

        collector.AssertEmpty();
    }

#endif

    [Theory]
    [InlineData("OTEL_DOTNET_AUTO_INSTRUMENTATION_ENABLED", "false")]
    [InlineData("OTEL_DOTNET_AUTO_TRACES_INSTRUMENTATION_ENABLED", "false")]
    [InlineData("OTEL_TRACES_EXPORTER", "none")]
    [Trait("Category", "EndToEnd")]
    public void TracesNoneInstrumentations(string envVarName, string envVarVal)
    {
        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);
        SetEnvironmentVariable(envVarName, envVarVal);
        RunTestApplication();
        collector.AssertEmpty();
    }

    [Theory]
    [InlineData("OTEL_DOTNET_AUTO_INSTRUMENTATION_ENABLED", "false")]
    [InlineData("OTEL_DOTNET_AUTO_METRICS_INSTRUMENTATION_ENABLED", "false")]
    [InlineData("OTEL_DOTNET_AUTO_METRICS_ENABLED", "false")]
    [InlineData("OTEL_METRICS_EXPORTER", "none")]
    [Trait("Category", "EndToEnd")]
    public void MetricsNoneInstrumentations(string envVarName, string envVarVal)
    {
        using var collector = new MockMetricsCollector(Output);
        SetExporter(collector);
        EnableOnlyHttpClientTraceInstrumentation();
        SetEnvironmentVariable(envVarName, envVarVal);
        RunTestApplication();
        collector.AssertEmpty();
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void LogsDisabledInstrumentation()
    {
        using var collector = new MockLogsCollector(Output);
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOGS_DISABLED_INSTRUMENTATIONS", "ILogger");
        EnableOnlyHttpClientTraceInstrumentation();
        EnableBytecodeInstrumentation();
        RunTestApplication();
        collector.AssertEmpty();
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void MetricsDisabledInstrumentation()
    {
        using var collector = new MockMetricsCollector(Output);
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_METRICS_HTTPCLIENT_INSTRUMENTATION_ENABLED", "false");
        EnableOnlyHttpClientTraceInstrumentation();
        EnableBytecodeInstrumentation();
        RunTestApplication();
        collector.AssertEmpty();
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void TracesDisabledInstrumentation()
    {
        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_HTTPCLIENT_INSTRUMENTATION_ENABLED", "false");
        RunTestApplication();
        collector.AssertEmpty();
    }

    [Fact]
    [Trait("Category", "EndToEnd")]
    public void ApplicationFailFastDisabled()
    {
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_EXCLUDE_PROCESSES", $"dotnet,dotnet.exe,{EnvironmentHelper.FullTestApplicationName},{EnvironmentHelper.FullTestApplicationName}.exe");
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_FAIL_FAST_ENABLED", "false");

        VerifyTestApplicationNotInstrumented();
    }

    [Theory]
    [Trait("Category", "EndToEnd")]
    [InlineData("OTEL_TRACES_EXPORTER", "non-supported")]
    [InlineData("OTEL_DOTNET_AUTO_EXCLUDE_PROCESSES", $"dotnet,dotnet.exe,TestApplication.Smoke,TestApplication.Smoke.exe")]
    public void ApplicationFailFastEnabled(string additionalVariableKey, string additionalVariableValue)
    {
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_FAIL_FAST_ENABLED", "true");
        SetEnvironmentVariable(additionalVariableKey, additionalVariableValue);

        using var process = StartTestApplication();
        using var helper = new ProcessHelper(process);
        process.Should().NotBeNull();
        var processTimeout = !process!.WaitForExit((int)TestTimeout.ProcessExit.TotalMilliseconds);
        if (processTimeout)
        {
            process.Kill();
        }

        Output.WriteResult(helper);
        processTimeout.Should().BeFalse();

        process!.ExitCode.Should().NotBe(0);
    }

    [Fact]
    public void NativeLogsHaveNoSensitiveData()
    {
        var tempLogsDirectory = DirectoryHelpers.CreateTempDirectory();
        var secretIdentificators = new[] { "API", "TOKEN", "SECRET", "KEY", "PASSWORD", "PASS", "PWD", "HEADER", "CREDENTIALS" };

        EnableBytecodeInstrumentation();
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_LOG_DIRECTORY", tempLogsDirectory.FullName);
        SetEnvironmentVariable("OTEL_LOG_LEVEL", "debug");

        foreach (var item in secretIdentificators)
        {
            SetEnvironmentVariable($"OTEL_{item}_VALUE", "this is secret!");

            if (!EnvironmentTools.IsWindows())
            {
                SetEnvironmentVariable($"otel_{item.ToLowerInvariant()}_value2", "this is secret!");
            }
        }

        try
        {
            RunTestApplication();

            var nativeLog = tempLogsDirectory.GetFiles("otel-dotnet-auto-*-Native.log").Single();
            var nativeLogContent = File.ReadAllText(nativeLog.FullName);
            nativeLogContent.Should().NotBeNullOrWhiteSpace();

            var environmentVariables = ParseEnvironmentVariablesLog(nativeLogContent);
            environmentVariables.Should().NotBeEmpty();

            var secretVariables = environmentVariables
                .Where(item => secretIdentificators.Any(i => item.Key.Contains(i)))
                .ToList();

            secretVariables.Should().NotBeEmpty();
            secretVariables.Should().AllSatisfy(secret => secret.Value.Should().Be("<hidden>"));
        }
        finally
        {
            tempLogsDirectory.Delete(true);
        }
    }

    private static ICollection<KeyValuePair<string, string>> ParseEnvironmentVariablesLog(string log)
    {
        var lines = log.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
        var variables = lines
            .SkipWhile(x => !x.Contains("Environment variables:"))
#if NETFRAMEWORK
            .TakeWhile(x => !x.Contains(".NET Runtime: .NET Framework"))
#else
            .TakeWhile(x => !x.Contains("Interface ICorProfilerInfo12 found."))
#endif
            .Skip(1)
            .Select(ParseEnvironmentVariableLogEntry)
            .ToEnvironmentVariablesList();

        return variables;
    }

    private static string ParseEnvironmentVariableLogEntry(string entry)
    {
        const string startMarker = "[debug]";
        var startIndex = entry.IndexOf("[debug]") + startMarker.Length;

        return entry.AsSpan().Slice(startIndex).Trim().ToString();
    }

    private static void ExpectResources(OtlpResourceExpector resourceExpector, int processId)
    {
        resourceExpector.Expect("service.name", ServiceName); // this is set via env var and App.config, but env var has precedence
#if NETFRAMEWORK
        resourceExpector.Expect("deployment.environment", "test"); // this is set via App.config
#endif
        resourceExpector.Expect("telemetry.sdk.name", "opentelemetry");
        resourceExpector.Expect("telemetry.sdk.language", "dotnet");
        resourceExpector.Expect("telemetry.sdk.version", typeof(OpenTelemetry.Resources.Resource).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion.Split('+')[0]);
        resourceExpector.Expect("telemetry.distro.name", "opentelemetry-dotnet-instrumentation");
        resourceExpector.Expect("telemetry.distro.version", typeof(OpenTelemetry.AutoInstrumentation.Constants).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion.Split('+')[0]);

        resourceExpector.Expect("process.pid", processId);
        resourceExpector.Expect("host.name", Environment.MachineName);

#if NETFRAMEWORK
        resourceExpector.Expect("process.runtime.name", ".NET Framework");
#else
        resourceExpector.Expect("process.runtime.name", ".NET");
#endif

        var expectedPlatform = EnvironmentTools.GetOS() switch
        {
            "win" => "windows",
            "osx" => "darwin",
            "linux" => "linux",
            _ => throw new PlatformNotSupportedException($"Unknown platform")
        };
        resourceExpector.Expect("os.type", expectedPlatform);
        resourceExpector.Exist("os.build_id");
        resourceExpector.Exist("os.description");
        resourceExpector.Exist("os.name");
        resourceExpector.Exist("os.version");
    }

    private void VerifyTestApplicationInstrumented(TestAppStartupMode startupMode = TestAppStartupMode.Auto)
    {
        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);
        collector.Expect("MyCompany.MyProduct.MyLibrary");
#if NETFRAMEWORK
        collector.Expect("OpenTelemetry.Instrumentation.Http.HttpWebRequest");
#else
        collector.Expect("System.Net.Http");
#endif

        EnableOnlyHttpClientTraceInstrumentation();
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_SOURCES", "MyCompany.MyProduct.MyLibrary");
        RunTestApplication(new() { StartupMode = startupMode });

        collector.AssertExpectations();
    }

    private void VerifyTestApplicationNotInstrumented(TestAppStartupMode startupMode = TestAppStartupMode.Auto)
    {
        using var collector = new MockSpansCollector(Output);
        SetExporter(collector);

        EnableOnlyHttpClientTraceInstrumentation();
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_SOURCES", "MyCompany.MyProduct.MyLibrary");
        RunTestApplication(new() { StartupMode = startupMode });

        collector.AssertEmpty();
    }

    private void EnableOnlyHttpClientTraceInstrumentation()
    {
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_INSTRUMENTATION_ENABLED", "false");
        SetEnvironmentVariable("OTEL_DOTNET_AUTO_TRACES_HTTPCLIENT_INSTRUMENTATION_ENABLED", "true");
    }
}
